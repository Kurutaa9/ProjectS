using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animEvents : MonoBehaviour
{
    public Weapon weapon;

    void Awake()
    {
        // Auto-find weapon if not assigned
        if (!weapon)
        {
            weapon = GetComponentInParent<Weapon>();
            if (!weapon) weapon = GetComponentInChildren<Weapon>();
        }
    }

    // Called by Animation Events on attack clips
    public void weaponCanDamageTrue()
    {
        if (!weapon) return;
        // Clear per-swing victims so each event window can hit once
        weapon.StartAttack();
        weapon.canDamage = true;
    }

    // Called by Animation Events on attack clips
    public void weaponCanDamageFalse()
    {
        if (!weapon) return;
        weapon.canDamage = false;
    }
}
