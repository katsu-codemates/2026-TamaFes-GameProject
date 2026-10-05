using System;
using UnityEngine;

[Serializable]
public class AnimalData
{
    public string animalName;
    public string imageBase64;
    public string createdAt;


    public float speed;
    public float power;
    public float wisdom;
    public float luck;
    public float stamina;

    /// <summary>
    /// レース履歴などで動物を識別するためのID。
    /// 本番はcreatedAt、テスト画像（createdAtなし）はファイル名で代用する。
    /// </summary>
    public string AnimalId => !string.IsNullOrEmpty(createdAt) ? createdAt : animalName;
}
