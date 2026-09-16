using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 動物の特殊行動（餌を食べる・眠る・けんかする）で使う共有リソースを調停する。
/// 餌場・寝床の使用状況の管理と、けんか相手のマッチングを行う。
/// 素材が未確定のため、餌場・寝床はプレースホルダの色付きスプライトとして実行時に生成する。
/// </summary>
public class AnimalActionScheduler : MonoBehaviour
{
    private static AnimalActionScheduler instance;

    public static AnimalActionScheduler Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(AnimalActionScheduler));
                instance = go.AddComponent<AnimalActionScheduler>();
            }
            return instance;
        }
    }

    /// <summary>
    /// シーン終了処理などでインスタンス生成を誘発せずに存在確認したい場合に使う。
    /// </summary>
    public static bool HasInstance => instance != null;

    [Header("餌場（プレースホルダ配置、素材ができたらスプライトを差し替え）")]
    [SerializeField] private Vector3[] feedingSpotPositions =
    {
        new Vector3(-4f, 5f, -13f),
        new Vector3(4f, 5f, -7f),
    };

    [Header("寝床（プレースホルダ配置、素材ができたらスプライトを差し替え）")]
    [SerializeField] private Vector3[] bedSpotPositions =
    {
        new Vector3(4f, 5f, -13f),
        new Vector3(-4f, 5f, -7f),
    };

    [Header("けんか相手を探す最大距離")]
    [SerializeField] private float fightOpponentMaxDistance = 6f;

    private readonly List<ActionSpot> feedingSpots = new List<ActionSpot>();
    private readonly List<ActionSpot> bedSpots = new List<ActionSpot>();
    private readonly List<AnimalIdleanimation> activeAnimals = new List<AnimalIdleanimation>();

    private class ActionSpot
    {
        public Transform Transform;
        public bool InUse;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BuildSpots(feedingSpotPositions, feedingSpots, "FeedingSpot(Placeholder)", new Color(0.9f, 0.7f, 0.2f));
        BuildSpots(bedSpotPositions, bedSpots, "BedSpot(Placeholder)", new Color(0.5f, 0.4f, 0.9f));
    }

    private void BuildSpots(Vector3[] positions, List<ActionSpot> spots, string namePrefix, Color color)
    {
        var placeholderSprite = CreatePlaceholderSprite();
        for (int i = 0; i < positions.Length; i++)
        {
            var go = new GameObject($"{namePrefix}_{i}");
            go.transform.SetParent(transform, false);
            go.transform.position = positions[i];

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = placeholderSprite;
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Simple;
            go.transform.localScale = Vector3.one * 1.5f;

            spots.Add(new ActionSpot { Transform = go.transform, InUse = false });
        }
    }

    private static Sprite placeholderSprite;

    private static Sprite CreatePlaceholderSprite()
    {
        if (placeholderSprite == null)
        {
            var texture = Texture2D.whiteTexture;
            placeholderSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                32f);
        }
        return placeholderSprite;
    }

    public void Register(AnimalIdleanimation animal)
    {
        if (!activeAnimals.Contains(animal))
        {
            activeAnimals.Add(animal);
        }
    }

    public void Unregister(AnimalIdleanimation animal)
    {
        activeAnimals.Remove(animal);
    }

    public Transform TryReserveFeedingSpot(out int spotIndex) => TryReserveSpot(feedingSpots, out spotIndex);

    public Transform TryReserveBedSpot(out int spotIndex) => TryReserveSpot(bedSpots, out spotIndex);

    public void ReleaseFeedingSpot(int spotIndex) => ReleaseSpot(feedingSpots, spotIndex);

    public void ReleaseBedSpot(int spotIndex) => ReleaseSpot(bedSpots, spotIndex);

    private Transform TryReserveSpot(List<ActionSpot> spots, out int spotIndex)
    {
        for (int i = 0; i < spots.Count; i++)
        {
            if (!spots[i].InUse)
            {
                spots[i].InUse = true;
                spotIndex = i;
                return spots[i].Transform;
            }
        }
        spotIndex = -1;
        return null;
    }

    private void ReleaseSpot(List<ActionSpot> spots, int spotIndex)
    {
        if (spotIndex >= 0 && spotIndex < spots.Count)
        {
            spots[spotIndex].InUse = false;
        }
    }

    /// <summary>
    /// けんか相手を探して予約する。相手が見つかった場合、相手のIsBusyもtrueにする。
    /// </summary>
    public AnimalIdleanimation TryReserveOpponent(AnimalIdleanimation self)
    {
        AnimalIdleanimation best = null;
        float bestDistance = fightOpponentMaxDistance;

        foreach (var candidate in activeAnimals)
        {
            if (candidate == null || candidate == self || candidate.IsBusy)
            {
                continue;
            }

            float distance = Vector3.Distance(self.transform.position, candidate.transform.position);
            if (distance <= bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        if (best != null)
        {
            best.IsBusy = true;
        }
        return best;
    }
}
