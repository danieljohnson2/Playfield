using UnityEngine;
using System.Collections;

/// <summary>
/// This controller implements the behavior where creatures
/// can pick up gold.
/// </summary>
public class GoldController : MovementBlocker
{
	public int goldAmount = 1;

	public GoldController ()
	{
		passable = true;
		pathable = true;
	}

	public override bool Block (GameObject mover)
	{
		var cc = mover.GetComponent<CreatureController> ();
		if (cc != null) {
			cc.AddTranscriptLine ("{0} picked up {1} gold!", mover.name, goldAmount);
			cc.goldCarried += goldAmount;
			goldAmount = 0;

			mapController.entities.RemoveEntity (gameObject);
		}

		return true;
	}
}
