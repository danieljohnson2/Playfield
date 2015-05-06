using UnityEngine;
using System.Collections;

public class GoldController : MovementBlocker
{
	public GoldController ()
	{
		passable = true;
		pathable = true;
	}

	public override bool Block (GameObject mover)
	{
		if (mover.GetComponent<CreatureController> () != null) {
			mapController.RemoveEntity (gameObject);
		}

		return true;
	}
}
