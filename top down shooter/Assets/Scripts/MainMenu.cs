using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject controlsPanel;

    // Play button
    public void PlayGame()
    {
        SceneManager.LoadScene("Client");
    }

    // Controls button
    public void ShowControls()
    {
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void ShowOptions()
    {
        if (controlsPanel && controlsPanel.activeSelf)
        {
            controlsPanel.SetActive(false);
            optionsPanel.SetActive(true);
        }
    }

    // Quit button
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
         Application.Quit();
    #endif
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowOptions();
        }
    }
}
