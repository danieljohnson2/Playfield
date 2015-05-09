﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GoblinController : CreatureController
{
	private Heatmap heatmap = new Heatmap ();
	private const int heatmapSpeed = 16;
	private const int heatmapCooling = 128;

	protected override void DoTurn ()
	{
		const short playerHeat = 100;
		const short goldHeat = 128;

		heatmap.Reduce (heatmapCooling);

		foreach (var pc in mapController.entities.Components<PlayerController>())
			heatmap [Location.Of (pc.gameObject)] = playerHeat;

		foreach (var gc in mapController.entities.Components<GoldController>())
			heatmap [Location.Of (gc.gameObject)] = goldHeat;

		heatmap = heatmap.GetHeated (heatmapSpeed, mapController.IsPathable);

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
