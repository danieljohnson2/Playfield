using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// This controller causes a creature to move according to a heatmap. The
/// heatmaps are provided through HeatmapPreferenceControllers attached
/// to the same GameObject; this will try each one in priority order
/// until it finds a move. If the heatmaps turn up no moves, this AI makes
/// a random move.
/// </summary>
public class HeatmapAIController : CreatureController
{
	/// <summary>
	/// This is the heatmap that was most recently used
	/// to make a move, or null if none could be used.
	/// </summary>
	public Heatmap activeHeatmap { get; private set; }

	/// <summary>
	/// This tests to see if the active heapmap is the one named, and
	/// if the active location in came from the source indicated, and has
	/// the minimum strength indicated.
	/// </summary>
	public bool CheckActiveHeatmap (string heatmapName, int minimumHeatmapStrength, HeatSourceIdentifier source)
	{
		if (activeHeatmap != null && 
			(heatmapName ?? "") == activeHeatmap.name) {
			Location loc = Location.Of (gameObject);
			Heatmap.Slot slot = activeHeatmap [loc];
			
			return
				slot.heat >= minimumHeatmapStrength &&
				source.Matches (slot.source);
		}

		return false;
	}

	protected override void DoTurn ()
	{
		List<Heatmap> heatmaps = UpdateHeatmaps ();

		// Note that the candidate moves must be to
		// passable cells, not pathable ones- it's
		// potentially different.

		Location[] candidateMoves =
			Location.Of (gameObject).Adjacent ().
			Where (mapController.IsPassable).
			ToArray ();

		if (candidateMoves.Length > 0) {
			foreach (Heatmap heatmap in heatmaps) {
				Location picked;
				if (heatmap.TryPickMove (candidateMoves, out picked)) {
					activeHeatmap = heatmap;
					MoveTo (picked);
					return;
				}
			}

			int randomIndex = Random.Range (0, candidateMoves.Length);
			activeHeatmap = null;
			MoveTo (candidateMoves [randomIndex]);
		} else {
			activeHeatmap = null;
		}
	}

	/// <summary>
	/// This method updates each HeatmapPreferenceController's heatmap
	/// at start of turn, and returns a list containing all of
	/// them.
	/// </summary>
	private List<Heatmap> UpdateHeatmaps ()
	{
		var heatmaps = new List<Heatmap> ();

		var components =
			from hpc in GetComponents<HeatmapPreferenceController> ()
			orderby hpc.priority descending, hpc.heatmapRange, hpc.heatmapCooling descending
			select hpc;

		foreach (var hpc in components) {
			heatmaps.Add (hpc.UpdateHeatmap (mapController));
		}

		return heatmaps;
	}
}