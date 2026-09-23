using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

/// <summary>
/// レース実況の表示を統括する。
///
/// - スパート/アクシデント/ミラクル/追い抜き/スタミナ切れ/ゴールなどの「イベント」は
///   RaceEventBus経由で通知され、即座にキューへ追加される
/// - イベントが何もない間は、2〜3秒おきに現在の順位状況から実況文を自動生成する
/// - 表示の切り替えはCanvasGroupのフェードで演出する
/// - イベント系コメントは発生から一定時間(staleThreshold)を超えて待たされていたら
///   読み捨てて次のコメントに進む(実際の状況との乖離を防ぐ)
///
/// ★強制表示(isForce): ミラクル・1着ゴール・カメラの切り替わりなど、画面と同時に出したい
/// コメントは表示間隔を無視して割り込み、すぐに表示する。
/// ★全員に出番を: 走者ごとに名前を呼んだ回数を数え、あまり呼ばれていない走者を優先して取り上げる。
/// </summary>
public class RaceCommentator : MonoBehaviour
{
    [Header("実況テキスト")]
    [SerializeField] private TextMeshProUGUI commentText;
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Header("1つのコメントを表示しておく時間（秒）")]
    [SerializeField] private float minDisplayDuration = 2f;
    [SerializeField] private float maxDisplayDuration = 3f;

    [Header("強制表示コメント同士で割り込むまでの最低表示時間（秒）")]
    [SerializeField] private float minForceDisplayDuration = 1f;

    [Header("フェードにかける時間（秒）")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("接戦とみなす進行度の差")]
    [SerializeField] private float closeRaceThreshold = 0.03f;

    [Header("コメントが古いとみなされるまでの時間（秒）")]
    [SerializeField] private float staleThreshold = 3f;

    [Header("先頭以外の走者を取り上げる確率(0~1)")]
    [SerializeField] private float spotlightChance = 0.5f;

    // カメラのフォーカス時間を参照する。
    [Header("カメラとの連動")]
    [SerializeField] private RaceCameraController raceCamera;

    [Header("強調表示の見た目")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emphasisColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float emphasisScaleAmount = 0.35f; // 拡大アニメの強さ
    [SerializeField] private float emphasisPunchDuration = 0.4f;
    [SerializeField] private float fallbackEmphasisDuration = 2.5f; // raceCamera未設定時のフォールバック


    /// <summary>
    /// コメントの種類。キューの整理（破棄・まとめ）に使う。
    /// </summary>
    private enum CommentKind
    {
        Status,   // 自動生成の状況コメント
        Event,    // スパート・アクシデントなどのイベント
        Overtake, // 追い抜き（キューには最新の1件だけ残す）
        Finish,   // 2着以降のゴール（読み捨てない）
    }

    /// <summary>
    /// キューに積む１件分のコメントの構造体。
    /// </summary>
    private struct PendingComment
    {
        public string text;
        public float timestamp;
        public bool isEventDriven; // trueだと古いとみなすもの。
        public bool isFocusEvent;  // trueだと強調表示する
        public bool isForce;       // trueだと表示間隔を無視して割り込み、すぐに表示する
        public float displayDuration; // 0ならランダム(minDisplayDuration~maxDisplayDuration)
        public CommentKind kind;
    }

    private List<RaceParticipant> participants;
    private readonly List<PendingComment> pendingComments = new List<PendingComment>();
    private readonly Dictionary<RaceParticipant, int> mentionCount = new Dictionary<RaceParticipant, int>();
    private RaceParticipant lastLeader;
    private bool forceRequested; // 強制表示コメントが積まれたら立てる

    public void SetParticipants(List<RaceParticipant> list)
    {
        participants = list;
        lastLeader = null;
        pendingComments.Clear();
        mentionCount.Clear();
        forceRequested = false;
        EnqueueStatusComment(CommentTempletes.RaceStart());
    }

    private void OnEnable()
    {
        RaceEventBus.OnSpurtStarted += HandleSpurt;
        RaceEventBus.OnAccidentStarted += HandleAccident;
        RaceEventBus.OnMiracleStarted += HandleMiracle;
        RaceEventBus.OnFinished += HandleFinished;
        RaceEventBus.OnStaminaDepleted += HandleStaminaDepleted;
        RaceEventBus.OnOvertake += HandleOvertake;

        if (raceCamera != null)
        {
            raceCamera.OnGoalCameraStarted += HandleGoalCameraStarted;
            raceCamera.OnChaserShotStarted += HandleChaserShotStarted;
        }
    }

    public void OnDisable()
    {
        RaceEventBus.OnSpurtStarted -= HandleSpurt;
        RaceEventBus.OnAccidentStarted -= HandleAccident;
        RaceEventBus.OnMiracleStarted -= HandleMiracle;
        RaceEventBus.OnFinished -= HandleFinished;
        RaceEventBus.OnStaminaDepleted -= HandleStaminaDepleted;
        RaceEventBus.OnOvertake -= HandleOvertake;

        if (raceCamera != null)
        {
            raceCamera.OnGoalCameraStarted -= HandleGoalCameraStarted;
            raceCamera.OnChaserShotStarted -= HandleChaserShotStarted;
        }
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
                if (generated != null) EnqueueStatusComment(generated);
            }

            if (pendingComments.Count == 0)
            {
                yield return null;
                continue;
            }

            PendingComment next = pendingComments[0];
            pendingComments.RemoveAt(0);
            forceRequested = false;

            Debug.Log($"[実況 {Time.time:F2}] {next.text}");
            yield return ShowText(next.text, next.isFocusEvent, instant: next.isForce);

            float wait = next.displayDuration > 0f
                            ? next.displayDuration
                            : Random.Range(minDisplayDuration, maxDisplayDuration);

            yield return WaitInterruptible(wait, next.isForce);
        }
    }

    /// <summary>
    /// duration秒待つ。途中で強制表示コメントが積まれたら待機を打ち切る。
    /// 表示中のコメント自体が強制表示なら、minForceDisplayDurationは割り込ませない。
    /// Time.deltaTimeで数えるため、スロー演出中はカメラのフォーカス時間と同じように伸びる。
    /// </summary>
    private IEnumerator WaitInterruptible(float duration, bool shownIsForce)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (forceRequested && (!shownIsForce || elapsed >= minForceDisplayDuration))
            {
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// キューの先頭から、staleThresholdを超えて待たされているコメントを取り除く。
    /// </summary>
    private void DropStaleComments()
    {
        while (pendingComments.Count > 0)
        {
            PendingComment front = pendingComments[0];
            bool isStale = front.isEventDriven && (Time.time - front.timestamp) > staleThreshold;

            if (isStale)
            {
                pendingComments.RemoveAt(0); // 読み捨てて次へ
                Debug.Log($"Disposed:{front.text}");
            }
            else
            {
                break;
            }
        }
    }

    private IEnumerator ShowText(string text, bool emphasize, bool instant = false)
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.DOKill();
            if (!instant)
            {
                textCanvasGroup.DOFade(0f, fadeDuration); // 消す処理
                yield return new WaitForSeconds(fadeDuration);
            }
        }

        commentText.text = text;
        commentText.color = emphasize ? emphasisColor : normalColor;

        if (textCanvasGroup != null)
        {
            if (instant)
            {
                textCanvasGroup.alpha = 1f; // 強制表示は即座に切り替える
            }
            else
            {
                textCanvasGroup.DOFade(1f, fadeDuration); // 表示する処理
            }
        }

        if (emphasize)
        {
            commentText.transform.DOKill();
            commentText.transform.localScale = Vector3.one;
            commentText.transform.DOPunchScale(
                Vector3.one * emphasisScaleAmount,
                emphasisPunchDuration,
                vibrato: 6,
                elasticity: 0.5f
            );
        }

        if (!instant)
        {
            yield return new WaitForSeconds(fadeDuration);
        }
    }

    private void EnqueueEventDrivenComment(string text, bool isFocusEvent = false, CommentKind kind = CommentKind.Event)
    {
        // 追い抜きは状況がすぐ変わるので、古い追い抜きコメントは捨てて最新の1件だけ残す
        if (kind == CommentKind.Overtake)
        {
            pendingComments.RemoveAll(c => c.kind == CommentKind.Overtake && !c.isForce);
        }

        pendingComments.Add(new PendingComment
        {
            text = text,
            timestamp = Time.time,
            isEventDriven = true,
            isFocusEvent = isFocusEvent,
            kind = kind
        });
    }

    private void EnqueueStatusComment(string text)
    {
       pendingComments.Add(new PendingComment
       {
          text = text,
          timestamp = Time.time,
          isEventDriven = false, // 時間制限の対象外
          isFocusEvent = false,
          kind = CommentKind.Status
       });
    }

    // 2着以降のゴールは、全員の名前を呼びたいので読み捨てない
    private void EnqueueFinishComment(string text)
    {
        pendingComments.Add(new PendingComment
        {
            text = text,
            timestamp = Time.time,
            isEventDriven = false,
            isFocusEvent = false,
            displayDuration = minDisplayDuration,
            kind = CommentKind.Finish
        });
    }

    /// <summary>
    /// 表示間隔を無視して、すぐに表示するコメントを積む。
    /// 画面と噛み合わなくなる待機中のコメント（ゴール以外）は捨てる。
    /// </summary>
    private void EnqueueForceComment(string text, bool emphasize, float duration = 0f)
    {
        pendingComments.RemoveAll(c => !c.isForce && c.kind != CommentKind.Finish);

        // すでに積まれている強制表示コメントの後ろ（=実質先頭）に割り込ませる
        int insertIndex = 0;
        while (insertIndex < pendingComments.Count && pendingComments[insertIndex].isForce) insertIndex++;

        pendingComments.Insert(insertIndex, new PendingComment
        {
            text = text,
            timestamp = Time.time,
            isEventDriven = false,
            isFocusEvent = emphasize,
            isForce = true,
            displayDuration = duration,
            kind = CommentKind.Event
        });
        forceRequested = true;
    }

    // イベント時のコメント表示処理
    private void HandleSpurt(RaceParticipant p)
    {
        Mention(p);
        EnqueueEventDrivenComment(CommentTempletes.Spurt(p));
    }

    private void HandleAccident(RaceParticipant p)
    {
        Mention(p);
        EnqueueEventDrivenComment(CommentTempletes.Accident(p));
    }

    private void HandleMiracle(RaceParticipant p)
    {
        Mention(p);
        string text = CommentTempletes.Miracle(p);

        if (raceCamera == null)
        {
            EnqueueForceComment(text, emphasize: true, fallbackEmphasisDuration);
        }
        else if (!raceCamera.IsGoalCameraActive)
        {
            // カメラが寄っている時間と同じだけ強調表示する
            EnqueueForceComment(text, emphasize: true, raceCamera.DefaultFocusDuration);
        }
        else
        {
            // ゴールカメラ中はカメラが寄らないので、通常のイベントとして扱う
            EnqueueEventDrivenComment(text);
        }
    }

    private void HandleStaminaDepleted(RaceParticipant p)
    {
        Mention(p);
        EnqueueEventDrivenComment(CommentTempletes.StaminaDepleted(p));
    }

    private void HandleOvertake(RaceParticipant passer, RaceParticipant passed, int newRank)
    {
        Mention(passer, passed);
        if (newRank == 1) lastLeader = passer; // 自動生成の「先頭に立った」と重複させない
        EnqueueEventDrivenComment(CommentTempletes.Overtake(passer, passed, newRank), kind: CommentKind.Overtake);
    }

    private void HandleFinished(RaceParticipant p)
    {
        Mention(p);

        if (p.finishRank == 1)
        {
            EnqueueForceComment(CommentTempletes.Winner(p), emphasize: true, maxDisplayDuration);
        }
        else if (participants != null && p.finishRank == participants.Count)
        {
            EnqueueFinishComment(CommentTempletes.LastFinisher(p));
        }
        else
        {
            EnqueueFinishComment(CommentTempletes.Finished(p, p.finishRank));
        }
    }

    // カメラ連動：ゴールカメラに切り替わった瞬間
    private void HandleGoalCameraStarted(RaceParticipant leader)
    {
        if (leader == null) return;
        Mention(leader);
        EnqueueForceComment(CommentTempletes.FinalStretch(leader), emphasize: false);
    }

    // カメラ連動：後続集団のショットに切り替わった瞬間
    private void HandleChaserShotStarted(List<RaceParticipant> members)
    {
        // 映っている走者の中から、あまり名前を呼ばれていない走者を選ぶ
        RaceParticipant target = null;
        foreach (var p in members)
        {
            if (p.isFinished) continue;
            if (target == null || GetMentionCount(p) < GetMentionCount(target)) target = p;
        }
        if (target == null) return;

        Mention(target);
        EnqueueForceComment(CommentTempletes.ChaserShot(target), emphasize: false, raceCamera.ChaserShotDuration);
    }

    // 二秒ごとの状況に合わせたコメント表示処理
    private string GenerateStatusComment()
    {
        RaceParticipant leader = GetLeader();
        if (leader == null) return null;

        RaceParticipant secondLeader = GetSecondLeader(leader);
        bool isClose = secondLeader != null && (leader.progress - secondLeader.progress) < closeRaceThreshold;

        // 1着が決まった後は「先頭」ではなく、次の着順争いとして実況する
        int finishedCount = GetFinishedCount();
        if (finishedCount > 0)
        {
            int place = finishedCount + 1;
            if (isClose)
            {
                Mention(leader, secondLeader);
                return CommentTempletes.PlaceBattle(leader, secondLeader, place);
            }

            string spotlight = TryGenerateSpotlight(leader);
            if (spotlight != null) return spotlight;

            Mention(leader);
            return CommentTempletes.HeadingToGoal(leader, place);
        }

        string comment;
        if (isClose)
        {
            Mention(leader, secondLeader);
            comment = CommentTempletes.CloseRace(leader, secondLeader);
        }
        else if (leader != lastLeader)
        {
            Mention(leader);
            comment = CommentTempletes.NewLeader(leader);
        }
        else
        {
            comment = TryGenerateSpotlight(leader);
            if (comment == null)
            {
                Mention(leader);
                comment = CommentTempletes.Leading(leader);
            }
        }

        lastLeader = leader;
        return comment;
    }

    /// <summary>
    /// 先頭よりも名前を呼ばれていない走者がいれば、spotlightChanceの確率でその走者を取り上げる。
    /// </summary>
    private string TryGenerateSpotlight(RaceParticipant leader)
    {
        if (Random.value >= spotlightChance) return null;

        RaceParticipant target = null;
        foreach (var p in participants)
        {
            if (p == leader || p.isFinished) continue;
            if (target == null || GetMentionCount(p) < GetMentionCount(target)) target = p;
        }
        if (target == null || GetMentionCount(target) >= GetMentionCount(leader)) return null;

        int rank = GetRank(target);
        string comment;
        if (Random.value < 0.5f)
        {
            comment = CommentTempletes.SpotlightStat(target);
        }
        else if (rank == participants.Count)
        {
            comment = CommentTempletes.SpotlightLast(target);
        }
        else
        {
            comment = CommentTempletes.SpotlightMid(target, rank);
        }

        Mention(target);
        return comment;
    }

    private void Mention(params RaceParticipant[] mentioned)
    {
        foreach (var p in mentioned)
        {
            if (p == null) continue;
            mentionCount[p] = GetMentionCount(p) + 1;
        }
    }

    private int GetMentionCount(RaceParticipant p)
        => mentionCount.TryGetValue(p, out int count) ? count : 0;

    // ゴール済みも含めた現在の順位(1始まり)
    private int GetRank(RaceParticipant target)
    {
        int rank = 1;
        foreach (var p in participants)
        {
            if (p != target && p.progress > target.progress) rank++;
        }
        return rank;
    }

    private int GetFinishedCount()
    {
        int count = 0;
        foreach (var p in participants)
        {
            if (p.isFinished) count++;
        }
        return count;
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
