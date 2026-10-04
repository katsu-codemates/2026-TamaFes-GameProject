using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BgmId / SeId と AudioClip の対応表。
/// AudioManagerから参照し、IDを指定するだけで音を鳴らせるようにする。
/// </summary>
[CreateAssetMenu(fileName = "SoundCatalog", menuName = "Audio/SoundCatalog")]
public class SoundCatalog : ScriptableObject
{
    [Serializable]
    public class BgmEntry
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Serializable]
    public class SeEntry
    {
        public SeId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("再生ごとにこの範囲からランダムにピッチを選ぶ(x=最小, y=最大)")]
        public Vector2 pitchRange = new Vector2(1f, 1f);
    }

    [SerializeField] private List<BgmEntry> bgmList = new List<BgmEntry>();
    [SerializeField] private List<SeEntry> seList = new List<SeEntry>();

    private Dictionary<BgmId, BgmEntry> bgmTable;
    private Dictionary<SeId, SeEntry> seTable;

    public bool TryGetBgm(BgmId id, out BgmEntry entry)
    {
        if (bgmTable == null) BuildTables();
        if (bgmTable.TryGetValue(id, out entry) && entry.clip != null) return true;

        Debug.LogWarning($"[SoundCatalog] BGM「{id}」のクリップが登録されていません。");
        return false;
    }

    public bool TryGetSe(SeId id, out SeEntry entry)
    {
        if (seTable == null) BuildTables();
        if (seTable.TryGetValue(id, out entry) && entry.clip != null) return true;

        Debug.LogWarning($"[SoundCatalog] SE「{id}」のクリップが登録されていません。");
        return false;
    }

    private void BuildTables()
    {
        bgmTable = new Dictionary<BgmId, BgmEntry>();
        foreach (var e in bgmList)
        {
            if (e == null) continue;
            if (bgmTable.ContainsKey(e.id))
            {
                Debug.LogWarning($"[SoundCatalog] BGM「{e.id}」が重複して登録されています。先頭のものを使います。");
                continue;
            }
            bgmTable.Add(e.id, e);
        }

        seTable = new Dictionary<SeId, SeEntry>();
        foreach (var e in seList)
        {
            if (e == null) continue;
            if (seTable.ContainsKey(e.id))
            {
                Debug.LogWarning($"[SoundCatalog] SE「{e.id}」が重複して登録されています。先頭のものを使います。");
                continue;
            }
            seTable.Add(e.id, e);
        }
    }

    // Inspectorで編集されたらキャッシュを作り直す
    private void OnValidate()
    {
        bgmTable = null;
        seTable = null;
    }
}
