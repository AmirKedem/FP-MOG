using UnityEngine;
using System.Collections;

public class AnimatedTexture : MonoBehaviour
{
    public float fps = 20.0f;
    public Texture2D[] frames;

    private MeshRenderer rendererMy;

    void Awake()
    {
        rendererMy = GetComponent<MeshRenderer>(); 
    }

    public void Flash()
    {
        StartCoroutine(StartFlash());
    }

    IEnumerator StartFlash()
    {
        rendererMy.enabled = true;

        for (int i = 0; i < frames.Length; i++)
        {
            rendererMy.sharedMaterial.SetTexture("_MainTex", frames[i]);
            yield return new WaitForSeconds(1 / fps);
        }

        rendererMy.enabled = false;
    }
}