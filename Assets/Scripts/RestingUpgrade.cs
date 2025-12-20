using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RestingUpgrade : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO baseStats;

    public TMP_Text healthLevelText;
    public TMP_Text staminaLevelText;
    public TMP_Text flaskLevelText;
    public TMP_Text damageLevelText;

    public void upgradeHealth()
    {
        if (baseStats.currentSolsSO < baseStats.healthLevel * 1000) return;

        baseStats.healthLevel += 1;
        Debug.Log("health level upgraded to " + baseStats.healthLevel);
        updateUI();
    }

    public void upgradeStamina()
    {
        if (baseStats.currentSolsSO < baseStats.staminaLevel * 1000) return;

        baseStats.staminaLevel += 1;
        Debug.Log("stamina level upgraded to " + baseStats.staminaLevel);
        updateUI();
    }

    public void upgradeFlask()
    {
        if (baseStats.currentSolsSO < baseStats.flaskLevel * 1000) return;

        baseStats.flaskLevel += 1;
        Debug.Log("flask level upgraded to " + baseStats.flaskLevel);
        updateUI();
    }

    public void upgradeDamage()
    {
        if (baseStats.currentSolsSO < baseStats.damageLevel * 1000) return;

        baseStats.damageLevel += 1;
        Debug.Log("damage level upgraded to " + baseStats.damageLevel);
        updateUI();
    }

    void updateUI()
    {
        Debug.Log("UI updated!");
        healthLevelText.text = baseStats.healthLevel.ToString();
        staminaLevelText.text = baseStats.staminaLevel.ToString();
        flaskLevelText.text = baseStats.flaskLevel.ToString();
        damageLevelText.text = baseStats.damageLevel.ToString();
    }
}
