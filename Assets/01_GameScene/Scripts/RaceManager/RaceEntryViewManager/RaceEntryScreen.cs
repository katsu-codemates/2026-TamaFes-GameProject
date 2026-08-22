using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


/// <summary>
/// レース開始前に、主層メンバーの画像 + 名前を並べて表示する画面。
/// </summary>
public class RaceEntryScreen : MonoBehaviour
{
    [SerializeField] private AnimalRoster animalRoster;
    [SerializeField] private RaceManager raceManager;

    [Header("参加者カード表示")]
    [SerializeField] private GameObject entryCardPrefab;
    [SerializeField] private Transform cardContainer;

    [Header("カウントダウン")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float countdownSeconds = 30f;

    [Header("カウントダウン終了時に非表示にするオブジェクト")]
    [SerializeField] private GameObject screenRoot;

    private void Start()
    {
        List<AnimalData> selectedAnimals = raceManager.SelectParticipants(new List<AnimalData>(animalRoster.Animals));
        DisplayEntries(selectedAnimals);
        StartCoroutine(CountdownRoutine());
    }

    private void DisplayEntries(List<AnimalData> selected)
    {
        foreach (var animal in selected)
        {
            GameObject card = Instantiate(entryCardPrefab, cardContainer);
            RaceEntryCardView view = card.GetComponent<RaceEntryCardView>();
            view.Setup(animal);
        }
    }

    private IEnumerator CountdownRoutine()
    {
        float remaining = countdownSeconds;

        while (remaining > 0f)
        {
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        countdownText.text = "0";

        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }

        raceManager.BeginRace();
    }
}
