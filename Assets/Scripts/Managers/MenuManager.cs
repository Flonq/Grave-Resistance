using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource buttonClickSound;
    
    // PLAY Button Function
    public void PlayGame()
    {
        PlayButtonSound();
        Debug.Log("Loading Game Scene...");
        SceneManager.LoadScene("GraveResistance");
    }
    
    // SETTINGS Button Function  
    public void OpenSettings()
    {
        PlayButtonSound();
        // TODO: Open Settings Panel
        Debug.Log("Open SETTINGS!");
    }
    
    // QUIT Button Function
    public void QuitGame()
    {
        PlayButtonSound();
        Debug.Log("QUIT Game!");
        Application.Quit();
    }
    
    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
            buttonClickSound.Play();
    }
} 