using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// レース全体の進み具合を見せる横長の進行バー(ミニマップ)。
/// trackAreaの左端=スタート、右端=ゴールとして、各走者のprogress(0~1)に合わせてアイコンを動かす。
/// UIはシーン上に手作業で配置する(README_RaceMinimap.md参照)。
/// </summary>
public class RaceMinimap : MonoBehaviour
{
    [Header("表示のON/OFFを切り替えるルート(出走表画面・結果画面では非表示)")]
    [SerializeField] private GameObject minimapRoot;

    [Header("コース部分(左端=スタート、右端=ゴール)")]
    [Tooltip("アイコンと目盛りはこのRectTransformの子として生成し、アンカーで位置を決める")]
    [SerializeField] private RectTransform trackArea;

    [Header("走者アイコン")]
    [SerializeField] private RaceMinimapIcon iconPrefab;
    [Tooltip("レーンごとにアイコンを縦にずらす量(px)。接戦でアイコンが完全に重なるのを防ぐ")]
    [SerializeField] private float laneOffsetY = 6f;

    [Header("中間の目盛り")]
    [SerializeField] private RectTransform tickPrefab;
    [Tooltip("何mごとに目盛りを置くか(スタートとゴールの位置には置かない)")]
    [SerializeField] private float tickIntervalMeters = 1000f;
    [Tooltip("目盛りの子にTextMeshProUGUIがあれば表示する文字。{0}=スタートからの距離, {1}=ゴールまでの残り距離(m)")]
    [SerializeField] private string tickLabelFormat = "残り{1}m";

    private readonly List<RaceMinimapIcon> icons = new List<RaceMinimapIcon>();
    private readonly List<GameObject> ticks = new List<GameObject>();
    private int totalParticipantCount;

    private void Awake()
    {
        SetVisible(false);
    }

    /// <summary>
    /// 出走メンバーのアイコンと目盛りを作り、ミニマップを表示する。
    /// </summary>
    public void SetParticipants(IReadOnlyList<RaceParticipant> participants)
    {
        // アイコン側で画像読み込みのコルーチンを開始するため、先に表示しておく
        SetVisible(true);
        Clear();
        totalParticipantCount = participants.Count;

        CreateTicks();

        foreach (var participant in participants)
        {
            RaceMinimapIcon icon = Instantiate(iconPrefab, trackArea);
            icon.Setup(participant);
            icons.Add(icon);
        }

        UpdateIcons();
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void Update()
    {
        if (icons.Count == 0) return;
        UpdateIcons();
    }

    private void UpdateIcons()
    {
        foreach (var icon in icons)
        {
            RaceParticipant participant = icon.Participant;
            float progress = participant.isFinished ? 1f : Mathf.Clamp01(participant.progress);

            // 横位置はアンカーで決める(画面解像度やバーの長さが変わってもずれない)
            RectTransform rect = icon.RectTransform;
            rect.anchorMin = new Vector2(progress, 0.5f);
            rect.anchorMax = new Vector2(progress, 0.5f);
            float laneOffset = (participant.laneIndex - (totalParticipantCount - 1) / 2f) * laneOffsetY;
            rect.anchoredPosition = new Vector2(0f, laneOffset);

            icon.SetFinished(participant.isFinished);
        }

        // 前にいる走者ほど手前に描画する(後に並んだ子ほど手前に描かれる)
        icons.Sort((a, b) => a.Participant.progress.CompareTo(b.Participant.progress));
        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].transform.SetAsLastSibling();
        }
    }

    private void CreateTicks()
    {
        if (tickPrefab == null || tickIntervalMeters <= 0f) return;

        for (float meters = tickIntervalMeters; meters < RaceTrack.TrackLength - 0.01f; meters += tickIntervalMeters)
        {
            RectTransform tick = Instantiate(tickPrefab, trackArea);
            float t = meters / RaceTrack.TrackLength;

            // 縦方向のアンカーはプレハブの設定を活かし、横方向だけ合わせる
            tick.anchorMin = new Vector2(t, tick.anchorMin.y);
            tick.anchorMax = new Vector2(t, tick.anchorMax.y);
            tick.anchoredPosition = new Vector2(0f, tick.anchoredPosition.y);

            var label = tick.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = string.Format(tickLabelFormat, Mathf.RoundToInt(meters), Mathf.RoundToInt(RaceTrack.TrackLength - meters));
            }

            ticks.Add(tick.gameObject);
        }
    }

    private void Clear()
    {
        foreach (var icon in icons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        icons.Clear();

        foreach (var tick in ticks)
        {
            if (tick != null) Destroy(tick);
        }
        ticks.Clear();
    }

    private void SetVisible(bool visible)
    {
        if (minimapRoot != null)
        {
            minimapRoot.SetActive(visible);
        }
    }
}
