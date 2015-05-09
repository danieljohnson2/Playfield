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
		if (mover == null)
			throw new System.ArgumentNullException ("mover");
		
		if (mapController == null)
			throw new System.InvalidOperationException ("Map Controller missing.");

		Location here = Location.Of (gameObject);
		Map hereMap = mapController.GetMap (here.mapIndex);
		Map.Cell cell = hereMap [here.x, here.y];

		if (cell.destinationMark != '\0' && cell.destinationMap != null) {
			int mapIndex = mapController.FindMapIndex (cell.destinationMap);
			Map map = mapController.GetMap (mapIndex);

			Location[] targets = map.FindMarks (cell.destinationMark).ToArray ();
			if (targets.Length > 0) {
				int index = Random.Range (0, targets.Length);
				mover.transform.position = targets [index].ToPosition ();
				return false;
			}
		}

		return base.Block (mover);
	}
}
