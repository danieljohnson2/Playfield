using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class JewelSlotController : MovementBlocker
{
    public Sprite[] filledSprites;
    private string filledColor = "";

    public JewelSlotController()
    {
        // Block() can still block movement, but this flag means
        // AI will at least try. We make this false again when the
        // slot is filled.
        passable = false;
    }
    public override bool IsPathableFor(GameObject mover)
    {
        // if a creature has a gem, it will path right to the
        // slot. Once the slot is full, this will then be false again.
        return CanBeOperatedBy(mover);
    }

    public override bool Block(GameObject mover, Location destination)
    {
        if (filledColor == "")
        {
            var cc = mover.GetComponent<CreatureController>();
            if (cc != null)
            {
                ItemController gem = cc.Inventory().FirstOrDefault(item => item.CompareTag("Gem"));

                if (gem != null)
                {
                    mapController.entities.ResetHeatmaps(gameObject);
                    mapController.adjacencyGenerator.InvalidatePathability(Location.Of(gameObject));

                    cc.DropItem(gem, makeActive: false);
                    mapController.entities.RemoveEntity(gem.gameObject);

                    filledColor = gem.name;
                    UpdateSprite();

                    IEnumerable<GameObject> allGems = mapController.entities.byTag["Gem"];
                    IEnumerable<GameObject> otherGems = allGems.Except(new[] { gem.gameObject });

                    if (!otherGems.Any())
                    {
                        IEnumerable<GameObject> allSlots = mapController.entities.byTag["JewelSlot"];

                        foreach (GameObject slot in allSlots)
                        {
                            mapController.entities.ResetHeatmaps(slot);
                            mapController.adjacencyGenerator.InvalidatePathability(Location.Of(slot));
                            mapController.entities.RemoveEntity(slot);
                        }
                    }
                }
            }
        }

        return false;
    }

    private bool CanBeOperatedBy(GameObject mover)
    {
        if (filledColor != "")
            return false;

        var cc = mover.GetComponent<CreatureController>();
        return cc != null && cc.Inventory().Any(item => item.tag == "Gem");
    }

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);
        writer.Write(filledColor);
    }

    public override void RestoreFrom(BinaryReader reader, Restoration restoration)
    {
        base.RestoreFrom(reader, restoration);
        filledColor = reader.ReadString();
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (filledColor != "")
        {
            Sprite newSprite = filledSprites.FirstOrDefault(s => s.name[0] == filledColor[0]);

            if (newSprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                passable = false;
            }
        }
    }
}