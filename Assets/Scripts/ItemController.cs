using UnityEngine;
using System.Collections;

public class ItemController : MovementBlocker
{
	/// <summary>
	/// This indicates which item is displayed in the
	/// creatures hand, if more than one are held. The
	/// highest priority item is shown..
	/// </summary>
	public int heldDisplayPriority = 0;

	/// <summary>
	/// If this item is shown in a creatures hand, it
	/// is scaled by this factor.
	/// </summary>
	public float scaleWhenHeld = 1.0f;

	public ItemController ()
	{
		passable = true;
	}
	
	public override bool Block (GameObject mover, Location destination)
	{
		var cc = mover.GetComponent<CreatureController> ();
		if (cc != null) {
			cc.AddTranscriptLine ("{0} picked up {1}!", mover.name, name);
			cc.PlaceInInventory (gameObject);
		}
		
		return true;
	}
}