using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// CreatureController provides the base class for things that take
/// action in the world - the player, NPCs, monsters. The MapController
/// invokes the DoTurnAsync() in a co-routine, let us take turns.
/// 
/// By default, creatures are passable and don't block pathing,
/// but when one moves into another the Block() method usually
/// prevents movement. We stil have 'passable=true' so that NPCs
/// will be willing to try to move into the creature's square;
/// if it's false they'll avoid doing this.
/// </summary>
public class CreatureController : MovementBlocker
{
    public int hitPoints = 10;
    public DieRoll damage = new DieRoll(1, 3);
    public bool canUseWeapons = true;
    public float speed = 1;
    public GameObject attackEffect;
    public bool teamAware = true;
    public Vector2 heldItemPivot = new Vector2(0.5f, 0.5f);
    private float maxSpeed = 20.0f;
    private float turnCounter = 0;

    public CreatureController()
    {
        this.passable = true;
    }

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);

        writer.Write(turnCounter);
        writer.Write(hitPoints);
    }

    public override void RestoreFrom(BinaryReader reader)
    {
        base.RestoreFrom(reader);

        turnCounter = reader.ReadSingle();
        hitPoints = reader.ReadInt32();
    }

    /// <summary>
    /// This method decides if the creatures turn has
    /// arrived; we reduce the turnCounter until it goes 0
    /// or negative, and then its this creature's turn. By
    /// having a larger or smaller speed, turns will come up
    /// more or less often.
    /// </summary>
    public virtual bool CheckTurn()
    {
        if (turnCounter <= float.Epsilon)
        {
            turnCounter += maxSpeed;
            return true;
        }
        else
        {
            turnCounter -= speed;
            return false;
        }
    }

    /// <summary>
    /// This is the entry point used to start the
    /// creatures turn. The next creature's turn
    /// begins only when this one ends, but in most
    /// cases aren't really much of a co-routine; this
    /// method calls DoTurn(), then ends the turn
    /// synchronously.
    /// </summary>
    public virtual IEnumerator DoTurnAsync()
    {
        DoTurn();
        return Enumerable.Empty<object>().GetEnumerator();
    }

    /// <summary>
    /// This is a sychrnonous entry point; you override this
    /// and do whatever the creature should do during its turn.
    /// </summary>
    protected virtual void DoTurn()
    {
    }

    #region Creature Actions

    public override bool Block(GameObject mover, Location destination)
    {
        var attacker = mover.GetComponent<CreatureController>();

        if (attacker != null)
        {
            if (!teamAware || !CompareTag(attacker.tag))
            {
                Fight(attacker);
            }
        }

        return false;
    }

    /// <summary>
    /// This method handles combat where 'attacker' attacks this
    /// creature.
    /// </summary>
    protected virtual void Fight(CreatureController attacker)
    {
        if (attacker.attackEffect != null && gameObject.activeSelf)
        {
            GameObject effect = Instantiate(attackEffect);
            effect.transform.parent = transform.parent;
            effect.transform.localPosition = transform.localPosition;
        }

        int damage = attacker.GetAttackDamage(this);
        hitPoints -= damage;

        if (hitPoints <= 0)
        {
            hitPoints = 0;
            AddTranscriptLine("{0} killed {1}!", attacker.name, this.name);
            Die();
        }
        else
        {
            AddTranscriptLine("{0} hit {1} for {2}!", attacker.name, this.name, damage);
        }
    }

    /// <summary>
    /// This method computes the damage this create does when it attacks
    /// the victim given.
    /// </summary>
    protected virtual int GetAttackDamage(CreatureController victim)
    {
        int basicDamage = damage.Roll();

        if (canUseWeapons)
        {
            return
                (from item in Inventory().OfType<WeaponController>()
                 select item.GetAttackDamage(this, victim)).
                DefaultIfEmpty(basicDamage).
                Max();
        }

        return basicDamage;
    }

    /// <summary>
    /// This method moves the creature by the delta indicated
    /// within the current map. This executes Block() methods
    /// and may fail; if the creature could not move this method
    /// returns false. If it did move it returns true.
    /// </summary>
    protected bool Move(int dx, int dy)
    {
        Location loc = Location.Of(gameObject).WithOffset(dx, dy);
        return MoveTo(loc);
    }

    /// <summary>
    /// This method moves the creature to a specific location,
    /// which could be on a different map. Like Move(), this runs
    /// Block() methods and returns false if the movement is blocked,
    /// true if it succeeds. 
    /// </summary>
    protected bool MoveTo(Location destination)
    {
        if (mapController.terrain.GetTerrain(destination) == null)
        {
            return false;
        }

        foreach (var blocker in mapController.ComponentsInCell<MovementBlocker>(destination).Reverse())
        {
            if (!blocker.Block(gameObject, destination))
                return false;
        }

        transform.localPosition = destination.ToPosition();
        return true;
    }

    /// <summary>
    /// This is called when the creature dies, and removes
    /// it from the game.
    /// </summary>
    protected virtual void Die()
    {
        Location here = Location.Of(gameObject);

        ItemController[] inventory = Inventory().ToArray();
        foreach (ItemController child in inventory)
        {
            child.transform.parent = transform.parent;
            child.transform.localPosition = here.ToPosition();
            child.gameObject.SetActive(true);
        }

        mapController.entities.RemoveEntity(gameObject);
    }

    /// <summary>
    /// Places an item into this creature's inventory. It will
    /// disppear from the world.
    /// </summary>
    public void PlaceInInventory(GameObject item)
    {
        item.SetActive(false); // make it vanish before it moves!
        item.transform.parent = transform;
        item.transform.localPosition = Location.nowhere.ToPosition();
        mapController.adjacencyGenerator.InvalidatePathability(gameObject);

        UpdateHeldItem();
    }

    /// <summary>
    /// This yields each object in the inventory of this creature. Only
    /// items that have item controllers are returned, and as a convenience
    /// we actually return thecontroller itself.
    /// </summary>
    public IEnumerable<ItemController> Inventory()
    {
        return
            from i in Enumerable.Range(0, transform.childCount)
            select transform.GetChild(i).GetComponent<ItemController>() into item
            where item != null
            select item;
    }

    private GameObject heldItemDisplay;

    /// <summary>
    /// This method works out what sprite to shown in this
    /// creatures hands, and displays it. If no sprite should be
    /// shown, but one is displayed, that sprite is removed.
    /// </summary>
    public void UpdateHeldItem()
    {
        Sprite held;
        ItemController heldItem;
        if (TryPickHeldItemSprite(out held, out heldItem))
        {
            if (heldItemDisplay == null)
            {
                heldItemDisplay = new GameObject("Held Item Sprite", typeof(SpriteRenderer));
                heldItemDisplay.transform.parent = transform;
                heldItemDisplay.transform.localPosition = heldItemPivot - new Vector2(0.5f, 0.5f);
            }

            SpriteRenderer heldItemSprite = heldItemDisplay.GetComponent<SpriteRenderer>();
            heldItemSprite.sprite = held;
            heldItemSprite.sortingOrder = 1000;
            heldItemDisplay.transform.localScale = new Vector2(heldItem.scaleWhenHeld, heldItem.scaleWhenHeld);
        }
        else if (heldItemDisplay != null)
        {
            Destroy(heldItemDisplay);
            heldItemDisplay = null;
        }
    }

    /// <summary>
    /// TryGetHeldItem() obtains the item the creature is visibly
    /// holding in its hand (or paw). Only items that have sprites
    /// can be held in this way.
    /// </summary>
    public bool TryGetHeldItem(out ItemController heldItem)
    {
        Sprite sprite;
        return TryPickHeldItemSprite(out sprite, out heldItem);
    }

    /// <summary>
    /// This decides which item held by this creature should be shown;
    /// it supplies the sprite and the scaling that should be applied to this
    /// sprite.
    /// 
    /// This method returns false to indicate no sprite should be shown at all.
    /// </summary>
    private bool TryPickHeldItemSprite(out Sprite sprite, out ItemController heldItem)
    {
        var found =
            (from item in Inventory()
             let sr = item.GetComponent<SpriteRenderer>()
             where sr != null && sr.sprite != null
             orderby item.heldDisplayPriority descending
             select new { sr.sprite, item }).FirstOrDefault();

        if (found != null)
        {
            sprite = found.sprite;
            heldItem = found.item;
            return true;
        }
        else
        {
            sprite = null;
            heldItem = null;
            return false;
        }
    }

    #endregion
}
