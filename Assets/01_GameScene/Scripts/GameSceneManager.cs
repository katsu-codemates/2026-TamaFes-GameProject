using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private AnimalRoster animalRoster;

    private void Start()
    {
        //raceManager.StartRace(new List<AnimalData>(animalRoster.Animals));
    }

    public void OnBackToBaseSceneButtonClicked()
    {
        SceneManager.LoadScene("BaseScene");
    }
}
