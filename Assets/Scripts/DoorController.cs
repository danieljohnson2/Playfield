using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Doors don't block movement, but they do block AI
/// pathfinding, and they look different when something
/// is standing on them.
/// </summary>
public class DoorController : MovementBlocker
{
	public Sprite door;
	public Sprite openDoor;
	private SpriteRenderer spriteRenderer;

	public DoorController ()
	{
		pathable = false;
		passable = true;
	}

	void Awake ()
	{
		this.spriteRenderer = GetComponent<SpriteRenderer> ();
	}

	void Update ()
	{
		bool open = isOpen;

		pathable = open;
		spriteRenderer.sprite = open ? openDoor : door;
	}

	/// <summary>
	/// This is true if there is any entity standing on
	/// the door's square; if true the door appears open.
	/// </summary>
	private bool isOpen {
		get {
			Location doorLoc = Location.Of (gameObject);
			return mapController.EntityObjectsAt (doorLoc).Any ();
		}
	}

	public override bool Block (GameObject mover)
	{
		Location here = Location.Of (gameObject);
		Map.Cell cell = mapController.GetMap (here.mapIndex) [here.x, here.y];

		if (cell.destinationMark != '\0' && cell.destinationMap!=null) {
			int mapIndex = mapController.FindMapIndex (cell.destinationMap);
			Map map = mapController.GetMap (mapIndex);

			List<Location> targets = new List<Location> ();
			map.ForEachMark (cell.destinationMark, (x,y) => targets.Add (new Location (x, y, mapIndex)));
			if (targets.Count > 0) {
				int index = Random.Range (0, targets.Count);
				mover.transform.position = targets [index].ToPosition ();
				return false;
			}
		}

		return base.Block (mover);
	}
}
