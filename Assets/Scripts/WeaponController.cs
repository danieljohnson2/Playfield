﻿using UnityEngine;
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
            // TODO: fix this. Not clear but I think this is supposed to make
            // the heatmap more intensse dependingon the relative strength
            // of the mover vs the weapon.

            int myDamage = damage.Roll();
            int moverDamage = cc.GetAttackDamage();
			float enemyBrave = (float)(cc.hitPoints)/20f;
			//purpose of this is to make the AIs more cowardly the fewer hit points they have.
			if (enemyBrave < 0.5f) enemyBrave = 0.5f;
			//sanity check in case we're given hit points less than zero upon a kill
			//increasing this value makes them more combative: 10 was very fighty, 1 relatively unfighty
			return 1; //(moverDamage + myDamage) / enemyBrave;
        }

        return base.GetHeatmapScalingFactor(mover);
    }
}
