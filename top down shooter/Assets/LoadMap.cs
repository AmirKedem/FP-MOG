using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadMap : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene("Map", LoadSceneMode.Additive);
    }
}
