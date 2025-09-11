using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LockOnIndicator : MonoBehaviour
{
    public PlayerController playercontroller;

    [Header("Target Lock Indicator")]
    public GameObject imagePrefab;
    public Canvas uiCanvas;
    public float indicatorScale;

    private GameObject currentIndicator;
    private RectTransform indicatorRect;
    private Image indicatorImage;

    private void Start()
    {
        currentIndicator = Instantiate(imagePrefab, uiCanvas.transform);
        indicatorRect = currentIndicator.GetComponent<RectTransform>();
        indicatorImage = indicatorRect.GetComponent<Image>();

        indicatorRect.localScale = Vector3.one * indicatorScale;
        currentIndicator.SetActive(false);
    }

    private void Update()
    {
        currentIndicator.SetActive(playercontroller.lockedOnTarget && playercontroller.currentTarget != null);

        if (currentIndicator.activeSelf)
        {
            Vector3 targetPos = playercontroller.currentTarget.transform.position;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPos);

            indicatorRect.position = new Vector3(screenPos.x, screenPos.y, 0);
        }
    }
}
