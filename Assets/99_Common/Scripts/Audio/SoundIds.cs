/// <summary>
/// BGMの識別子。SoundCatalogでAudioClipと対応付ける。
/// ※ Inspector上では数値でシリアライズされるため、新しい値は必ず末尾に追加すること。
/// </summary>
public enum BgmId
{
    None = 0,
    Base = 1,
    Race = 2,
    Entry = 3,
    Result = 4,
}

/// <summary>
/// SEの識別子。SoundCatalogでAudioClipと対応付ける。
/// ※ Inspector上では数値でシリアライズされるため、新しい値は必ず末尾に追加すること。
/// </summary>
public enum SeId
{
    None = 0,
    Cheer = 1,
    Start = 2,
    Finish = 3,
    Accident = 4,
    Miracle = 5,
    Eating = 6,
    Sleep = 7,
    Attack = 8,
}
