using UnityEngine;
using System.Collections;

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

	// Update is called once per frame
	void Update ()
	{
		bool open = isOpen;

		pathable = open;
		spriteRenderer.sprite = open ? openDoor : door;
	}

	public override bool Block (GameObject mover)
	{
		return true;
	}

	private bool isOpen {
		get {
			Location doorLoc = Location.Of (transform);

			foreach (GameObject obj in mapController.EntityObjects()) {
				if (doorLoc == Location.Of (obj)) {
					return true;
				}
			}

			return false;
		}
	}
}
