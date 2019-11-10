using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMap : MonoBehaviour
{
#if !UNITY_EDITOR
    void Start()
    {
        Scene scn = SceneManager.GetSceneByPath("Map");
        if (!scn.isLoaded)
            SceneManager.LoadScene("Map", LoadSceneMode.Additive);
    }
#endif
}

