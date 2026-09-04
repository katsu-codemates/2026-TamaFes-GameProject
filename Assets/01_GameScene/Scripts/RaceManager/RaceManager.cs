using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

/// <summary>
/// レースの進行を管理するクラス。
/// 出走メンバーの決定⇒レーン割り当て⇒生成⇒進行監視⇒結果通知
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    [Header("動物一体分のプレハブ")]
    [SerializeField] private GameObject animalPrefab;
    [SerializeField] private Transform animalsParent;

    [Header("カメラ")]
    [SerializeField] private RaceCameraController raceCamera;

    [Header("レース計算式の調整用パラメータ")]
    [SerializeField] private RaceTuningConfig raceTuning;

    [Header("実況機能")]
    [SerializeField] private RaceCommentator raceCommentator;

    [Header("結果画面")]
    [SerializeField] private RaceResultScreen raceResultScreen;

    [Header("出走数")]
    [SerializeField] private int racerCount = 5;

    private List<RaceParticipant> participants = new List<RaceParticipant>();
    private List<RaceParticipant> finishedOrder = new List<RaceParticipant>();
    private List<AnimalRacerView> racerViews = new List<AnimalRacerView>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 登録済みの動物一覧から出走メンバーを抽選し、レースを開始する。
    /// </summary>
    public List<AnimalData> SelectParticipants(List<AnimalData> allAnimals)
    {
        Debug.Log("レーススタート");

        // 出走数に応じて動物を抽選
        List<AnimalData> selectedAnimals = allAnimals
            .OrderBy(a => Random.value)
            .Take(racerCount)
            .ToList();

        participants = selectedAnimals
            .Select((animalData, index) => new RaceParticipant { animalData = animalData, laneIndex = index })
            .ToList();

        return selectedAnimals;
    }

    public void BeginRace()
    {
        finishedOrder.Clear();
        racerViews.Clear();

        raceCamera.SetParticipants(participants);
        raceCommentator.SetParticipants(participants);

        foreach (var participant in participants)
        {
            GameObject racerObj = Instantiate(animalPrefab, animalsParent);
            var racerView = racerObj.GetComponent<AnimalRacerView>();
            racerView.SetUp(participant, raceTuning, participants.Count);
            racerViews.Add(racerView);
        }
    }

    public void NotifyFinished(RaceParticipant participant)
    {
        if (finishedOrder.Contains(participant)) return;

        participant.finishRank = finishedOrder.Count + 1;
        finishedOrder.Add(participant);

        RaceEventBus.RaiseFinished(participant);
        Debug.Log($"{participant.animalData.animalName} がゴール！順位{participant.finishRank}");

        if (finishedOrder.Count == participants.Count)
        {
            OnRaceComplete();
        }
    }

    private void OnRaceComplete()
    {
        // 結果画面へ
        Debug.Log("レース終了");
        if (raceResultScreen != null)
        {
            raceResultScreen.Show(finishedOrder);
        }
    }
}
