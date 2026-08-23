using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Runtime.InteropServices;

/// <summary>
/// レース実況機能の表示を統括するクラス。
/// </summary>
public class RaceCommentator : MonoBehaviour
{
    [Header("実況テキスト")]
    [SerializeField] private TextMeshProUGUI commentText;
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Header("1つのコメントを表示しておく時間（秒）")]
    [SerializeField] private float minDisplayDuration = 2f;
    [SerializeField] private float maxDisplayDuration = 3f;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("接戦とみなす進行度の差")]
    [SerializeField] private float closeRaceThreshold = 0.03f;

    [Header("コメントが古いとみなされるまでの時間（秒）")]
    [SerializeField] private float staleThreshold = 3f;
    /// <summary>
    /// キューに積む１件分のコメントの構造体。
    /// </summary>
    private struct PendingComment
    {
        public string text;
        public float timestamp;
        public bool isEventDriven; // trueだと古いとみなすもの。
    }

    private List<RaceParticipant> participants;
    private readonly Queue<PendingComment> pendingComments = new Queue<PendingComment>();
    private RaceParticipant lastLeader;

    public void SetParticipants(List<RaceParticipant> list)
    {
        participants = list;
        lastLeader = null;
        pendingComments.Clear();
        EnqueueStatusComment(CommentTempletes.RaceStart());
    }

    private void OnEnable()
    {
        RaceEventBus.OnSpurtStarted += HandleSpurt;
        RaceEventBus.OnAccidentStarted += HandleAccident;
        RaceEventBus.OnMiracleStarted += HandleMiracle;
        RaceEventBus.OnFinished += HandleFinished;
    }

    public void OnDisable()
    {
        RaceEventBus.OnSpurtStarted -= HandleSpurt;
        RaceEventBus.OnAccidentStarted -= HandleAccident;
        RaceEventBus.OnMiracleStarted -= HandleMiracle;
        RaceEventBus.OnFinished -= HandleFinished;
    }

    private void Start()
    {
        StartCoroutine(DisplayLoop());
    }

    private IEnumerator DisplayLoop()
    {
        while (true)
        {
            DropStaleComments();

            // 2秒ごとのコメントを生成
            if (pendingComments.Count == 0 && participants != null)
            {
                string generated = GenerateStatusComment();
                Debug.Log(generated);
                if (generated != null) EnqueueStatusComment(generated);
            }

            if (pendingComments.Count > 0)
            {
                PendingComment next = pendingComments.Dequeue();
                yield return ShowText(next.text);
            }

            float wait = Random.Range(minDisplayDuration, maxDisplayDuration);
            yield return new WaitForSeconds(wait);
        }
    }

    /// <summary>
    /// キューの先頭から、staleThresholdを超えて待たされているコメントを取り除く。
    /// </summary>
    private void DropStaleComments()
    {
        while (pendingComments.Count > 0)
        {
            PendingComment front = pendingComments.Peek();
            bool isStale = front.isEventDriven && (Time.time - front.timestamp) > staleThreshold;

            if (isStale)
            {
                PendingComment p = pendingComments.Dequeue(); // 読み捨てて次へ
                Debug.Log($"Disposed:{p.text}");
            }
            else
            {
                break;
            }
        }
    }

    private IEnumerator ShowText(string text)
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.DOFade(0f, fadeDuration); // 消す処理
            yield return new WaitForSeconds(fadeDuration);
        }

        commentText.text = text;

        if (textCanvasGroup != null)
        {
            textCanvasGroup.DOFade(1f, fadeDuration); // 表示する処理
            yield return new WaitForSeconds(fadeDuration);
        }
    }

    private void EnqueueEventDrivenComment(string text)
    {
        pendingComments.Enqueue(new PendingComment
        {
            text = text,
            timestamp = Time.time,
            isEventDriven = true
        });
    }

    private void EnqueueStatusComment(string text)
    {
       pendingComments.Enqueue(new PendingComment
       {
          text = text,
          timestamp = Time.time,
          isEventDriven = false // 時間制限の対象外 
       });

    }

    // イベント時のコメント表示処理
    private void HandleSpurt(RaceParticipant p) 
        => EnqueueEventDrivenComment(CommentTempletes.Spurt(p));
    private void HandleAccident(RaceParticipant p)
        => EnqueueEventDrivenComment(CommentTempletes.Accident(p));
    private void HandleMiracle(RaceParticipant p)
        => EnqueueEventDrivenComment(CommentTempletes.Miracle(p));
    
    private void HandleFinished(RaceParticipant p)
    {
        if (p.finishRank == 1)
        {
            EnqueueStatusComment(CommentTempletes.Winner(p));
        }
    }

    // 二秒ごとの状況に合わせたコメント表示処理
    private string GenerateStatusComment()
    {
        RaceParticipant leader = GetLeader();
        if (leader == null) return null;

        RaceParticipant secondLeader = GetSecondLeader(leader);
        string comment;

        if (secondLeader != null && (leader.progress - secondLeader.progress) < closeRaceThreshold)
        {
            comment = CommentTempletes.CloseRace(leader, secondLeader);
        }
        else if (leader != lastLeader)
        {
            comment = CommentTempletes.NewLeader(leader);
        }
        else
        {
            comment = CommentTempletes.Leading(leader);
        }

        lastLeader = leader;
        return comment;
    }

    private RaceParticipant GetLeader()
    {
        RaceParticipant leader = null;
        foreach (var p in participants)
        {
            if (p.isFinished) continue;
            if (leader == null || p.progress > leader.progress) leader = p;
        }
        return leader;
    }

    private RaceParticipant GetSecondLeader(RaceParticipant leader)
    {
        RaceParticipant secondLeader = null;
        foreach (var p in participants)
        {
            if (p == leader || p.isFinished) continue;
            if (secondLeader == null || p.progress > secondLeader.progress)  secondLeader = p;
        }
        return secondLeader;
    }
}
