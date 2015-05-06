using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GoblinController : CreatureController
{
	private Heatmap heatmap = new Heatmap ();

	public override void DoTurn ()
	{
		const short playerHeat = 16;
		const short goldHeat = 8;

		foreach (var pc in mapController.EntityComponents<PlayerController>())
			heatmap[Location.Of(pc.gameObject)] = playerHeat;

		foreach (var gc in mapController.EntityComponents<GoldController>())
			heatmap [Location.Of (gc.gameObject)] = goldHeat;

		heatmap = heatmap.GetHeated (mapController.IsPathable);
		heatmap.Reduce ();

		IEnumerable<Location> moves =
				(from d in Location.Of (transform).GetAdjacent ()
				 where mapController.IsPassable (d)
				 group d by heatmap [d] into g
				 orderby g.Key descending
				 select g).FirstOrDefault ();

		if (moves != null) {
			int index = Random.Range (0, moves.Count ());
			MoveTo (moves.ElementAt (index));
		}
	}
}
