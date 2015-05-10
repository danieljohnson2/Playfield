using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller causes a creature to move according to a heapmap.
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
/// The creature would try to stay at least 5 squers away from the
/// player, but would try tor each gold that is within 16 squares of
/// iteself. Note that anything beyond 'heatmapRange' squares is invisble
/// anyway, and the lure of gold will be stronger than the fear of the player
/// because it has a bigger number.
/// 
/// You can specify the distance we spread the 'heat' in the map;
/// this is the range the creature can 'see' things. You can specify
/// how much the map cools before each turns; if this is less than
/// the preference values, the creature will act acording to its
/// memory of where things were, rather than instantly knowing where
/// everything is in range is right now.
/// </summary>
public class HeapmapAIController : CreatureController
{
	private Heatmap heatmap = new Heatmap ();
	public short heatmapRange = 16;
	public short heatmapCooling = 128;
	public string[] preferences;

	protected override void DoTurn ()
	{
		UpdateHeatmap ();

		IEnumerable<Location> candidateMoves =
			Location.Of (gameObject).
				GetAdjacent ().
				Where (mapController.IsPassable);
		
		Location picked;
		if (heatmap.TryPickMove (candidateMoves, out picked)) {
			MoveTo (picked);
		}
	}

	/// <summary>
	/// This adjusts the heatmap according to the current
	/// position of entities.
	/// </summary>
	private void UpdateHeatmap ()
	{
		heatmap.Reduce (heatmapCooling);
		
		if (preferences != null && preferences.Length > 0) {
			ILookup<string, GameObject> taggedEntities = mapController.entities.byTag;
			
			foreach (string prefText in preferences) {
				string tag;
				short heat;
				ParsePreference (prefText, out tag, out heat);
				
				if (tag != null && heat != 0) {
					foreach (GameObject target in taggedEntities[tag]) {
						Location targetLoc = Location.Of (target);
						heatmap [targetLoc] += heat;
					}
				}
			}
		}
		
		heatmap.Heat (heatmapRange, mapController.IsPathable);
	}

	/// <summary>
	/// This method parses an entry in 'preferences' into a tag
	/// and a heat value. If the heat value is omitted we
	/// assume 'heatmapRange' as the default.
	/// </summary>
	private void ParsePreference (string text, out string tag, out short heat)
	{
		string[] parts = (text ?? "").Trim ().Split ('=');

		tag = parts [0].Trim ();
		if (parts.Length > 1) {
			heat = short.Parse (parts [1]);
		} else {
			heat = heatmapRange;
		}
	}
}