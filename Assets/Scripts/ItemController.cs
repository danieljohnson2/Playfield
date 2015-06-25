using UnityEngine;
using System.Collections;

public class ItemController : MovementBlocker
{
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