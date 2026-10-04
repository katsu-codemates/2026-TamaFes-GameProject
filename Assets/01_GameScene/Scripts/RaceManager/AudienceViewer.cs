using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 観客オブジェクトの目印。
/// RaceSoundDirectorがカメラに観客が映っているかを判定するために、シーン内の観客を一覧で持つ。
/// </summary>
[RequireComponent(typeof(Renderer))]
public class AudienceViewer : MonoBehaviour
{
    private static readonly List<AudienceViewer> all = new List<AudienceViewer>();
    public static IReadOnlyList<AudienceViewer> All => all;

    public Renderer Renderer { get; private set; }

    private void Awake()
    {
        Renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
    }
}
