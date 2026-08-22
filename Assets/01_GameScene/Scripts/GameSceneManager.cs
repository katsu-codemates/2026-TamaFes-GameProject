using System.Collections.Generic;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private AnimalRoster animalRoster;

    private void Start()
    {
        //raceManager.StartRace(new List<AnimalData>(animalRoster.Animals));
    }
}
