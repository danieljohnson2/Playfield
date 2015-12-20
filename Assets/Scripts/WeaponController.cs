using UnityEngine;
using System.Collections;

/// <summary>
/// This is the controller for items that improve your
/// attack damage; the actual ogica
/// </summary>
public class WeaponController : ItemController
{
    public DieRoll damage = new DieRoll();

    /// <summary>
    /// This method computes the damage this weapon will do when
    /// it attacks the victim given.
    /// </summary>
    public virtual int GetAttackDamage(CreatureController attacker)
    {
        return damage.Roll();
    }

    public override float GetHeatmapScalingFactor(GameObject mover)
    {
        CreatureController cc = mover.GetComponent<CreatureController>();

        if (cc != null)
        {
            int myDamage = damage.Roll();
            int moverDamage = cc.GetAttackDamage();

            return (moverDamage - myDamage);// / 10.0f;
        }

        return base.GetHeatmapScalingFactor(mover);
    }
}
