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
/// </summary>
public class HeatmapPreferenceController : MonoBehaviour
{
	private readonly Heatmap heatmap = new Heatmap ();
	public int priority = 0;
	public short heatmapRange = 16;
	public float heatmapCooling = 128.0f;
	public string[] preferences;
	public GameObject heatmapMarkerPrefab;
	private GameObject[] heatmapMarkers;
	private float residualCooling = 0.0f;
	public string heatmapName;

	public void Awake ()
	{
		this.heatmap.name = heatmapName;
	}

	/// <summary>
	/// This adjusts the heatmap according to the current
	/// position of entities.
	/// </summary>
	public Heatmap UpdateHeatmap (MapController mapController)
	{
		residualCooling += heatmapCooling;

		if (residualCooling > 0.0f) {
			int cool = Mathf.FloorToInt (residualCooling);
			heatmap.Reduce (cool);
			residualCooling -= cool;
		}

		if (preferences != null) {
			foreach (string prefText in preferences) {
				IEnumerable<GameObject> targets;
				short heat;
				ParsePreference (prefText, mapController, out targets, out heat);
				
				if (targets != null) {
					foreach (GameObject target in targets) {
						Location targetLoc = Location.Of (target);
						heatmap [targetLoc] = heat;
					}
				}
			}
		}

		heatmap.Heat (heatmapRange,
		              (loc, adj) => mapController.adjacencyGenerator.GetAdjacentLocationsInto (gameObject, loc, adj));

		if (heatmapMarkerPrefab != null) {
			ShowHeatmap ();
		}

		return heatmap;
	}

	private void ShowHeatmap ()
	{
		const short minHeat = 1;

		var markers = new Queue<GameObject> (heatmapMarkers ?? Enumerable.Empty<GameObject> ());
		var usedMarkers = new List<GameObject> ();
		Map activeMap = MapController.instance.activeMap;

		if (activeMap != null) {
			foreach (var pair in heatmap) {
				if (pair.Value >= minHeat && pair.Key.mapIndex == activeMap.mapIndex) {
					GameObject marker = markers.Count > 0 ?
						markers.Dequeue () : Instantiate (heatmapMarkerPrefab);

					marker.transform.localPosition = pair.Key.ToPosition ();
					marker.name = string.Format ("Heat = {0}", pair.Value);
					usedMarkers.Add (marker);
				}
			}
		}

		while (markers.Count > 0) {
			Destroy (markers.Dequeue ());
		}

		heatmapMarkers = usedMarkers.ToArray ();
	}

	/// <summary>
	/// This method parses an entry in 'preferences' into a set of
	/// targets and a heat value. If the heat value is omitted we
	/// assume 'heatmapRange' as the default.
	/// </summary>
	private void ParsePreference (string text, MapController mapController, out IEnumerable<GameObject> targets, out short heat)
	{
		string[] parts = (text ?? "").Trim ().Split ('=');
		
		string tag = parts [0].Trim ();
		
		if (tag == "") {
			targets = Enumerable.Empty<GameObject> ();
			heat = 0;
			return;
		}
		
		if (tag.StartsWith ("\"") && tag.EndsWith ("\"")) {
			string name = tag.Substring (1, tag.Length - 2).Trim ();
			targets = mapController.entities.byName [name];
		} else {
			targets = mapController.entities.byTag [tag];
		}
		
		if (parts.Length > 1) {
			heat = short.Parse (parts [1]);
		} else {
			heat = heatmapRange;
		}
	}
}
