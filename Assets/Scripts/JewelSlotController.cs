﻿using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class JewelSlotController : MovementBlocker
{
    public Sprite[] filledSprites;
    private string filledColor = "";

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
                    cc.DropItem(gem, makeActive: false);
                    mapController.entities.RemoveEntity(gem.gameObject);

                    filledColor = gem.name;
                    UpdateSprite();

                    IEnumerable<GameObject> allGems = mapController.entities.byTag["Gem"];
                    IEnumerable<GameObject> otherGems = allGems.Except(new[] { gem.gameObject });

                    if (otherGems.Count() <= 3)
                    {
                        IEnumerable<GameObject> allSlots = mapController.entities.byTag["JewelSlot"];

                        foreach (GameObject slot in allSlots)
                            mapController.entities.RemoveEntity(slot);
                    }
                }
            }
        }

        return false;
    }

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);
        writer.Write(filledColor);
    }

    public override void RestoreFrom(BinaryReader reader)
    {
        base.RestoreFrom(reader);
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
            }
        }
    }
}