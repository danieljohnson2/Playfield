using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CreatureController : MovementBlocker
{
	public int hitPoints = 10;
	public int damage = 3;
	public GameObject attackEffect;

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
			if (attacker.attackEffect != null) {
				GameObject effect = Instantiate (attackEffect);
				effect.transform.position = transform.position;
			}

			hitPoints = Math.Max (0, hitPoints - attacker.damage);

			if (hitPoints <= 0) {
				mapController.entities.RemoveEntity (gameObject);
			}
		}

		return false;
	}

	public void Move (int dx, int dy)
	{
		Location loc = Location.Of (gameObject).WithOffset (dx, dy);
		MoveTo (loc);
	}

	public void MoveTo (Location destination)
	{
		if (mapController.GetTerrain (destination) == null) {
			return;
		}

		foreach (var b in mapController.ComponentsInCell<MovementBlocker>(destination)) {
			if (!b.Block (gameObject))
				return;
		}
		
		transform.position = destination.ToPosition ();
	}
}
