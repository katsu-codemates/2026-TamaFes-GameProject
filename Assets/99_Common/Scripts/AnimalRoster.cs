using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BaseSceneで読み込んだ動物リストを保持しておくためのコンテナ。
/// BaseSceneとGameSceneの両方のオブジェクトからこのアセットを参照する。
/// </summary>
[CreateAssetMenu(fileName = "AnimalRoster", menuName = "Racing/AnimalRoster")]
public class AnimalRoster : ScriptableObject
{
    [SerializeField] private List<AnimalData> animals = new List<AnimalData>();

    public IReadOnlyList<AnimalData> Animals => animals;

    ///<summary>
    /// BaseSceneで画像一覧を取得した後、ここにセットする
    /// </summary>
    public void SetAnimals(List<AnimalData> newAnimals)
    {
        animals = newAnimals;
    }

    public void Clear()
    {
        animals.Clear();
    }
}
