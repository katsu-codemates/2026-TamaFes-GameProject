using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AnimalIdleAnimation : MonoBehaviour
{
    private enum AnimalAction { Wander, Eat, Sleep, Fight, Celebrate }

    [Header("特殊行動の抽選確率（%）。残りはWander（通常移動）になる")]
    [SerializeField] private float eatWeight = 10f;
    [SerializeField] private float sleepWeight = 10f;
    [SerializeField] private float fightWeight = 10f;
    [Tooltip("直前のレースで1着だった動物だけが抽選する")]
    [SerializeField] private float celebrateWeight = 30f;

    [Header("演出（食べる）")]
    [SerializeField] private float eatingDuration = 0.3f;
    [SerializeField] private int maxBiteCount = 6;
    [SerializeField] private int minBiteCount = 3;

    [Header("演出（眠る）")]
    [SerializeField] private float breathingDuration = 1f;
    [SerializeField] private float minSleepingDuration = 3f;
    [SerializeField] private float maxSleepingDuration = 6f;

    [Header("演出（けんか")]
    [SerializeField] private int minClashCount = 3;
    [SerializeField] private int maxClashCount = 5;

    [Header("演出（喜ぶ）")]
    [SerializeField] private int minHopCount = 3;
    [SerializeField] private int maxHopCount = 5;
    [SerializeField] private float hopHeight = 0.8f;
    [SerializeField] private float hopDuration = 0.35f;
    [SerializeField] private float finalJumpHeight = 2f;
    [SerializeField] private float finalJumpDuration = 0.9f;
    [SerializeField] private Color celebrateColor = new Color(1f, 0.92f, 0.5f, 1f);

    [Header("演出（新規登場）")]
    [Tooltip("着地点からどれだけ上から落ちてくるか")]
    [SerializeField] private float entranceDropHeight = 6f;
    [SerializeField] private float entranceDropDuration = 0.8f;
    [SerializeField] private Color entranceGlowColor = new Color(1f, 1f, 0.75f, 1f);
    [Tooltip("着地後に明滅する回数")]
    [SerializeField] private int entranceGlowCount = 3;
    [Tooltip("頭上の「NEW!」を表示しておく時間（秒）。演出が終わって歩き出した後も追従して残る")]
    [SerializeField] private float newBadgeDuration = 5f;
    [SerializeField] private SeId entranceSe = SeId.Landing;

    [Header("エフェクト")]
    [Tooltip("眠っている間に「Z」を出す間隔（秒）")]
    [SerializeField] private float sleepZInterval = 0.8f;

    Tween idleTween;
    Tween jumpTween;
    SpriteRenderer spriteRenderer;
    Billboard billboard;
    Coroutine randomMoveCoroutine;
    AnimalActionScheduler animalActionScheduler;
    AnimalDataHolder dataHolder;
    AnimalEmoteEffect emoteEffect;
    bool playEntranceOnStart;

    // 予約中の餌場・寝床の番号。行動中に破棄されたとき(表示上限で消されたときなど)に予約を解除するために覚えておく
    int reservedFeedingSpotIndex = -1;
    int reservedBedSpotIndex = -1;

    public bool IsBusy { get; set; }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        billboard = GetComponent<Billboard>();
        dataHolder = GetComponent<AnimalDataHolder>();
        emoteEffect = GetComponent<AnimalEmoteEffect>();
        animalActionScheduler.Register(this);
        if (playEntranceOnStart)
        {
            randomMoveCoroutine = StartCoroutine(EntranceThenRandomMove());
        }
        else
        {
            IdleMotion();
            randomMoveCoroutine = StartCoroutine(RandomMove());
        }
    }

    public void Initialize(AnimalActionScheduler animalActionScheduler)
    {
        this.animalActionScheduler = animalActionScheduler;
    }

    /// <summary>
    /// 新しく追加された動物として、表示開始時に登場演出を再生させる。Start()より前に呼ぶこと。
    /// </summary>
    public void RequestEntrance()
    {
        playEntranceOnStart = true;
    }

    IEnumerator EntranceThenRandomMove()
    {
        yield return DoEntrance();
        IdleMotion();
        // 同じコルーチン内で続けて実行するので、OnDestroyのStopCoroutineで一緒に止まる
        yield return RandomMove();
    }

    /// <summary>
    /// 空から落ちてきて着地し、キラキラ光って「NEW!」を出す。
    /// </summary>
    IEnumerator DoEntrance()
    {
        // 演出中にけんか相手として予約されないようにする
        IsBusy = true;
        Vector3 landingPosition = transform.position;
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color;

        // 上空から落下する（落ちる間は縦に伸ばして勢いを出す）
        transform.position = landingPosition + Vector3.up * entranceDropHeight;
        transform.localScale = Vector3.Scale(originalScale, new Vector3(0.85f, 1.2f, 1f));
        jumpTween = transform.DOMove(landingPosition, entranceDropDuration)
            .SetEase(Ease.InQuad)
            .SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        AudioManager.Instance.PlaySe(entranceSe);
        if (emoteEffect != null)
        {
            emoteEffect.PlaySparkles();
            emoteEffect.PlayNewBadge(newBadgeDuration);
        }

        // 着地のつぶれ
        transform.localScale = originalScale;
        yield return SquashAndStretch(originalScale, new Vector3(1.35f, 0.65f, 1f), 0.3f);

        // 光るように明滅させる。
        // 選択時の透過演出（アルファ変更）を潰さないよう、RGBだけを変える
        Tween glowTween = DOTween.To(() => 0f, t =>
            {
                Color c = Color.Lerp(originalColor, entranceGlowColor, t);
                c.a = spriteRenderer.color.a;
                spriteRenderer.color = c;
            }, 1f, 0.2f)
            .SetLoops(Mathf.Max(entranceGlowCount, 1) * 2, LoopType.Yoyo)
            .SetLink(gameObject);
        yield return glowTween.WaitForCompletion();

        transform.position = landingPosition;
        transform.localScale = originalScale;
        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, spriteRenderer.color.a);

        IsBusy = false;
    }

    void IdleMotion()
    {
        Vector3 startPosition = transform.position;
        startPosition.y += 0.2f;
        idleTween = transform.DOMove(startPosition, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject); // このオブジェクトが破棄されたときに自動でkillされる
    }

    IEnumerator RandomMove()
    {
        while (true)
        {
            float waitTime = Random.Range(0.1f, 10f);
            yield return new WaitForSeconds(waitTime);

            // けんか相手として他の動物から予約されている場合はここで待つ
            if (IsBusy)
            {
                continue;
            }

            if (idleTween != null && idleTween.IsActive())
            {
                idleTween.Kill();
            }

            switch (RollAction())
            {
                case AnimalAction.Eat:
                    yield return DoEat();
                    break;
                case AnimalAction.Sleep:
                    yield return DoSleep();
                    break;
                case AnimalAction.Fight:
                    yield return DoFight();
                    break;
                case AnimalAction.Celebrate:
                    yield return DoCelebrate();
                    break;
                default:
                    yield return DoWander();
                    break;
            }

            IdleMotion();
        }
    }

    private AnimalAction RollAction()
    {
        float roll = Random.Range(0f, 100f);
        if (IsLastRaceWinner())
        {
            if (roll < celebrateWeight) return AnimalAction.Celebrate;
            roll -= celebrateWeight;
        }
        if (roll < eatWeight) return AnimalAction.Eat;
        roll -= eatWeight;
        if (roll < sleepWeight) return AnimalAction.Sleep;
        roll -= sleepWeight;
        if (roll < fightWeight) return AnimalAction.Fight;
        return AnimalAction.Wander;
    }

    IEnumerator DoWander()
    {
        int jumpCount = Random.Range(1, 4);
        for (int i = 0; i < jumpCount; i++)
        {
            float moveX = Random.Range(-2f, 2f);
            spriteRenderer.flipX = moveX < 0; // 反対に進むので画像を反転

            Vector3 currentPosition = transform.position;
            currentPosition.x += moveX;

            jumpTween = transform.DOJump(currentPosition, 1f, 1, 1f)
                .SetLink(gameObject);

            yield return jumpTween.WaitForCompletion();
        }
    }

    private bool IsLastRaceWinner()
    {
        return dataHolder != null && dataHolder.Data != null
            && RaceHistoryStore.IsLastRaceWinner(dataHolder.Data.AnimalId);
    }

    IEnumerator DoCelebrate()
    {
        // 演出中にけんか相手として予約されないようにする
        IsBusy = true;
        Vector3 originalPosition = transform.position;
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color;
        bool originalFlipX = spriteRenderer.flipX;

        // 喜んでいる間は金色っぽく明滅させる。
        // 選択時の透過演出（アルファ変更）を潰さないよう、RGBだけを変える
        Tween glowTween = DOTween.To(() => 0f, t =>
            {
                Color c = Color.Lerp(originalColor, celebrateColor, t);
                c.a = spriteRenderer.color.a;
                spriteRenderer.color = c;
            }, 1f, 0.25f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        // その場で左右を向きながら小刻みに跳ねる（溜め→伸び→着地のつぶれ）
        int hopCount = Random.Range(minHopCount, maxHopCount + 1);
        for (int i = 0; i < hopCount; i++)
        {
            yield return SquashAndStretch(originalScale, new Vector3(1.15f, 0.85f, 1f), hopDuration * 0.3f);

            spriteRenderer.flipX = !spriteRenderer.flipX;
            Sequence hop = DOTween.Sequence().SetLink(gameObject);
            hop.Append(transform.DOJump(originalPosition, hopHeight, 1, hopDuration));
            hop.Insert(0f, transform.DOScale(Vector3.Scale(originalScale, new Vector3(0.9f, 1.15f, 1f)), hopDuration * 0.5f));
            hop.Insert(hopDuration * 0.5f, transform.DOScale(originalScale, hopDuration * 0.5f));
            jumpTween = hop;
            AudioManager.Instance.PlaySe(SeId.Jump);
            if (emoteEffect != null) emoteEffect.PlayNote();
            yield return hop.WaitForCompletion();
        }

        // 締めに大きくジャンプして一回転する
        yield return SquashAndStretch(originalScale, new Vector3(1.25f, 0.75f, 1f), 0.2f);

        // Billboardがtransformの向きを毎フレーム上書きするため、回転する間だけ止める
        if (billboard != null) billboard.enabled = false;

        Sequence finalJump = DOTween.Sequence().SetLink(gameObject);
        finalJump.Append(transform.DOJump(originalPosition, finalJumpHeight, 1, finalJumpDuration));
        finalJump.Insert(0f, transform.DORotate(new Vector3(0f, 0f, 360f), finalJumpDuration * 0.8f, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad));
        jumpTween = finalJump;
        AudioManager.Instance.PlaySe(SeId.HighJump);
        if (emoteEffect != null)
        {
            emoteEffect.PlayNote();
            emoteEffect.PlayNote();
        }
        yield return finalJump.WaitForCompletion();

        if (billboard != null) billboard.enabled = true;

        // 着地の余韻
        yield return transform.DOPunchScale(Vector3.Scale(originalScale, new Vector3(0.2f, -0.2f, 0f)), 0.4f, 6, 0.5f)
            .SetLink(gameObject).WaitForCompletion();

        glowTween.Kill();
        transform.position = originalPosition;
        transform.localScale = originalScale;
        spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, spriteRenderer.color.a);
        spriteRenderer.flipX = originalFlipX;

        IsBusy = false;
    }

    /// <summary>
    /// ジャンプ前の「溜め」として一瞬つぶれてから元の大きさに戻る。
    /// </summary>
    private IEnumerator SquashAndStretch(Vector3 originalScale, Vector3 squashRatio, float duration)
    {
        yield return transform.DOScale(Vector3.Scale(originalScale, squashRatio), duration * 0.5f)
            .SetEase(Ease.OutQuad).SetLink(gameObject).WaitForCompletion();
        yield return transform.DOScale(originalScale, duration * 0.5f)
            .SetEase(Ease.InQuad).SetLink(gameObject).WaitForCompletion();
    }

    IEnumerator DoEat()
    {
        Transform spot = animalActionScheduler.TryReserveFeedingSpot(out int spotIndex);
        reservedFeedingSpotIndex = spotIndex;
        if (spot == null)
        {
            // 餌場が空いていなければ通常移動で代替する
            yield return DoWander();
            yield break;
        }

        IsBusy = true;
        Vector3 originalPosition = transform.position;

        jumpTween = transform.DOJump(spot.position, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        // 頭を振って食べる動作を表現する
        int biteCount = Random.Range(minBiteCount, maxBiteCount);
        for (int i = 0; i < biteCount; i++)
        {
            AudioManager.Instance.PlaySe(SeId.Eating);
            if (emoteEffect != null) emoteEffect.PlayCrumbs();
            yield return transform.DOPunchPosition(Vector3.down * 0.3f, eatingDuration, 1, 0.5f)
                .SetLink(gameObject).WaitForCompletion();
        }

        jumpTween = transform.DOJump(originalPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        animalActionScheduler.ReleaseFeedingSpot(spotIndex);
        reservedFeedingSpotIndex = -1;
        IsBusy = false;
    }

    IEnumerator DoSleep()
    {
        Transform spot = animalActionScheduler.TryReserveBedSpot(out int spotIndex);
        reservedBedSpotIndex = spotIndex;
        if (spot == null)
        {
            // 寝床が空いていなければ通常移動で代替する
            yield return DoWander();
            yield break;
        }

        IsBusy = true;
        Vector3 originalPosition = transform.position;
        Vector3 originalScale = transform.localScale;
        Color originalColor = spriteRenderer.color;

        jumpTween = transform.DOJump(spot.position, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        // Billboardがtransformの向きを毎フレーム上書きするため、横になる間だけ止める
        if (billboard != null) billboard.enabled = false;

        AudioManager.Instance.PlaySe(SeId.Sleep);
        yield return transform.DORotate(new Vector3(0f, 0f, 90f), 0.5f, RotateMode.LocalAxisAdd)
            .SetLink(gameObject).WaitForCompletion();
        spriteRenderer.color = new Color(originalColor.r * 0.6f, originalColor.g * 0.6f, originalColor.b * 0.6f, originalColor.a);

        Tween breathingTween = transform.DOScale(originalScale * 1.05f, breathingDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        // 眠っている間は一定間隔で「Z」を出す
        float sleepingDuration = Random.Range(minSleepingDuration, maxSleepingDuration);
        float sleptTime = 0f;
        while (sleptTime < sleepingDuration)
        {
            if (emoteEffect != null) emoteEffect.PlaySleepZ();
            float wait = Mathf.Min(Mathf.Max(sleepZInterval, 0.1f), sleepingDuration - sleptTime);
            yield return new WaitForSeconds(wait);
            sleptTime += wait;
        }

        breathingTween.Kill();
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;

        yield return transform.DORotate(new Vector3(0f, 0f, -90f), 0.5f, RotateMode.LocalAxisAdd)
            .SetLink(gameObject).WaitForCompletion();
        if (billboard != null) billboard.enabled = true;

        jumpTween = transform.DOJump(originalPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        animalActionScheduler.ReleaseBedSpot(spotIndex);
        reservedBedSpotIndex = -1;
        IsBusy = false;
    }

    IEnumerator DoFight()
    {
        AnimalIdleAnimation opponent = animalActionScheduler.TryReserveOpponent(this);
        if (opponent == null)
        {
            // 近くにけんか相手がいなければ通常移動で代替する
            yield return DoWander();
            yield break;
        }

        IsBusy = true;
        Vector3 originalPosition = transform.position;
        Vector3 opponentOriginalPosition = opponent.transform.position;
        Vector3 midpoint = (originalPosition + opponentOriginalPosition) / 2f;

        Vector3 selfApproach = Vector3.Lerp(originalPosition, midpoint, 0.6f);
        Vector3 opponentApproach = Vector3.Lerp(opponentOriginalPosition, midpoint, 0.6f);

        opponent.StartFightAsOpponent(opponentApproach, opponentOriginalPosition);
        yield return RunFightExchange(selfApproach, originalPosition);

        IsBusy = false;
    }

    /// <summary>
    /// けんかを仕掛けられた側として呼ばれる。
    /// </summary>
    public void StartFightAsOpponent(Vector3 approachPosition, Vector3 originalPosition)
    {
        StartCoroutine(FightAsOpponentRoutine(approachPosition, originalPosition));
    }

    private IEnumerator FightAsOpponentRoutine(Vector3 approachPosition, Vector3 originalPosition)
    {
        yield return RunFightExchange(approachPosition, originalPosition);
        IsBusy = false;
        IdleMotion();
    }

    private IEnumerator RunFightExchange(Vector3 approachPosition, Vector3 originalPosition)
    {
        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
        }

        jumpTween = transform.DOJump(approachPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        int clashCount = Random.Range(minClashCount, maxClashCount + 1);
        Color originalColor = spriteRenderer.color;
        for (int i = 0; i < clashCount; i++)
        {
            AudioManager.Instance.PlaySe(SeId.Attack);
            if (emoteEffect != null) emoteEffect.PlayAnger();
            yield return transform.DOShakePosition(0.3f, 0.3f, 10, 90, false, true)
                .SetLink(gameObject).WaitForCompletion();
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }

        jumpTween = transform.DOJump(originalPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();
    }

    // このゲームオブジェクトが破棄されたときに呼び出されるメソッド。
    void OnDestroy()
    {
        if (randomMoveCoroutine != null)
        {
            StopCoroutine(randomMoveCoroutine);
        }

        if (idleTween != null && idleTween.IsActive())
        {
            idleTween.Kill();
        }

        if (jumpTween != null && jumpTween.IsActive())
        {
            jumpTween.Kill();
        }

        if (animalActionScheduler != null)
        {
            animalActionScheduler.Unregister(this);

            // 食事中・睡眠中に破棄された場合、餌場・寝床の予約が残らないように解除する
            if (reservedFeedingSpotIndex >= 0) animalActionScheduler.ReleaseFeedingSpot(reservedFeedingSpotIndex);
            if (reservedBedSpotIndex >= 0) animalActionScheduler.ReleaseBedSpot(reservedBedSpotIndex);
        }
    }
}
