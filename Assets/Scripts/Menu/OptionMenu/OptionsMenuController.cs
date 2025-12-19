using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsMenuController : MonoBehaviour
{
    public enum OptionAction { Resolution, Fullscreen, Volume, Back }

    [System.Serializable]
    public class Entry
    {
        public MenuEntryUI item;      // reuse your MenuEntryUI for coloring
        public OptionAction action;
    }

    [Header("Entries (top to bottom)")]
    public List<Entry> entries = new();

    [Header("Selection Glow")]
    public Image selectionBar;
    public Vector2 barPadding = new Vector2(120f, 18f);
    public float moveSpeed = 18f;

    [Header("Input System")]
    public InputActionReference navigate; // Vector2 (up/down)
    public InputActionReference submit;   // Button

    [Header("Back")]
    public MainMenuPanels panels; // your OpenOptions/CloseOptions script

    int index = 0;
    RectTransform barRect;
    RectTransform targetRect;

    void EnsureMenuActionsEnabled()
    {
        if (navigate != null && navigate.action != null)
            navigate.action.actionMap.Enable();

        if (submit != null && submit.action != null)
            submit.action.actionMap.Enable();
    }   

    void Awake()
    {
        EnsureMenuActionsEnabled();
    }


    void OnEnable()
    {
        if (selectionBar) barRect = selectionBar.rectTransform;

        if (navigate?.action != null)
        {
            navigate.action.Enable();   
            navigate.action.performed += OnNavigate;
        }
        if (submit?.action != null)
        {
            submit.action.Enable();    
            submit.action.performed += OnSubmit;
        }

        index = Mathf.Clamp(index, 0, entries.Count - 1);
        ApplySelection(true);
    }

    void OnDisable()
    {
        if (navigate?.action != null)
        {
            navigate.action.performed -= OnNavigate;
            navigate.action.Disable();  
        }
        if (submit?.action != null)
        {
            submit.action.performed -= OnSubmit;
            submit.action.Disable();
        }
    }

    void Update()
    {
        if (barRect && targetRect)
        {
            barRect.position = Vector3.Lerp(barRect.position, targetRect.position, Time.unscaledDeltaTime * moveSpeed);

            float w = targetRect.rect.width + barPadding.x;
            float h = targetRect.rect.height + barPadding.y;
            barRect.sizeDelta = Vector2.Lerp(barRect.sizeDelta, new Vector2(w, h), Time.unscaledDeltaTime * moveSpeed);
        }
    }

    void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (panels != null && panels.InputLocked) return;
        Vector2 v = ctx.ReadValue<Vector2>();
        if (v.y > 0.5f) Move(-1);
        else if (v.y < -0.5f) Move(+1);
    }
    System.Collections.IEnumerator CloseOptionsNextFrame()
    {
        yield return null;
        if (panels) panels.CloseOptions();
    }


    void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (panels != null && panels.InputLocked) return;
        switch (entries[index].action)
        {
            case OptionAction.Back:
            Debug.Log("BACK PRESSED");
            StartCoroutine(CloseOptionsNextFrame());
            break;

            // we’ll implement these next:
            case OptionAction.Resolution:
            case OptionAction.Fullscreen:
            case OptionAction.Volume:
                Debug.Log("Change this option (we'll add left/right soon).");
                break;
        }
    }

    void Move(int delta)
    {
        if (entries.Count == 0) return;

        entries[index].item?.SetSelected(false);

        index = (index + delta) % entries.Count;
        if (index < 0) index += entries.Count;

        ApplySelection(false);
    }

    public void DisableInput()
    {
        if (navigate?.action != null) navigate.action.Disable();
        if (submit?.action != null) submit.action.Disable();
    }

    public void EnableInput()
    {
        if (navigate?.action != null) navigate.action.Enable();
        if (submit?.action != null) submit.action.Enable();
    }


    void ApplySelection(bool instant)
    {
        // clear previous selection visuals (optional but recommended)
        for (int i = 0; i < entries.Count; i++)
            entries[i].item?.SetSelected(i == index);

        // Prefer the parent row rect (covers label + value), fallback to self rect
        RectTransform selfRect = entries[index].item
            ? entries[index].item.GetComponent<RectTransform>()
            : null;

        RectTransform rowRect = entries[index].item
            ? entries[index].item.transform.parent as RectTransform
            : null;

        targetRect = (rowRect != null && selfRect != null && rowRect.rect.width > selfRect.rect.width)
            ? rowRect
            : selfRect;

        if (instant && barRect && targetRect)
        {
            barRect.position = targetRect.position;
            barRect.sizeDelta = new Vector2(
                targetRect.rect.width + barPadding.x,
                targetRect.rect.height + barPadding.y
            );
        }
    }


}
