using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 観客席（Viewer）がカメラに映りだしたら、歓声の効果音を徐々に大きくするクラス。
/// 観客席がカメラの視錐台に入っているかを毎フレーム判定し、
/// 映っている間は最大音量へ、映っていない間は最小音量へゆっくり近づける。
/// </summary>
public class CheerSoundController : MonoBehaviour
{
    [Header("判定に使うカメラ（未設定ならCamera.main）")]
    [SerializeField] private Camera targetCamera;

    [Header("歓声の効果音")]
    [SerializeField] private AudioClip cheerClip;

    [Header("観客席のRenderer（空ならシーン内の名前一致オブジェクトを自動収集）")]
    [SerializeField] private List<Renderer> viewerRenderers = new List<Renderer>();
    [SerializeField] private string viewerObjectName = "Viewer";

    [Header("音量")]
    [SerializeField, Range(0f, 1f)] private float minVolume = 0f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    [Header("観客席が映ってから最大音量になるまでの秒数")]
    [SerializeField] private float fadeInDuration = 2.5f;

    [Header("観客席が映らなくなってから最小音量に戻るまでの秒数（0以下なら下げない）")]
    [SerializeField] private float fadeOutDuration = 3f;

    private AudioSource audioSource;
    private Plane[] frustumPlanes = new Plane[6];

    // 現在観客席が映っているか（デバッグ・他クラス参照用）
    public bool IsViewerVisible { get; private set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (cheerClip != null) audioSource.clip = cheerClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 距離減衰させず、画面に映っているかだけで音量を決める
        audioSource.volume = minVolume;

        if (viewerRenderers.Count == 0) CollectViewerRenderers();
    }

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (audioSource.clip != null) audioSource.Play();
    }

    // シーン内から観客席オブジェクトを名前で探してRendererを集める
    private void CollectViewerRenderers()
    {
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.gameObject.name == viewerObjectName) viewerRenderers.Add(r);
        }

        if (viewerRenderers.Count == 0)
        {
            Debug.LogWarning($"[CheerSoundController] 観客席（{viewerObjectName}）が見つかりませんでした");
        }
    }

    private void Update()
    {
        if (targetCamera == null || audioSource == null) return;

        IsViewerVisible = IsAnyViewerInView();

        // スロー演出（timeScale変更）中でも音量の変化速度が変わらないようunscaledを使う
        float dt = Time.unscaledDeltaTime;
        float range = Mathf.Max(maxVolume - minVolume, 0.0001f);

        if (IsViewerVisible)
        {
            float step = fadeInDuration > 0f ? range / fadeInDuration * dt : range;
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, maxVolume, step);
        }
        else if (fadeOutDuration > 0f)
        {
            float step = range / fadeOutDuration * dt;
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, minVolume, step);
        }
    }

    // いずれかの観客席がカメラの視錐台に入っているか
    private bool IsAnyViewerInView()
    {
        GeometryUtility.CalculateFrustumPlanes(targetCamera, frustumPlanes);

        foreach (var r in viewerRenderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, r.bounds)) return true;
        }
        return false;
    }
}
