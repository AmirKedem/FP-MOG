using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartClient : MonoBehaviour
{
    public void RestartClientScene()
    {
        SceneManager.LoadScene("Client");
    }
}
