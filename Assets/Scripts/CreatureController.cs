using UnityEngine;
using UnityEngine.UI;
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
public class CreatureController : PlayableEntityController
{
    public int hitPoints = 10;
    public DieRoll damage = new DieRoll(1, 3);
    public bool canUseWeapons = true;
    public float weight = 10;
    public GameObject attackEffect;
    public bool teamAware = true;
    public bool bigBad = false;
    public Vector2 heldItemPivot = new Vector2(0.5f, 0.5f);

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);

        writer.Write(hitPoints);
    }

    public override void RestoreFrom(BinaryReader reader, Restoration restoration)
    {
        base.RestoreFrom(reader, restoration);

        hitPoints = reader.ReadInt32();
    }

    #region Creature Actions

    public override bool Block(GameObject mover, Location destination)
    {
        var attacker = mover.GetComponent<CreatureController>();

        if (attacker != null)
        {
            if (!teamAware || attacker.isPlayerControlled || !CompareTag(attacker.tag))
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
        int damage = attacker.GetAttackDamage();
        hitPoints -= damage;
        if (hitPoints <= 0)
        {
            hitPoints = 0;
            AddTranscriptLine("{0} killed {1}!", attacker.name, this.name);

            // If the player kills something that he can control, we record
            // it as activated. This will take effect even if the player
            // is later killed, or never saves.

            //if player kills something, that character is always unlocked
            if (attacker.isPlayerControlled && this.isPlayerCandidate)
                CharacterActivation.instance[name] = true;

            // if we the player is killed, we will always lock that character even if it's the last one
            // generally.

            if (this.isPlayerControlled)
            {
                CharacterActivation.instance[name] = false;
                mapController.GameOver();
            }

            attacker.hitPoints += 1;
            Die();

            if (bigBad)
            {
                transcript.AddLine("{0} killed the Rat King!", attacker.name);

                // A character that wins the game ceases to be available;
                // only if it is not the player. Player wins always remain available.
                // This is only in the rare event that AI can win the game while the player
                // is doing other things.

                if (!attacker.isPlayerControlled)
                    CharacterActivation.instance[attacker.name] = false;

                if (attacker.name != "Rat King") //the purpose of this test is to see if ALL the unlocks have been unlocked.
                                                 //asking if it's Rat King is just a silly way to make us keep respawning for the time being
                {
                    transcript.AddLine("Continue with a new character!");
                    Application.LoadLevel("Intro");
                }
                else
                {
                    transcript.AddLine("You won the game as {0}!", attacker.name);
                    mapController.GameOver();
                }
                //if all characters have been unlocked and you've just won with one of them,
                //overall game is won also and we go to GameOver.

            }
        }
        else
        {
            float myWeightScore = UnityEngine.Random.Range(weight / 2, weight);
            float attackerWeightScor = UnityEngine.Random.Range(attacker.weight / 2, attacker.weight);
            bool knockedBack =
                myWeightScore < attackerWeightScor &&
                KnockedBack(attacker);

            string message = string.Format("{0} hit {1} for {2}!{3}", attacker.name, this.name, damage,
                knockedBack ? " Knockback!" : "");

            if (damage > 0)
                AddTranscriptLine(message);
            else
                AddLocalTranscriptLine(message);
        }

        if (attacker.attackEffect != null && gameObject.activeInHierarchy)
        {
            float animationSize = (float)damage;
            animationSize = (float)0.3 + (animationSize / 7);
            if (hitPoints <= 0)
                animationSize *= 2;
            if (KnockedBack(attacker))
                animationSize *= 2;

            GameObject effect = Instantiate(attackEffect);
            effect.transform.parent = transform.parent;
            effect.transform.localScale = new Vector3(animationSize, animationSize, 1);
            effect.transform.localPosition = transform.localPosition;
        }
    }

    protected virtual bool KnockedBack(CreatureController attacker)
    {
        Location src = Location.Of(attacker.gameObject);
        Location here = Location.Of(gameObject);

        if (src.mapIndex == here.mapIndex)
        {
            int dx = Math.Max(Math.Min(here.x - src.x, 1), -1);
            int dy = Math.Max(Math.Min(here.y - src.y, 1), -1);

            if (dx != 0 || dy != 0)
            {
                Location dest = here.WithOffset(dx, dy);

                if (mapController.adjacencyGenerator.IsPathableFor(gameObject, dest))
                {
                    MoveTo(dest);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// This method computes the damage this create does when it attacks
    /// the victim given.
    /// </summary>
    public int GetAttackDamage()
    {
        int basicDamage = damage.Roll();

        if (canUseWeapons)
        {
            return
                (from item in Inventory().OfType<WeaponController>()
                 select item.GetAttackDamage(this)).
                 DefaultIfEmpty(basicDamage).
                 Max();
            //this is driving me crazy. Please fix.
            //It needs to roll only from the weapon in hand, not take the highest of all rolls of all weapons in inventory.
            //That defeats the purpose of constructing die rolls out of multiple dice, forces all the attacks to be super effective
            //from characters with many weapons, and worst of all I can't begin to solve this because there's no telling where it
            //does what it does. Virtual functions? selects? running GetAttackDamage on the entire contents of the collection?
            //I just tried to check the inventory for presence of things like Sword, to do a series of if statements in crude form
            //and therefore return first a Sword hit, then if no sword a Mace and so on, and I couldn't even do that.
            //Since we're not talking on the phone or teamspeak or something, I can only make a comment.
            //Please make this return only damage of weapon in hand. I can't even get autocomplete to show that parameter, but
            //weapon in hand priority IS one of the properties of the items, and I want to select by that.
        }
        return (int)(basicDamage * (weight / 20f));
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
        mapController.adjacencyGenerator.InvalidatePathability(here);

        if (isPlayerControlled)
        {
            UpdateStatusText();
            mapController.GameOver();
        }
    }

    protected override void UpdateStatusText()
    {
        transcript.SetPlayerStatus(string.Format("HP: {0}", hitPoints));
    }

    protected override float GetSpinnyness()
    {
        return 102 - Mathf.Pow(hitPoints, 2);
    }

    /// <summary>
    /// Places an item into this creature's inventory. It will
    /// disppear from the world.
    /// </summary>
    public void PlaceInInventory(GameObject item)
    {
        Location here = Location.Of(item.gameObject);

        item.SetActive(false); // make it vanish before it moves!

        if (HasItemInInventory(item.name))
        {
            mapController.entities.RemoveEntity(item);
        }
        else
        {
            item.transform.parent = transform;
            item.transform.localPosition = Location.nowhere.ToPosition();
        }

        mapController.adjacencyGenerator.InvalidatePathability(here);
        UpdateHeldItem();
    }

    /// <summary>
    /// This method causes this creature to drop the item; this
    /// does nothing if the item is not being carried by
    /// this creature.
    /// 
    /// We need to drop items before destoying them, so
    /// 'makeActive' can can set to false to avoid showing
    /// the item just because you have dropped it.
    /// </summary>
    public void DropItem(ItemController item, bool makeActive = true)
    {
        CreatureController carrier;
        if (item.TryGetCarrier(out carrier) && carrier == this)
        {
            Location here = Location.Of(gameObject);

            item.gameObject.SetActive(makeActive);
            item.transform.parent = transform.parent;
            item.transform.localPosition = here.ToPosition();

            mapController.adjacencyGenerator.InvalidatePathability(here);

            UpdateHeldItem();
        }
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

    /// <summary>
    /// HasItemInInventory() checks to see if this creature has
    /// an item in inventory by name.
    /// </summary>
    public bool HasItemInInventory(string itemName)
    {
        foreach (ItemController ic in Inventory())
        {
            if (ic.name == itemName)
                return true;
        }

        return false;
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

                SpriteRenderer sr = GetComponent<SpriteRenderer>();

                // we need evertyhing in world co-ordinates here, not pixels.
                Vector2 characterPivot = sr.sprite.pivot / sr.sprite.pixelsPerUnit;
                Vector3 characterSize = sr.sprite.bounds.size;

                float heldItemX = (heldItemPivot.x * characterSize.x) - characterPivot.x;
                float heldItemY = (heldItemPivot.y * characterSize.y) - characterPivot.y;
                heldItemDisplay.transform.localPosition = new Vector2(heldItemX, heldItemY);
            }

            SpriteRenderer heldItemSprite = heldItemDisplay.GetComponent<SpriteRenderer>();
            heldItemSprite.sprite = held;
            heldItemSprite.sortingOrder = 1000;
            heldItemDisplay.transform.localScale = new Vector2(-heldItem.scaleWhenHeld, heldItem.scaleWhenHeld);
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
