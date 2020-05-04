using UnityEngine;
using System.Collections;

public class AnimatedTexture : MonoBehaviour
{
    public float fps = 20.0f;

    private Coroutine co;
    [SerializeField] private GameObject muzzelFlashGO;

    public void Flash()
    {
        if (this.isActiveAndEnabled)
        {
            if (co != null)
                StopCoroutine(co);
            co = StartCoroutine(StartFlash());
        }
    }

    IEnumerator StartFlash()
    {
        muzzelFlashGO.SetActive(true);
        yield return new WaitForSeconds(1 / fps);
        muzzelFlashGO.SetActive(false);
    }
}