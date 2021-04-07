using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    [SerializeField] private float m_FadeSpeed;
    public IEnumerator FadeOut(CanvasGroup fade)
    {
        fade.blocksRaycasts = true;
        while (fade.alpha < 1f)
        {
            fade.alpha += Time.deltaTime * m_FadeSpeed;
            yield return null;
        }
        yield return null;
        fade.blocksRaycasts = false;
    }

    public IEnumerator FadeIn(CanvasGroup fade)
    {
        fade.blocksRaycasts = true;
        yield return new WaitForSeconds(0.5f);

        while (fade.alpha > 0f)
        {
            fade.alpha -= Time.deltaTime * m_FadeSpeed;
            yield return null;
        }
        yield return null;
        fade.blocksRaycasts = false;
    }
}
