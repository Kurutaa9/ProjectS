using System.Collections;
using UnityEngine;

public class MainMenuIntroFade : MonoBehaviour
{
    public CanvasGroup titleGroup;
    public CanvasGroup menuGroup;
    public CanvasGroup barGroup;

    public float titleDelay = 0.15f;
    public float titleFadeTime = 0.6f;

    public float menuDelay = 0.35f;
    public float menuFadeTime = 0.6f;

    void Start()
    {
        // Start hidden
        SetAlpha(titleGroup, 0f);
        SetAlpha(menuGroup, 0f);
        SetAlpha(barGroup, 0f);

        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return FadeIn(titleGroup, titleDelay, titleFadeTime);
        yield return FadeIn(menuGroup, menuDelay, menuFadeTime);
        yield return FadeIn(barGroup, 0f, 0.35f);
    }

    IEnumerator FadeIn(CanvasGroup g, float delay, float time)
    {
        if (!g) yield break;

        yield return new WaitForSecondsRealtime(delay);

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Clamp01(t / time);
            yield return null;
        }
        g.alpha = 1f;
    }

    void SetAlpha(CanvasGroup g, float a)
    {
        if (g) g.alpha = a;
    }
}
