using UnityEngine;


/// <summary>
/// レース計算式の調整用パラメータ式。
/// レースを回しながら調整できるようにしている。
/// </summary>
[CreateAssetMenu(fileName = "RaceTuning", menuName = "Racing/RaceTuningConfig")]
public class RaceTuningConfig : ScriptableObject
{
    [Header("加速フェーズ")]
    public float baseSpeed = 8f; // 最高速度の下限
    public float speedRange = 6f; // 最高速度の幅（speedパラメータで加算される分）
    public float baseAccel = 10f; // 加速度の下限
    public float accelRange = 15f; // 加速度の幅（powerパラメータで加算される分）

    [Header("フェーズ境界（個体ごとにこの範囲でランダム化）")]
    public float earlyPhaseEndMin = 0.15f;
    public float earlyPhaseEndMax = 0.25f;
    public float latePhaseStartMin = 0.70f;
    public float latePhaseStartMax = 0.80f;

    [Header("スタミナ")]
    public float staminaMin = 50f;  // 初期スタミナの下限
    public float staminaRange = 100f; // 初期スタミナの幅(staminaパラメータで加算される分)
    public float baseConsumption = 6f; // 基本消費量
    public float wisdomEfficiency = 0.5f; // 賢さによる消費軽減効果(0~1)

    [Header("終盤：バテ")]
    public float fatigueThreshold = 0.3f; // このスタミナ比率を切ったらバテ始める
    public float minFatigueFactor = 0.6f; // 最大で何倍まで落ちるか(0.6なら-40%)

    [Header("終盤：スパート")]
    public float spurtMinStaminaRatio = 0.35f; // これ以上スタミナが残っていないとスパートしない
    public float suprtBaseChance = 0.4f; // 賢さ100の場合の発動確率
    public float spurtBonusRange = 4f; // スパート時の速度ボーナス幅（賢さで変動）
    public float spurtDuration = 2.5f; // スパート持続時間

    [Header("運：アクシデント")]
    public float baseAccidentChancePerSeccond = 0.02f; // 運0の場合の発生率（秒あたり）
    public float luckReducttionFactor = 0.8f; // 運による軽減効果(0~1)
    public float accidentDuration = 1.0f; // よろけている時間(秒)
    public float accidentSlowMin = 0.5f; // 減速倍率の範囲
    public float accidentSlowMax = 0.7f;

    [Header("運：ミラクル")]
    public float miracleChancePerSecond = 0.004f;
    public float miracleDuration = 1.0f;
    public float miracleBoostMin = 1.3f;
    public float miracleBoostMax = 1.6f;
}
