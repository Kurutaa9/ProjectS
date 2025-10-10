using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float damage;
    private List<GameObject> hitEnemies = new List<GameObject>();
    public bool canDamage= false;


    public void StartAttack()
    {
        hitEnemies.Clear();
        Debug.Log("Weapon: Starting new attack, cleared hitEnemies list");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Enemy" && !hitEnemies.Contains(other.gameObject) && canDamage)
        {
            other.gameObject.GetComponent<EnemyStatController>().TakeDamage(damage);
            hitEnemies.Add(other.gameObject);
        }
    }

}
