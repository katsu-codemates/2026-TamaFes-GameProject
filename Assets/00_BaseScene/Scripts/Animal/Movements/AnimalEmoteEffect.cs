using DG.Tweening;
using UnityEngine;

/// <summary>
/// 待機モーション中に表示する簡単なエフェクト（食べかす・Zzz・怒りマーク・音符・新規登場のキラキラとNEW!）。
/// 動物と同じく単一画像をDOTweenで動かし、終わったら破棄する。
/// エフェクトは動物の子にしない（寝転んだときの回転や呼吸のスケールの影響を受けないようにするため）。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AnimalEmoteEffect : MonoBehaviour
{
    [Header("エフェクト画像（差し替え可）")]
    [SerializeField] private Sprite crumbSprite;
    [SerializeField] private Sprite sleepSprite;
    [SerializeField] private Sprite angerSprite;
    [SerializeField] private Sprite noteSprite;
    [SerializeField] private Sprite sparkleSprite;
    [SerializeField] private Sprite newBadgeSprite;

    [Header("食べかす")]
    [SerializeField] private int minCrumbCount = 3;
    [SerializeField] private int maxCrumbCount = 5;
    [SerializeField] private float crumbSize = 0.15f;
    [SerializeField] private float crumbSpread = 0.9f;
    [SerializeField] private float crumbJumpHeight = 0.5f;
    [SerializeField] private float crumbDuration = 0.6f;
    [SerializeField] private Color[] crumbColors =
    {
        new Color(0.62f, 0.42f, 0.22f, 1f),
        new Color(0.85f, 0.68f, 0.38f, 1f),
        new Color(0.95f, 0.85f, 0.45f, 1f),
    };

    [Header("眠る（Z）")]
    [SerializeField] private float sleepSize = 0.5f;
    [SerializeField] private float sleepRise = 1.2f;
    [SerializeField] private float sleepDuration = 1.8f;

    [Header("けんか（怒りマーク）")]
    [SerializeField] private float angerSize = 0.6f;
    [SerializeField] private float angerDuration = 0.6f;

    [Header("喜ぶ（音符）")]
    [SerializeField] private float noteSize = 0.5f;
    [SerializeField] private float noteRise = 1.2f;
    [SerializeField] private float noteSway = 0.25f;
    [SerializeField] private float noteDuration = 1.2f;

    [Header("新規登場（キラキラ）")]
    [SerializeField] private int sparkleCount = 6;
    [SerializeField] private float sparkleSize = 1.2f;
    [SerializeField] private float sparkleDuration = 0.9f;

    [Header("新規登場（NEW!）")]
    [SerializeField] private float newBadgeSize = 1.5f;
    [Tooltip("頭のてっぺんからどれだけ上に出すか")]
    [SerializeField] private float newBadgeOffset = 0.5f;
    [SerializeField] private float newBadgeBob = 0.2f;

    [Header("共通")]
    [Tooltip("動物の画像に隠れないよう、カメラ側へずらす距離")]
    [SerializeField] private float towardCameraOffset = 0.3f;

    private SpriteRenderer ownerRenderer;

    private void Awake()
    {
        ownerRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 口元（画像の下寄り）から食べかすを左右に飛び散らせる。
    /// </summary>
    public void PlayCrumbs()
    {
        if (crumbSprite == null) return;

        Bounds bounds = ownerRenderer.bounds;
        Vector3 origin = bounds.center + Vector3.down * bounds.extents.y * 0.3f;
        Vector3 right = CameraRight();

        int count = Random.Range(minCrumbCount, maxCrumbCount + 1);
        for (int i = 0; i < count; i++)
        {
            Color color = crumbColors.Length > 0 ? crumbColors[Random.Range(0, crumbColors.Length)] : Color.white;
            float size = crumbSize * Random.Range(0.7f, 1.3f);
            SpriteRenderer crumb = Spawn(crumbSprite, origin, size, color);

            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 target = origin
                + right * side * Random.Range(crumbSpread * 0.3f, crumbSpread)
                + Vector3.down * Random.Range(0.1f, 0.4f);
            float duration = crumbDuration * Random.Range(0.8f, 1.2f);

            Sequence seq = DOTween.Sequence().SetLink(crumb.gameObject);
            seq.Append(crumb.transform.DOJump(target, crumbJumpHeight * Random.Range(0.6f, 1.2f), 1, duration).SetEase(Ease.Linear));
            seq.Insert(duration * 0.5f, crumb.DOFade(0f, duration * 0.5f));
            seq.Insert(duration * 0.5f, crumb.transform.DOScale(0f, duration * 0.5f));
            seq.OnComplete(() => Destroy(crumb.gameObject));
        }
    }

    /// <summary>
    /// 頭の斜め上から「Z」が揺れながら浮かび上がって消えていく。
    /// </summary>
    public void PlaySleepZ()
    {
        if (sleepSprite == null) return;

        Bounds bounds = ownerRenderer.bounds;
        Vector3 right = CameraRight();
        Vector3 origin = bounds.center + Vector3.up * bounds.extents.y * 0.8f + right * bounds.extents.x * 0.4f;
        SpriteRenderer z = Spawn(sleepSprite, origin, sleepSize, Color.white);

        Vector3 endScale = z.transform.localScale;
        z.transform.localScale = endScale * 0.4f;
        Color c = z.color;
        z.color = new Color(c.r, c.g, c.b, 0f);

        Sequence seq = DOTween.Sequence().SetLink(z.gameObject);
        seq.Append(z.transform.DOMove(origin + Vector3.up * sleepRise + right * sleepRise * 0.5f, sleepDuration).SetEase(Ease.OutSine));
        seq.Insert(0f, z.transform.DOScale(endScale, sleepDuration * 0.6f).SetEase(Ease.OutQuad));
        seq.Insert(0f, z.DOFade(c.a, sleepDuration * 0.2f));
        seq.Insert(sleepDuration * 0.6f, z.DOFade(0f, sleepDuration * 0.4f));
        seq.OnComplete(() => Destroy(z.gameObject));
    }

    /// <summary>
    /// 頭の右上に怒りマークがポンと出て、脈打ってから消える。
    /// </summary>
    public void PlayAnger()
    {
        if (angerSprite == null) return;

        Bounds bounds = ownerRenderer.bounds;
        Vector3 right = CameraRight();
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 origin = bounds.center
            + Vector3.up * bounds.extents.y * Random.Range(0.6f, 0.9f)
            + right * side * bounds.extents.x * Random.Range(0.5f, 0.8f);
        SpriteRenderer anger = Spawn(angerSprite, origin, angerSize, Color.white);

        Vector3 endScale = anger.transform.localScale;
        anger.transform.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence().SetLink(anger.gameObject);
        seq.Append(anger.transform.DOScale(endScale, angerDuration * 0.3f).SetEase(Ease.OutBack, 3f));
        seq.Append(anger.transform.DOPunchScale(endScale * 0.15f, angerDuration * 0.4f, 4, 0.5f));
        seq.Append(anger.DOFade(0f, angerDuration * 0.3f));
        seq.OnComplete(() => Destroy(anger.gameObject));
    }

    /// <summary>
    /// 頭の上に音符が出て、左右に揺れながら浮かび上がって消えていく。
    /// </summary>
    public void PlayNote()
    {
        if (noteSprite == null) return;

        Bounds bounds = ownerRenderer.bounds;
        Vector3 right = CameraRight();
        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 origin = bounds.center
            + Vector3.up * bounds.extents.y * 0.9f
            + right * side * bounds.extents.x * Random.Range(0.2f, 0.7f);
        SpriteRenderer note = Spawn(noteSprite, origin, noteSize, Color.white);

        Vector3 endScale = note.transform.localScale;
        note.transform.localScale = endScale * 0.5f;

        Sequence seq = DOTween.Sequence().SetLink(note.gameObject);
        seq.Append(note.transform.DOMoveY(origin.y + noteRise, noteDuration).SetEase(Ease.OutQuad));
        // 左右の揺れはカメラから見た横方向に付ける
        seq.Insert(0f, DOTween.To(() => 0f, t =>
            {
                Vector3 p = note.transform.position;
                Vector3 baseXZ = new Vector3(origin.x, p.y, origin.z);
                note.transform.position = baseXZ + right * Mathf.Sin(t * Mathf.PI * 2f) * noteSway;
            }, 2f, noteDuration).SetEase(Ease.Linear));
        seq.Insert(0f, note.transform.DOScale(endScale, noteDuration * 0.25f).SetEase(Ease.OutBack));
        seq.Insert(noteDuration * 0.6f, note.DOFade(0f, noteDuration * 0.4f));
        seq.OnComplete(() => Destroy(note.gameObject));
    }

    /// <summary>
    /// 体の周りにキラキラがポンと出て、瞬きながら小さくなって消える。新規登場の演出用。
    /// </summary>
    public void PlaySparkles()
    {
        if (sparkleSprite == null) return;

        Bounds bounds = ownerRenderer.bounds;
        Vector3 right = CameraRight();

        for (int i = 0; i < sparkleCount; i++)
        {
            // 体の輪郭あたりにぐるっと散らす
            float angle = (i + Random.Range(-0.3f, 0.3f)) / Mathf.Max(sparkleCount, 1) * Mathf.PI * 2f;
            Vector3 origin = bounds.center
                + right * Mathf.Cos(angle) * bounds.extents.x * Random.Range(0.8f, 1.2f)
                + Vector3.up * Mathf.Sin(angle) * bounds.extents.y * Random.Range(0.8f, 1.2f);
            float size = sparkleSize * Random.Range(0.7f, 1.3f);
            SpriteRenderer sparkle = Spawn(sparkleSprite, origin, size, Color.white);

            Vector3 endScale = sparkle.transform.localScale;
            sparkle.transform.localScale = Vector3.zero;
            float delay = Random.Range(0f, sparkleDuration * 0.3f);
            float duration = sparkleDuration * Random.Range(0.8f, 1.2f);

            // Billboardが回転を毎フレーム上書きするため、回転は使わず拡大縮小で瞬かせる
            Sequence seq = DOTween.Sequence().SetLink(sparkle.gameObject);
            seq.AppendInterval(delay);
            seq.Append(sparkle.transform.DOScale(endScale, duration * 0.35f).SetEase(Ease.OutBack, 3f));
            seq.Append(sparkle.transform.DOScale(endScale * 0.6f, duration * 0.15f));
            seq.Append(sparkle.transform.DOScale(endScale, duration * 0.15f));
            seq.Append(sparkle.transform.DOScale(0f, duration * 0.35f).SetEase(Ease.InQuad));
            seq.Insert(delay + duration * 0.65f, sparkle.DOFade(0f, duration * 0.35f));
            seq.OnComplete(() => Destroy(sparkle.gameObject));
        }
    }

    /// <summary>
    /// 頭上に「NEW!」をポンと出し、ふわふわさせながらduration秒表示して消す。
    /// 動物は演出のあとすぐ歩き出すので、表示中は毎フレーム頭上に追従させる。
    /// </summary>
    public void PlayNewBadge(float duration)
    {
        if (newBadgeSprite == null) return;

        SpriteRenderer badge = Spawn(newBadgeSprite, BadgePosition(0f), newBadgeSize, Color.white);
        Vector3 endScale = badge.transform.localScale;
        badge.transform.localScale = Vector3.zero;
        Color baseColor = badge.color;
        baseColor.a = 1f;

        float popDuration = 0.35f;
        float fadeDuration = 0.4f;
        float total = Mathf.Max(duration, popDuration + fadeDuration);

        // 位置とアルファを毎フレーム更新する（エフェクトは動物の子にしない方針なので手動で追従させる）
        Tween follow = DOTween.To(() => 0f, elapsed =>
            {
                if (badge == null) return;
                float bob = Mathf.Sin(elapsed * Mathf.PI * 2f) * newBadgeBob;
                badge.transform.position = BadgePosition(bob);
                // 選択時に他の動物が半透明になっている場合は合わせる。最後はフェードアウトする
                float fade = Mathf.Clamp01((total - elapsed) / fadeDuration);
                badge.color = new Color(baseColor.r, baseColor.g, baseColor.b, ownerRenderer.color.a * fade);
            }, total, total)
            .SetEase(Ease.Linear);

        // 動物が破棄されたらNEW!も一緒に消えるよう、動物側にリンクする
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(badge.transform.DOScale(endScale, popDuration).SetEase(Ease.OutBack, 3f));
        seq.Insert(0f, follow);
        seq.OnKill(() =>
        {
            if (badge != null) Destroy(badge.gameObject);
        });
    }

    private Vector3 BadgePosition(float bob)
    {
        Bounds bounds = ownerRenderer.bounds;
        Vector3 top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        return top + Vector3.up * (newBadgeOffset + newBadgeSize * 0.5f + bob) + TowardCamera();
    }

    /// <summary>
    /// エフェクト用のSpriteRendererを生成する。worldSizeは画像の高さ（ワールド単位）。
    /// </summary>
    private SpriteRenderer Spawn(Sprite sprite, Vector3 position, float worldSize, Color color)
    {
        var go = new GameObject($"Effect_{sprite.name}");
        go.transform.position = position + TowardCamera();

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        // BillboardはLookAtで+Zをカメラに向けるため、スプライトを裏側から見ることになり左右が反転する。それを打ち消す
        renderer.flipX = true;
        renderer.sharedMaterial = ownerRenderer.sharedMaterial;
        renderer.sortingLayerID = ownerRenderer.sortingLayerID;
        renderer.sortingOrder = ownerRenderer.sortingOrder + 1;
        // 選択時に他の動物が半透明になっている場合はエフェクトも合わせる
        color.a *= ownerRenderer.color.a;
        renderer.color = color;

        float spriteHeight = Mathf.Max(sprite.bounds.size.y, 0.0001f);
        go.transform.localScale = Vector3.one * (worldSize / spriteHeight);

        go.AddComponent<Billboard>();
        return renderer;
    }

    private Vector3 CameraRight()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.right : Vector3.right;
    }

    private Vector3 TowardCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized * towardCameraOffset : Vector3.zero;
    }
}
