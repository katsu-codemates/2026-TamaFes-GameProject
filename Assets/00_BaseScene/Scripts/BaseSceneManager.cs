using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BaseSceneManager : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private IllustrationManager illustrationManager;
    [SerializeField] private AnimalRoster animalRoster;

    private void Start()
    {
        audioManager.PlayBgm(BgmId.Base);
    }

    public void OnRaceStartButtonClicked()
    {
        List<AnimalData> loadedAnimals = illustrationManager.GetRegisteredAnimals();
        animalRoster.SetAnimals(loadedAnimals);
        SceneManager.LoadScene("GameScene");
    }
}
