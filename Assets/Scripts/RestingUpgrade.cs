using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Input System")]
    public InputActionReference navigateAction;
    public InputActionReference submitAction;

    [Header("Controller Settings")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;
    private int selectedIndex = 0;
    private bool isAxisInUse = false;

    private void OnEnable()
    {
        if (navigateAction != null && navigateAction.action != null) navigateAction.action.Enable();
        if (submitAction != null && submitAction.action != null) submitAction.action.Enable();
    }

    private void OnDisable()
    {
        if (navigateAction != null && navigateAction.action != null) navigateAction.action.Disable();
        if (submitAction != null && submitAction.action != null) submitAction.action.Disable();
    }

    void Start()
    {
        if (baseStats == null) Debug.LogError("RestingUpgrade: BaseStats is not assigned!");
        if (solsAmountText == null) 
        {
            Debug.LogWarning("RestingUpgrade: SolsAmountText is not assigned!");
        }
        else
        {
            // Ensure the text object is active and rendered on top of its siblings (backgrounds)
            solsAmountText.gameObject.SetActive(true);
            solsAmountText.transform.SetAsLastSibling();
        }

        if (flaskAmountText == null) 
        {
            Debug.LogWarning("RestingUpgrade: FlaskAmountText is not assigned!");
        }
        else
        {
            flaskAmountText.gameObject.SetActive(true);
            flaskAmountText.transform.SetAsLastSibling();
        }
        
        updateUI();
    }

    void Update(){
        HandleInput();
        updateUI();
        Debug.Log("current sols: " + baseStats.currentSolsSO);
        Debug.Log("current health: " + baseStats.maxHealth);
        Debug.Log("current stamina: " + baseStats.maxStamina);
        Debug.Log("current flask: " + baseStats.maxFlasks);
        Debug.Log("current damage: " + baseStats.baseDamage);

    }

    void HandleInput()
    {
        float v = 0f;
        bool submit = false;

        // Try New Input System
        if (navigateAction != null && navigateAction.action != null)
        {
            v = navigateAction.action.ReadValue<Vector2>().y;
        }
        else
        {
            // Fallback to Legacy
            v = Input.GetAxisRaw("Vertical");
        }

        // Try New Input System Submit
        if (submitAction != null && submitAction.action != null)
        {
            if (submitAction.action.WasPressedThisFrame()) submit = true;
        }
        else
        {
            // Fallback to Legacy Submit
            if (Input.GetButtonDown("Submit") || Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Return)) submit = true;
        }

        if (Mathf.Abs(v) > 0.5f)
        {
            if (!isAxisInUse)
            {
                if (v < 0)
                {
                    selectedIndex++;
                    if (selectedIndex > 3) selectedIndex = 0;
                }
                else if (v > 0)
                {
                    selectedIndex--;
                    if (selectedIndex < 0) selectedIndex = 3;
                }
                isAxisInUse = true;
            }
        }
        else
        {
            isAxisInUse = false;
        }

        if (submit)
        {
            switch (selectedIndex)
            {
                case 0: upgradeHealth(); break;
                case 1: upgradeStamina(); break;
                case 2: upgradeFlask(); break;
                case 3: upgradeDamage(); break;
            }
        }
    }

    public void upgradeHealth()
    {   
        Debug.Log("UPGRADEHEALTH PRESSED!");
        int cost = baseStats.healthLevel * 1000;
        if (baseStats.currentSolsSO < cost) return;
        
        Debug.Log("UPGRADEHEALTH INSIDE!");
        baseStats.currentSolsSO -= cost;
        baseStats.healthLevel += 1;
        Debug.Log("health level upgraded to " + baseStats.healthLevel);
        updateUI();
    }

    public void upgradeStamina()
    {
        int cost = baseStats.staminaLevel * 1000;
        if (baseStats.currentSolsSO < cost) return;

        baseStats.currentSolsSO -= cost;
        baseStats.staminaLevel += 1;
        Debug.Log("stamina level upgraded to " + baseStats.staminaLevel);
        updateUI();
    }

    public void upgradeFlask()
    {
        int cost = baseStats.flaskLevel * 1000;
        if (baseStats.currentSolsSO < cost) return;

        baseStats.currentSolsSO -= cost;
        baseStats.flaskLevel += 1;
        Debug.Log("flask level upgraded to " + baseStats.flaskLevel);
        updateUI();
    }

    public void upgradeDamage()
    {
        int cost = baseStats.damageLevel * 1000;
        if (baseStats.currentSolsSO < cost) return;

        baseStats.currentSolsSO -= cost;
        baseStats.damageLevel += 1;
        Debug.Log("damage level upgraded to " + baseStats.damageLevel);
        updateUI();
    }

    void updateUI()
    {
        // Debug.Log("UI updated!");
        if (baseStats == null) return;

        if (healthLevelText != null) 
        {
            healthLevelText.text = baseStats.healthLevel.ToString();
            healthLevelText.color = (selectedIndex == 0) ? selectedColor : normalColor;
        }

        if (staminaLevelText != null) 
        {
            staminaLevelText.text = baseStats.staminaLevel.ToString();
            staminaLevelText.color = (selectedIndex == 1) ? selectedColor : normalColor;
        }

        if (flaskLevelText != null) 
        {
            flaskLevelText.text = baseStats.flaskLevel.ToString();
            flaskLevelText.color = (selectedIndex == 2) ? selectedColor : normalColor;
        }

        if (damageLevelText != null) 
        {
            damageLevelText.text = baseStats.damageLevel.ToString();
            damageLevelText.color = (selectedIndex == 3) ? selectedColor : normalColor;
        }

        if (solsAmountText != null) 
        {
            solsAmountText.text = baseStats.currentSolsSO.ToString();
            solsAmountText.color = normalColor;
        }
        if (flaskAmountText != null) 
        {
            flaskAmountText.text = baseStats.maxFlasks.ToString();
            flaskAmountText.color = normalColor;
        }
    }
}
