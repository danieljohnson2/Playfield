using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GoblinController : CreatureController
{
	private Heatmap heatmap = new Heatmap ();
	private const int heatmapSpeed = 8;

	public override void DoTurn ()
	{
		const short playerHeat = 128;
		const short goldHeat = 256;

		foreach (var pc in mapController.EntityComponents<PlayerController>())
			heatmap [Location.Of (pc.gameObject)] = playerHeat;

		foreach (var gc in mapController.EntityComponents<GoldController>())
			heatmap [Location.Of (gc.gameObject)] = goldHeat;

		heatmap = heatmap.GetHeated (heatmapSpeed, mapController.IsPathable);
		heatmap.Reduce (heatmapSpeed);

		IEnumerable<Location> candidateMoves =
			Location.Of(gameObject).
			GetAdjacent ().
			Where (mapController.IsPassable);

		Location picked;
		if (heatmap.TryPickMove (candidateMoves, out picked)) {
			MoveTo (picked);
		}
	}
}
