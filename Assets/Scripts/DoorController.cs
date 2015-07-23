﻿using UnityEngine;
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
	public GameObject attackEffect;
	public DoorController ()
	{
		// Unkeyed doors are more decoration than obstacle!
		passable = true;
	}

	void Awake ()
	{
		this.spriteRenderer = GetComponent<SpriteRenderer> ();
	}

	void Update ()
	{
		Sprite newSprite = isOpen ? openDoor : door;

		if (newSprite != null)
			spriteRenderer.sprite = newSprite;
	}

	/// <summary>
	/// This is true if there is any entity standing on
	/// the door's square; if true the door appears open.
	/// </summary>
	private bool isOpen {
		get {
			return mapController.entities.
				ComponentsAt<CreatureController> (Location.Of (gameObject)).
				Any ();
		}
	}

	public override bool Block (GameObject mover, Location destination)
	{
		if (mover == null)
			throw new System.ArgumentNullException ("mover");

		Map.Cell cell = mapController.maps [destination];

		Location doorExit;
		if (mapController.maps.TryFindDestination (cell, out doorExit)) {
			mover.transform.position = doorExit.ToPosition ();
			mapController.entities.ActivateEntities ();
			mapController.entities.ActivateMapContainers ();
			if (attackEffect != null && gameObject.activeSelf) {
				GameObject effect = Instantiate (attackEffect);
				effect.transform.parent = mover.transform.parent;
				effect.transform.localPosition = mover.transform.localPosition;
			}
			return false;
		}

		return base.Block (mover, destination);
	}
}
