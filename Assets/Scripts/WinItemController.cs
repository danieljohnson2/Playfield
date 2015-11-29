using UnityEngine;
using System.Collections;
using System.Linq;

/// <summary>
/// WinItemController is an item control for 'win' items; a creature that
/// has all of these wins the game.
/// </summary>
public class WinItemController : ItemController
{
    protected override void Pickup(CreatureController carrier)
    {
        base.Pickup(carrier);

        if (mapController.entities.Components<WinItemController>().All(wi => wi.CarriedBy(carrier)))
        {
            transcript.AddLine("{0} wins the game!", carrier.name);
            mapController.GameOver();
        }
    }

    /// <summary>
    /// CarriedBy() is true if this item is carried by the 'winner';
    /// All 'win' items must be in thte same carrier's inventory for that creature to win.
    /// </summary>
    private bool CarriedBy(CreatureController winner)
    {
        CreatureController carrier;
        return TryGetCarrier(out carrier) && winner == carrier;
    }
}
