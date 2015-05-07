using UnityEngine;
using System.Collections;
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
			Location doorLoc = mapController.GetLocation (gameObject);
			return mapController.EntityObjectsAt (doorLoc).Any ();
		}
	}
}
