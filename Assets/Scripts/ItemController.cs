using UnityEngine;
using System.Collections;

public class ItemController : MovementBlocker
{
    /// <summary>
    /// This indicates which item is displayed in the
    /// creatures hand, if more than one are held. The
    /// highest priority item is shown..
    /// </summary>
    public int heldDisplayPriority = 0;

    /// <summary>
    /// If this item is shown in a creatures hand, it
    /// is scaled by this factor.
    /// </summary>
    public float scaleWhenHeld = 1.0f;

    public ItemController()
    {
        passable = true;
    }

    /// <summary>
    /// Pickup()  is called when a creature tries to pick up
    /// this item from the ground.
    /// </summary>
    protected virtual void Pickup(CreatureController carrier)
    {
        Location where = Location.Of(gameObject);

        foreach (var hpc in mapController.entities.Components<HeatmapPreferenceController>())
        {
            if (hpc.heatmapAutoReset)
            {
                if (hpc.AppliesHeatFor(gameObject))
                {
                    Location hpcWhere = Location.Of(hpc.gameObject);

                    if (hpcWhere.mapIndex == where.mapIndex)
                        hpc.ResetHeatmap();
                }
            }
        }

        carrier.AddTranscriptLine("{0} picked up {1}!", carrier.name, name);
        carrier.PlaceInInventory(gameObject);
    }

    /// <summary>
    /// TryGetCarrier() returns the createure that is carrying this
    /// item; if none is, then this method returns false.
    /// </summary>
    public bool TryGetCarrier(out CreatureController carrier)
    {
        if (Location.Of(gameObject) == Location.nowhere &&
            transform.parent != null)
        {
            carrier = transform.parent.GetComponent<CreatureController>();
            return carrier != null;
        }

        carrier = null;
        return false;
    }

    /// <summary>
    /// isHeldItem is true if this item is both carried by some creature, and
    /// is the item the creature shows in its hands.
    /// </summary>
    public bool isHeldItem
    {
        get
        {
            CreatureController cc;
            ItemController heldItem;
            return TryGetCarrier(out cc) && cc.TryGetHeldItem(out heldItem) && heldItem.gameObject == gameObject;
        }
    }

    public override bool Block(GameObject mover, Location destination)
    {
        var cc = mover.GetComponent<CreatureController>();
        if (cc != null)
            Pickup(cc);

        return true;
    }

    // TODO: comment this
    public virtual float GetHeatmapScalingFactor(GameObject mover)
    {
        return 1.0f;
    }
}