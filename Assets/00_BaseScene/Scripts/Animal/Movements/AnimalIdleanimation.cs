using System.Collections;
using DG.Tweening;
using UnityEngine;

public class AnimalIdleanimation : MonoBehaviour
{
    private enum AnimalAction { Wander, Eat, Sleep, Fight }

    [Header("特殊行動の抽選確率（%）。残りはWander（通常移動）になる")]
    [SerializeField] private float eatWeight = 10f;
    [SerializeField] private float sleepWeight = 10f;
    [SerializeField] private float fightWeight = 10f;

    Tween idleTween;
    Tween jumpTween;
    SpriteRenderer spriteRenderer;
    Billboard billboard;
    Coroutine randomMoveCoroutine;

    public bool IsBusy { get; set; }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        billboard = GetComponent<Billboard>();
        AnimalActionScheduler.Instance.Register(this);
        IdleMotion();
        randomMoveCoroutine = StartCoroutine(RandomMove());
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

    IEnumerator DoEat()
    {
        Transform spot = AnimalActionScheduler.Instance.TryReserveFeedingSpot(out int spotIndex);
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
        int biteCount = Random.Range(3, 6);
        for (int i = 0; i < biteCount; i++)
        {
            yield return transform.DOPunchPosition(Vector3.down * 0.3f, 0.3f, 1, 0.5f)
                .SetLink(gameObject).WaitForCompletion();
        }

        jumpTween = transform.DOJump(originalPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        AnimalActionScheduler.Instance.ReleaseFeedingSpot(spotIndex);
        IsBusy = false;
    }

    IEnumerator DoSleep()
    {
        Transform spot = AnimalActionScheduler.Instance.TryReserveBedSpot(out int spotIndex);
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

        yield return transform.DORotate(new Vector3(0f, 0f, 90f), 0.5f, RotateMode.LocalAxisAdd)
            .SetLink(gameObject).WaitForCompletion();
        spriteRenderer.color = new Color(originalColor.r * 0.6f, originalColor.g * 0.6f, originalColor.b * 0.6f, originalColor.a);

        Tween breathingTween = transform.DOScale(originalScale * 1.05f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);

        yield return new WaitForSeconds(Random.Range(3f, 6f));

        breathingTween.Kill();
        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;

        yield return transform.DORotate(new Vector3(0f, 0f, -90f), 0.5f, RotateMode.LocalAxisAdd)
            .SetLink(gameObject).WaitForCompletion();
        if (billboard != null) billboard.enabled = true;

        jumpTween = transform.DOJump(originalPosition, 1f, 1, 1f).SetLink(gameObject);
        yield return jumpTween.WaitForCompletion();

        AnimalActionScheduler.Instance.ReleaseBedSpot(spotIndex);
        IsBusy = false;
    }

    IEnumerator DoFight()
    {
        AnimalIdleanimation opponent = AnimalActionScheduler.Instance.TryReserveOpponent(this);
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

        int clashCount = Random.Range(3, 5);
        Color originalColor = spriteRenderer.color;
        for (int i = 0; i < clashCount; i++)
        {
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

        if (AnimalActionScheduler.HasInstance)
        {
            AnimalActionScheduler.Instance.Unregister(this);
        }
    }
}
