using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaceSceneManager : MonoBehaviour
{
    [SerializeField] private IllustrationManager illustrationManager;
    [SerializeField] private AnimalRoster animalRoster;

    public void OnRaceStartButtonClicked()
    {
        List<AnimalData> loadedAnimals = illustrationManager.GetResisterdAnimals();
        animalRoster.SetAnimals(loadedAnimals);
        SceneManager.LoadScene("GameScene");
    }
}
