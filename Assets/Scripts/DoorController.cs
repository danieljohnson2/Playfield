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

    public DoorController()
    {
        // Unkeyed doors are more decoration than obstacle!
        passable = true;
    }

    void Awake()
    {
        this.spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (openDoor == door)
            spriteRenderer.sprite = door;
        else if (isOpen)
            spriteRenderer.sprite = openDoor;
        else
            spriteRenderer.sprite = door;
    }

    /// <summary>
    /// This is true if there is any entity standing on
    /// the door's square; if true the door appears open.
    /// </summary>
    private bool isOpen
    {
        get
        {
            return mapController.entities.
                ComponentsAt<CreatureController>(Location.Of(gameObject)).
                Any();
        }
    }

    public override MoveEffect Block(GameObject mover, Location destination)
    {
        if (mover == null)
            throw new System.ArgumentNullException("mover");

        Map.Cell cell = mapController.maps[destination];

        Location doorExit;
        if (mapController.maps.TryFindDestination(cell, destination, out doorExit))
        {
            mover.transform.localPosition = doorExit.ToLocalPosition();
            mapController.entities.ActivateEntities();
            mapController.entities.ActivateMapContainers();
            return MoveEffect.Action; // this is not a normal movement, but more like a teleportation.
        }

        return base.Block(mover, destination);
    }
}
