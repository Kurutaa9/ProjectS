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

    public TMP_Text solsAmountText;
    public TMP_Text flaskAmountText;

    // void Start(){
    //     updateUI();
    // }

    void Update(){
        updateUI();
        Debug.Log("current sols: " + baseStats.currentSolsSO);
        Debug.Log("current health: " + baseStats.maxHealth);
        Debug.Log("current stamina: " + baseStats.maxStamina);
        Debug.Log("current flask: " + baseStats.maxFlasks);
        Debug.Log("current damage: " + baseStats.baseDamage);

    }

    public void upgradeHealth()
    {   
        Debug.Log("UPGRADEHEALTH PRESSED!");
        if (baseStats.currentSolsSO < baseStats.healthLevel * 1000) return;
        Debug.Log("UPGRADEHEALTH INSIDE!");
        baseStats.healthLevel += 1;
        Debug.Log("health level upgraded to " + baseStats.healthLevel);
        baseStats.currentSolsSO -= baseStats.healthLevel * 1000;
        updateUI();
    }

    public void upgradeStamina()
    {
        if (baseStats.currentSolsSO < baseStats.staminaLevel * 1000) return;

        baseStats.staminaLevel += 1;
        Debug.Log("stamina level upgraded to " + baseStats.staminaLevel);
        baseStats.currentSolsSO -= baseStats.staminaLevel * 1000;
        updateUI();
    }

    public void upgradeFlask()
    {
        if (baseStats.currentSolsSO < baseStats.flaskLevel * 1000) return;

        baseStats.flaskLevel += 1;
        Debug.Log("flask level upgraded to " + baseStats.flaskLevel);
        baseStats.currentSolsSO -= baseStats.flaskLevel * 1000;
        updateUI();
    }

    public void upgradeDamage()
    {
        if (baseStats.currentSolsSO < baseStats.damageLevel * 1000) return;

        baseStats.damageLevel += 1;
        Debug.Log("damage level upgraded to " + baseStats.damageLevel);
        baseStats.currentSolsSO -= baseStats.damageLevel * 1000;
        updateUI();
    }

    void updateUI()
    {
        Debug.Log("UI updated!");
        healthLevelText.text = baseStats.healthLevel.ToString();
        staminaLevelText.text = baseStats.staminaLevel.ToString();
        flaskLevelText.text = baseStats.flaskLevel.ToString();
        damageLevelText.text = baseStats.damageLevel.ToString();

        solsAmountText.text = baseStats.currentSolsSO.ToString();
        flaskAmountText.text = baseStats.maxFlasks.ToString();
    }
}
