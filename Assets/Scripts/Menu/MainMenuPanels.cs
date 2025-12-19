using System.Collections;
using UnityEngine;

public class MainMenuPanels : MonoBehaviour
{
    float inputLockUntil = 0f;
    public float inputLockSeconds = 0.15f;
    public bool InputLocked => Time.unscaledTime < inputLockUntil;
    public GameObject mainMenuPanel;
    public GameObject optionsMenuPanel;

    public MainMenuController mainMenu;
    public OptionsMenuController optionsMenu;

    public RectTransform selectionBar;

    public Transform mainMenuPanelTransform;
    public Transform optionsMenuPanelTransform;

    // Drag the MenuGroup objects for correct layering
    public Transform mainMenuMenuGroup;
    public Transform optionsMenuMenuGroup;


    public void OpenOptions()  => StartCoroutine(OpenOptionsCo());
    public void CloseOptions() => StartCoroutine(CloseOptionsCo());

    void PlaceBar(Transform panel, Transform menuGroup)
    {
        if (!selectionBar || !panel || !menuGroup) return;

        selectionBar.SetParent(panel, worldPositionStays: true);

        // Put the bar just BEFORE MenuGroup so it renders behind the text
        selectionBar.SetSiblingIndex(menuGroup.GetSiblingIndex());
    }


    IEnumerator OpenOptionsCo()
    {
        inputLockUntil = Time.unscaledTime + inputLockSeconds;

        if (mainMenu)
        {
            mainMenu.DisableInput();
            mainMenu.enabled = false;
        }
        if (mainMenuPanel) mainMenuPanel.SetActive(false);

        yield return null;

        if (optionsMenuPanel) optionsMenuPanel.SetActive(true);
        PlaceBar(optionsMenuPanelTransform, optionsMenuMenuGroup);

        if (optionsMenu)
        {
            optionsMenu.enabled = true;
            optionsMenu.EnableInput();
        }
    }



    IEnumerator CloseOptionsCo()
    {
        inputLockUntil = Time.unscaledTime + inputLockSeconds;
        if (optionsMenu)
        {
            optionsMenu.DisableInput();
            optionsMenu.enabled = false;
        }

        if (optionsMenuPanel) optionsMenuPanel.SetActive(false);

        yield return null; // 1 frame

        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        PlaceBar(mainMenuPanelTransform, mainMenuMenuGroup);
        if (mainMenu)
        {
            mainMenu.enabled = true;
            mainMenu.EnableInput();
        }
    }
}
