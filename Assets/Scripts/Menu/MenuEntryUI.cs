using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public TMP_Text label;

    [Header("Colors")]
    public Color normalColor   = new Color(0.55f, 0.55f, 0.55f, 1f);
    public Color selectedColor = new Color(0.92f, 0.89f, 0.82f, 1f);
    public Color disabledColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("State")]
    public bool interactable = true;

    [HideInInspector] public MainMenuController owner;

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
    }

    public void SetSelected(bool selected)
    {
        if (!label) return;

        if (!interactable)
        {
            label.color = disabledColor;
            return;
        }

        label.color = selected ? selectedColor : normalColor;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        SetSelected(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null && interactable)
            owner.SelectByItem(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null || !interactable) return;

        // If clicked item isn't selected yet, select it first
        owner.SelectByItem(this);
        owner.ActivateCurrent();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
