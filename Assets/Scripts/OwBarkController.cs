using UnityEngine;
using System.Collections;

/// <summary>
/// This controller plays a bark when the HP of the
/// creature drops.
/// </summary>
public class OwBarkController : BarkController
{
	private int lastKnownHitPoints;
	private CreatureController creatureController;

	public void Awake ()
	{
		creatureController = GetComponent<CreatureController> ();
	}

	public void Start ()
	{
		if (creatureController != null) {
			lastKnownHitPoints = creatureController.hitPoints;
		}
	}

	protected override bool CheckShouldBark ()
	{
		if (!base.CheckShouldBark ()) {
			return false;
		}

		if (creatureController == null) {
			return false;
		}

		if (creatureController.hitPoints < lastKnownHitPoints) {
			lastKnownHitPoints = creatureController.hitPoints;
			return creatureController.hitPoints > 0;
		}

		return false;
	}
}
