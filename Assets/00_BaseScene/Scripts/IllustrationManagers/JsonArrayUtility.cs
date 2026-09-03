using System;
using UnityEngine;

/// <summary>
/// トップレベルが配列のJSONを{items:[]}形式に変換するユーティリティ
/// </summary>
public static class JsonArrayUtility
{
    public static T[] FromJsonArray<T>(string json)
    {
        string wrapped = "{\"items\":" + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
        return wrapper.items ?? Array.Empty<T>();
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] items;
    }
}
