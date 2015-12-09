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

	public KeyedDoorController() {
		// creatures will try to step into the door, but
		// Block() makes the final call.
		this.passable = true;
	}

    public override bool? pathable
    {
        get { return null; }
    }

    public override bool IsPathableFor (GameObject mover)
	{
		// if a creature has the key, it will path right
		// through the door. When it tries to move in, it will
		// opent eh door.
		return CanBeOpenedBy (mover);
	}

	public override bool Block (GameObject mover, Location destination)
	{
		if (CanBeOpenedBy (mover)) {
			mapController.entities.RemoveEntity (gameObject);
			AddTranscriptLine ("{0} opened a door.", mover.name);
			return false;
		}

		return false;
	}

	private bool CanBeOpenedBy (GameObject mover)
	{
		var cc = mover.GetComponent<CreatureController> ();
		return cc != null && cc.Inventory ().Any (item => item.name == keyName);
	}
}