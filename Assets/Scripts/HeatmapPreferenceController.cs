using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller contains the preference data that we use
/// to generate heatmaps. This controller is useless by itself;
/// you need a HeatmapAIController to use the heatmaps defined.
/// 
/// Doing it this way means you can have multiple heatmaps with
/// priorities, so the creature AI will pick the highest priority
/// heatmap that has any heat for any of its moves.
/// 
/// The 'preferences' array indicates the heat values applied for 
/// various objects (identified by tag); the creature moves
/// towards positive values and away from negative ones.
/// 
/// Each preference item takes the form "<tag>=<heat>" so you have
/// entries like this:
/// 	Player=-5
/// 	Gold=16
/// 
/// Object names can also be used, by quoting them:
///  	"Bob the Goblin"=-128
/// 
/// The creature would try to stay at least 5 squers away from the
/// player, but would try tor each gold that is within 16 squares of
/// iteself. Note that anything beyond 'heatmapRange' squares is invisble
/// anyway, and the lure of gold will be stronger than the fear of the player
/// because it has a bigger number. Note that over time a creater can become
/// aware of things that are out of the heatmap range, if the cooling is not too
/// high- but this will be out-of-date information.
/// 
/// You can specify the distance we spread the 'heat' in the map;
/// this is the range the creature can 'see' things. You can specify
/// how much the map cools before each turns; if this is less than
/// the preference values, the creature will act acording to its
/// memory of where things were, rather than instantly knowing where
/// everything is in range is right now.
/// 
/// You can specify the 'auto reset' option to maket he map reset whenever
/// its source game objects move. This is useful for long range loot-tracking
/// heatmaps- they can have a low range but lower cooling, and the heat data
/// will spread slowing, saving CPU cycles, but will be discarded when invalid.
/// </summary>
public class HeatmapPreferenceController : MonoBehaviour
{
    private readonly Heatmap heatmap = new Heatmap();
    public int priority = 0;
    public short heatmapRange = 16;
    public float heatmapCooling = 128.0f;
    public bool heatmapAutoReset = false;
    public string[] preferences;
    public GameObject heatmapMarkerPrefab;
    private GameObject[] heatmapMarkers;
    private float residualCooling = 0.0f;
    public string heatmapName;
    public float heldItemAwareness = 1.0f;
    public float carriedItemAwareness = 0.25f;
    public bool itemSpecificHeat = false;

    public void Awake()
    {
        this.heatmap.name = heatmapName;
    }

    private KeyValuePair<HeatSourceIdentifier, short>[] lazyPreferences;

    private IEnumerable<KeyValuePair<HeatSourceIdentifier, short>> Preferences()
    {
        return Lazy.Init(ref lazyPreferences, delegate
        {
            if (preferences == null)
                return new KeyValuePair<HeatSourceIdentifier, short>[0];

            var b = new List<KeyValuePair<HeatSourceIdentifier, short>>(preferences.Length);

            foreach (string prefText in preferences)
            {
                HeatSourceIdentifier sourceID;
                short heat;
                ParsePreference(prefText, MapController.instance, out sourceID, out heat);
                b.Add(new KeyValuePair<HeatSourceIdentifier, short>(sourceID, heat));
            }

            return b.ToArray();
        });
    }

    public bool AppliesHeatFor(GameObject candidate)
    {
        var info = new Heatmap.SourceInfo(candidate);

        foreach (var pair in Preferences())
        {
            if (pair.Key.Matches(info))
                return true;
        }

        return false;
    }

    public void ResetHeatmap()
    {
        heatmap.Clear();
    }

    /// <summary>
    /// This adjusts the heatmap according to the current
    /// position of entities.
    /// </summary>
    public Heatmap UpdateHeatmap()
    {
        if (PlayableEntityController.isCommandPending)
            return heatmap;

        MapController mapController = MapController.instance;

        residualCooling += heatmapCooling;

        if (residualCooling > 0.0f)
        {
            int cool = Mathf.FloorToInt(residualCooling);
            heatmap.Reduce(cool);
            residualCooling -= cool;
        }

        foreach (KeyValuePair<HeatSourceIdentifier, short> pair in Preferences())
        {
            HeatSourceIdentifier sourceID = pair.Key;
            short heat = pair.Value;

            foreach (GameObject target in sourceID.GameObjects())
            {
                if (target != gameObject)
                {
                    Location targetLoc = Location.Of(target);

                    if (targetLoc == Location.nowhere)
                    {
                        ItemController ic = target.GetComponent<ItemController>();
                        CreatureController carrier;

                        if (ic != null && ic.TryGetCarrier(out carrier) && carrier.gameObject != gameObject)
                        {
                            float scaling = ic.isHeldItem ? heldItemAwareness : carriedItemAwareness;

                            if (itemSpecificHeat)
                                scaling *= ic.GetHeatmapScalingFactor(gameObject);

                            if (scaling == 0.0f)
                                heat = 0;
                            else
                            {
                                targetLoc = Location.Of(carrier.gameObject);

                                // we must apply the scaling in such a way that
                                // we don't overflow the heat value, which is only
                                // a short.
                                float scaledHeat = heat * scaling;

                                if (scaledHeat <= short.MinValue)
                                    heat = short.MinValue;
                                else if (scaledHeat >= short.MaxValue)
                                    heat = short.MaxValue;
                                else
                                    heat = (short)scaledHeat;
                            }
                        }
                    }

                    if (heat != 0 && targetLoc != Location.nowhere && mapController.IsPassable(targetLoc))
                        heatmap[targetLoc] = new Heatmap.Slot(target, heat);
                }
            }
        }

        MapController.AdjacencyGenerator adjGen = MapController.instance.adjacencyGenerator;

        heatmap.Heat(heatmapRange, (loc, adj) =>
            adjGen.GetAdjacentLocationsInto(gameObject, loc, adj));

        heatmap.TrimExcess();

        if (heatmapMarkerPrefab != null)
            ShowHeatmap();

        return heatmap;
    }

    private void ShowHeatmap()
    {
        var markers = new Queue<GameObject>(heatmapMarkers ?? Enumerable.Empty<GameObject>());
        var usedMarkers = new List<GameObject>();
        Map activeMap = MapController.instance.activeMap;

        if (activeMap != null)
        {
            foreach (var pair in heatmap)
            {
                if (pair.Value.heat != 0 && pair.Key.mapIndex == activeMap.mapIndex)
                {
                    GameObject marker = markers.Count > 0 ?
                        markers.Dequeue() : Instantiate(heatmapMarkerPrefab);

                    marker.transform.localPosition = pair.Key.ToPosition();
                    marker.name = string.Format("Heat = {0}", pair.Value);
                    usedMarkers.Add(marker);
                }
            }
        }

        while (markers.Count > 0)
        {
            Destroy(markers.Dequeue());
        }

        heatmapMarkers = usedMarkers.ToArray();
    }

    /// <summary>
    /// This method parses an entry in 'preferences' into a set of
    /// targets and a heat value. If the heat value is omitted we
    /// assume 'heatmapRange' as the default.
    /// </summary>
    private void ParsePreference(string text, MapController mapController, out HeatSourceIdentifier sourceID, out short heat)
    {
        string[] parts = (text ?? "").Trim().Split('=');

        string tag = parts[0].Trim();

        if (tag == "")
        {
            sourceID = default(HeatSourceIdentifier);
            heat = 0;
            return;
        }

        sourceID = HeatSourceIdentifier.Parse(tag);

        if (parts.Length > 1)
        {
            heat = short.Parse(parts[1]);
        }
        else
        {
            heat = heatmapRange;
        }
    }
}
