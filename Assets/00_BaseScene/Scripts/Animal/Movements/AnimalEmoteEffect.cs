using DG.Tweening;
using UnityEngine;

/// <summary>
/// 待機モーション中に表示する簡単なエフェクト（食べかす・Zzz・怒りマーク・音符）。
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
    /// エフェクト用のSpriteRendererを生成する。worldSizeは画像の高さ（ワールド単位）。
    /// </summary>
    private SpriteRenderer Spawn(Sprite sprite, Vector3 position, float worldSize, Color color)
    {
        var go = new GameObject($"Effect_{sprite.name}");
        go.transform.position = position + TowardCamera();

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
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
