using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    public enum MenuAction { NewGame, Continue, ExitGame }

    [System.Serializable]
    public class Entry
    {
        public MenuEntryUI item;
        public MenuAction action;
    }

    [Header("Entries (top to bottom)")]
    public List<Entry> entries = new();

    [Header("Selection Glow")]
    public Image selectionBar;
    public Vector2 barPadding = new Vector2(120f, 18f);
    public float moveSpeed = 18f;

    [Header("Scenes")]
    public Object newGameScene;

    [Header("Input System")]
    public InputActionReference navigate; // Vector2
    public InputActionReference submit;   // Button

    [Header("Navigation Feel")]
    public float deadzone = 0.5f;
    public float initialRepeatDelay = 0.25f;
    public float repeatRate = 0.12f;

    [Header("Save (temp)")]
    public bool hasSaveFile = false; // set false for now (disables Continue)

    int index = 0;
    RectTransform barRect;
    RectTransform targetRect;

    float nextRepeatTime = 0f;
    int lastMoveDir = 0;

    void OnEnable()
    {
        if (navigate?.action != null)
        {
            navigate.action.Enable();
            navigate.action.performed += OnNavigate;
            navigate.action.canceled += OnNavigateCanceled;
        }

        if (submit?.action != null)
        {
            submit.action.Enable();
            submit.action.performed += OnSubmit;
        }
    }

    void OnDisable()
    {
        if (navigate?.action != null)
        {
            navigate.action.performed -= OnNavigate;
            navigate.action.canceled -= OnNavigateCanceled;
            navigate.action.Disable();
        }

        if (submit?.action != null)
        {
            submit.action.performed -= OnSubmit;
            submit.action.Disable();
        }
    }

    void Awake()
    {
        if (selectionBar) barRect = selectionBar.rectTransform;

        // Let items know who owns them (for hover)
        foreach (var e in entries)
            if (e.item) e.item.owner = this;

        // Disable Continue for now
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].action == MenuAction.Continue && entries[i].item)
                entries[i].item.SetInteractable(false);
        }

        index = FindFirstInteractableIndex();
        ApplySelection(true);
    }

    void Update()
    {
        if (entries.Count == 0) return;

        if (lastMoveDir != 0 && Time.unscaledTime >= nextRepeatTime)
        {
            Move(lastMoveDir);
            nextRepeatTime = Time.unscaledTime + repeatRate;
        }

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
        Vector2 v = ctx.ReadValue<Vector2>();

        if (v.y > deadzone) StartMoveRepeat(-1);
        else if (v.y < -deadzone) StartMoveRepeat(+1);
        else StopMoveRepeat();
    }

    void OnNavigateCanceled(InputAction.CallbackContext ctx) => StopMoveRepeat();

    void StartMoveRepeat(int dir)
    {
        if (dir != lastMoveDir)
        {
            lastMoveDir = dir;
            Move(dir);
            nextRepeatTime = Time.unscaledTime + initialRepeatDelay;
            return;
        }

        if (lastMoveDir == 0)
        {
            lastMoveDir = dir;
            Move(dir);
            nextRepeatTime = Time.unscaledTime + initialRepeatDelay;
        }
    }

    void StopMoveRepeat() => lastMoveDir = 0;

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        StartCoroutine(ActivateNextFrame());
    }

    System.Collections.IEnumerator ActivateNextFrame()
    {
        yield return null;
        ActivateCurrent();
    }


    int FindFirstInteractableIndex()
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].item && entries[i].item.interactable) return i;
        return 0;
    }

    bool IsInteractable(int i) => entries[i].item != null && entries[i].item.interactable;

    void Move(int delta)
    {
        if (entries.Count == 0) return;

        entries[index].item?.SetSelected(false);

        int tries = 0;
        do
        {
            index = (index + delta) % entries.Count;
            if (index < 0) index += entries.Count;
            tries++;
        }
        while (tries <= entries.Count && !IsInteractable(index));

        ApplySelection(false);
    }

    void ApplySelection(bool instant)
    {
        entries[index].item?.SetSelected(true);
        targetRect = entries[index].item ? entries[index].item.GetComponent<RectTransform>() : null;

        if (instant && barRect && targetRect)
        {
            barRect.position = targetRect.position;
            barRect.sizeDelta = new Vector2(
                targetRect.rect.width + barPadding.x,
                targetRect.rect.height + barPadding.y
            );
        }
    }

    // called by hover
    public void SelectByItem(MenuEntryUI hovered)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].item == hovered && hovered.interactable)
            {
                entries[index].item?.SetSelected(false);
                index = i;
                ApplySelection(false);
                return;
            }
        }
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



    public void ActivateCurrent()
    {
        if (!IsInteractable(index)) return;

        switch (entries[index].action)
        {
            case MenuAction.NewGame:
                LoadSceneSafe(newGameScene);
                break;

            case MenuAction.Continue:
                break;

            case MenuAction.ExitGame:
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                    break;
        }
    }

    void LoadSceneSafe(Object sceneObj)
    {
        if (!sceneObj)
        {
            Debug.LogWarning("[MainMenu] New Game scene not assigned.");
            return;
        }

        SceneManager.LoadScene(sceneObj.name);
    }

}
