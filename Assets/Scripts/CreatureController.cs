using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// CreatureController provides the base class for things that take
/// action in the world - the player, NPCs, monsters. The MapController
/// invokes the DoTurnAsync() in a co-routine, let us take turns.
/// 
/// By default, creatures are passable and don't block pathing,
/// but when one moves into another the Block() method usually
/// prevents movement. We stil have 'passable=true' so that NPCs
/// will be willing to try to move into the creature's square;
/// if it's false they'll avoid doing this.
/// </summary>
public class CreatureController : MovementBlocker
{
	public int hitPoints = 10;
	public DieRoll damage = new DieRoll (1, 3);
	public float speed = 1;
	public int goldCarried = 0;
	public GameObject attackEffect;
	private float maxSpeed = 20.0f;
	private float turnCounter = 0;

	public CreatureController ()
	{
		this.pathable = true;
		this.passable = true;
	}

	/// <summary>
	/// This method decides if the creatures turn has
	/// arrived; we reduce the turnCounter until it goes 0
	/// or negative, and then its this creature's turn. By
	/// having a larger or smaller speed, turns will come up
	/// more or less often.
	/// </summary>
	public virtual bool CheckTurn ()
	{
		if (turnCounter <= float.Epsilon) {
			turnCounter += maxSpeed;
			return true;
		} else {
			turnCounter -= speed;
			return false;
		}
	}

	/// <summary>
	/// This is the entry point used to start the
	/// creatures turn. The next creature's turn
	/// begins only when this one ends, but in most
	/// cases aren't really much of a co-routine; this
	/// method calls DoTurn(), then ends the turn
	/// synchronously.
	/// </summary>
	public virtual IEnumerator DoTurnAsync ()
	{
		DoTurn ();
		return Enumerable.Empty<object> ().GetEnumerator ();
	}

	/// <summary>
	/// This is a sychrnonous entry point; you override this
	/// and do whatever the creature should do during its turn.
	/// </summary>
	protected virtual void DoTurn ()
	{
	}

	#region Creature Actions
		
	public override bool Block (GameObject mover, Location destination)
	{
		var attacker = mover.GetComponent<CreatureController> ();
		
		if (attacker != null) {
			Fight (attacker);
		}
		
		return false;
	}

	/// <summary>
	/// This method handles combat where 'attacker' attachs this
	/// creature.
	/// </summary>
	protected virtual void Fight (CreatureController attacker)
	{
		if (attacker.attackEffect != null && gameObject.activeSelf) {
			GameObject effect = Instantiate (attackEffect);
			effect.transform.parent = transform.parent;
			effect.transform.position = transform.position;
		}

		int damage = attacker.damage.Roll ();
		hitPoints -= damage;
		
		if (hitPoints <= 0) {
			hitPoints = 0;
			AddTranscriptLine ("{0} killed {1}!", attacker.name, this.name);
			Die ();
		} else {
			AddTranscriptLine ("{0} hit {1} for {2}!", attacker.name, this.name, damage);
		}
	}

	/// <summary>
	/// This method moves the creature by the delta indicated
	/// within the current map. This executes Block() methods
	/// and may fail; if the creature could not move this method
	/// returns false. If it did move it returns true.
	/// </summary>
	protected bool Move (int dx, int dy)
	{
		Location loc = Location.Of (gameObject).WithOffset (dx, dy);
		return MoveTo (loc);
	}

	/// <summary>
	/// This method moves the creature to a specific location,
	/// which could be on a different map. Like Move(), this runs
	/// Block() methods and returns false if the movement is blocked,
	/// true if it succeeds. 
	/// </summary>
	protected bool MoveTo (Location destination)
	{
		if (mapController.GetTerrain (destination) == null) {
			return false;
		}

		foreach (var blocker in mapController.ComponentsInCell<MovementBlocker>(destination)) {
			if (!blocker.Block (gameObject, destination))
				return false;
		}
		
		transform.position = destination.ToPosition ();
		return true;
	}

	/// <summary>
	/// This is called when the creature dies, and removes
	/// it from the game.
	/// </summary>
	protected virtual void Die ()
	{
		if (goldCarried > 0) {
			GameObject goldObj = mapController.entities.InstantiateEntity (
				mapController.maps.GetPrefabByName ("Gold"),
				Location.Of (gameObject));
			goldObj.GetComponent<GoldController> ().goldAmount = goldCarried;
			goldCarried = 0;
		}

		mapController.entities.RemoveEntity (gameObject);
	}

	#endregion
}
