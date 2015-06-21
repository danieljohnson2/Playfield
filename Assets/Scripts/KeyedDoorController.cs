using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller handles a door that is opened with
/// a specific key. This door disappears when opened,
/// but blocks movement until then. This would normally
/// be placed on top of an ordinary door, which remains
/// behind (and can be a portal to another room).
/// </summary>
public class KeyedDoorController : MovementBlocker
{
	public string keyName;

	public override bool Block (GameObject mover, Location destination)
	{
		var cc = mover.GetComponent<CreatureController> ();

		if (cc.Inventory ().Any (go => go.name == keyName)) {
			mapController.entities.RemoveEntity (gameObject);
			AddTranscriptLine ("{0} opened a door.", gameObject.name);
			return false;
		}

		return base.Block (mover, destination);
	}
}