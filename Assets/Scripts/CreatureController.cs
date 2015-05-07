using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CreatureController : MovementBlocker
{
	public int hitPoints = 10;
	public int damage = 3;

	public CreatureController ()
	{
		this.pathable = true;
		this.passable = true;
	}

	public virtual IEnumerator DoTurnAsync ()
	{
		DoTurn ();
		return Enumerable.Empty<object> ().GetEnumerator ();
	}
	
	public virtual void DoTurn ()
	{
	}

	public override bool Block (GameObject mover)
	{
		var attacker = mover.GetComponent<CreatureController> ();

		if (attacker != null) {
			mapController.InstantiateByName ("Slash", Location.Of (gameObject));

			hitPoints = Math.Max (0, hitPoints - attacker.damage);

			if (hitPoints <= 0) {
				mapController.RemoveEntity (gameObject);
			}
		}

		return false;
	}

	public void Move (int dx, int dy)
	{
		Location loc = Location.Of (transform);
		loc.x += dx;
		loc.y += dy;
		MoveTo (loc);
	}

	public void MoveTo (Location destination)
	{
		foreach (var b in mapController.ComponentsInCell<MovementBlocker>(destination)) {
			if (!b.Block (gameObject))
				return;
		}
		
		transform.position = destination.ToPosition ();
	}
}
