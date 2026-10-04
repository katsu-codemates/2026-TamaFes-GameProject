using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public void OnBackToBaseSceneButtonClicked()
    {
        SceneManager.LoadScene("BaseScene");
    }
}
