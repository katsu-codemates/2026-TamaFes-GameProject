using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public AudioManager audioManager;

    public void OnBackToBaseSceneButtonClicked()
    {
        SceneManager.LoadScene("BaseScene");
    }
}
