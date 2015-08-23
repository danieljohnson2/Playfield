using UnityEngine;
using System.Collections;

/// <summary>
/// This is the controller for items that improve your
/// attack damage; the actual ogica
/// </summary>
public class WeaponController : ItemController
{
	public DieRoll damage = new DieRoll ();
	
	/// <summary>
	/// This method computes the damage this weapon will do when
	/// it attacks the victim given.
	/// </summary>
	public virtual int GetAttackDamage (CreatureController attacker, CreatureController victim)
	{
		return damage.Roll ();
	}
}
