using Unity.Burst.CompilerServices;
using UnityEngine;

/// <summary>
/// レース進行の計算式クラス。
/// 加速⇒巡行⇒終盤の3フェーズ構造に、
/// 運要素（アクシデント・ミラクル）を独立したイベントとして重ねている。
/// </summary>
public class RaceSimulator
{
    /// <summary>
    /// レース開始時に一回だけ呼ぶメソッド。個体ごとのパラメータを、
    /// 実際にレースで使う値に変換する。
    /// </summary>
    public static void Initialize(RaceParticipant participant, RaceTuningConfig raceTuning)
    {
        float speedNorm = participant.animalData.speed / 100f;
        float powerNorm = participant.animalData.power / 100f;
        float staminaNorm = participant.animalData.stamina / 100f;

        participant.maxSpeed = raceTuning.baseSpeed + speedNorm * raceTuning.speedRange;
        participant.accelerarion = raceTuning.baseAccel + powerNorm * raceTuning.accelRange;
        participant.initialStamina = raceTuning.staminaMin + staminaNorm * raceTuning.staminaRange;
        
        // フェーズ境界は個体ごとにランダム化（全員が同時に切り替わらないようにする）
        participant.earlyPhaseEnd = Random.Range(raceTuning.earlyPhaseEndMin, raceTuning.earlyPhaseEndMax);
        participant.latePhaseStart = Random.Range(raceTuning.latePhaseStartMin, raceTuning.latePhaseStartMax);

        participant.progress = 0f;
        participant.currentSpeed = 0f;
        participant.currentStamina = participant.initialStamina;
        participant.isFinished = false;
        participant.hasRolledLatePhase = false;
        participant.isSpurting = false;
        participant.isAccident = false;
        participant.isMiracle = false;
    }

    /// <summary>
    /// 毎フレーム呼び、progressとcurrentSpeedを更新する。
    /// </summary>
    public static void Tick(RaceParticipant participant, float deltaTime, RaceTuningConfig raceTuning)
    {
        if (participant.isFinished) return;

        // 運要素はフェーズに関係なく毎フレーム抽選
        UpdateLuckEvents(participant, deltaTime, raceTuning);

        if (participant.progress < participant.earlyPhaseEnd)
        {
            TickAccelerationPhase(participant, deltaTime);
        }
        else if (participant.progress < participant.latePhaseStart)
        {
            TickCruisePhase(participant, deltaTime, raceTuning);
        }
        else
        {
            TickLatePhase(participant, deltaTime, raceTuning);
        }

        // 運イベントによる速度倍率を最後にまとめて適用
        float eventMultiplier = 1f;
        if (participant.isAccident) eventMultiplier *= participant.accidentSlowFactor;
        if (participant.isMiracle) eventMultiplier *= participant.miracleBoost;

        float effectiveSpeed = participant.currentSpeed * eventMultiplier;
        participant.progress = Mathf.Clamp01(participant.progress + effectiveSpeed * deltaTime / RaceTrack.TrackLength);

        if (participant.progress >= 1f)
        {
            participant.isFinished = true;
        }
    }

    // 序盤；加速フェーズ
    private static void TickAccelerationPhase(RaceParticipant participant, float deltaTime)
    {
        participant.currentSpeed = 
            Mathf.Min(
                participant.maxSpeed,
                participant.currentSpeed + participant.accelerarion * deltaTime
            );
    }

    // 中盤：巡行フェーズ
    private static void TickCruisePhase(RaceParticipant participant, float deltaTime, RaceTuningConfig raceTuning)
    {
        participant.currentSpeed = participant.maxSpeed;
        ConsumeStamina(participant, deltaTime, raceTuning);
    }

    // 終盤：ラストスパート
    private static void TickLatePhase(RaceParticipant participant, float deltaTime, RaceTuningConfig raceTuning)
    {
        // 終盤突入時にスパートするか抽選
        if (!participant.hasRolledLatePhase)
        {
            RollSpurt(participant, raceTuning);
            participant.hasRolledLatePhase = true;
        }

        ConsumeStamina(participant, deltaTime, raceTuning);

        float staminaRatio = participant.currentStamina / participant.initialStamina;

        // バテ判定：スタミナ比率が閾値を切っていたら減速する
        float fatigueFactor = 1f;
        if (staminaRatio < raceTuning.fatigueThreshold)
        {
            // minFatigueFactor ~ 1f　の間で何倍減速するかを決める。
            float t = (raceTuning.fatigueThreshold - staminaRatio) / raceTuning.fatigueThreshold;
            fatigueFactor = Mathf.Lerp(1f, raceTuning.minFatigueFactor, t);
        }

        // スパート中なら速度をその分を足す
        float spurtBonus = 0f;
        if (participant.isSpurting)
        {
            participant.spurtTimer -= deltaTime;
            spurtBonus = participant.spurtBonusValue;
            if (participant.spurtTimer <= 0f) participant.isSpurting = false;
        }

        participant.currentSpeed = (participant.maxSpeed + spurtBonus) * fatigueFactor;
    }

    private static void ConsumeStamina(RaceParticipant participant, float deltaTime, RaceTuningConfig raceTuning)
    {
        if (participant.currentStamina <= 0f) return;

        float wisdomNorm = participant.animalData.wisdom / 100f;
        float spurtConsume = 1f;
        if (participant.isSpurting) spurtConsume = 1f + raceTuning.spurtConsumption * (1f - wisdomNorm * raceTuning.wisdomEfficiency);
        float consumption = raceTuning.baseConsumption * (1f - wisdomNorm * raceTuning.wisdomEfficiency) * spurtConsume; // 賢さが高いほど消費倍率が低い
        participant.currentStamina = Mathf.Max(0f, participant.currentStamina - consumption * deltaTime);
    }
    private static void RollSpurt(RaceParticipant participant, RaceTuningConfig raceTuning)
    {
        float staminaRatio = participant.currentStamina / participant.initialStamina;
        if (staminaRatio <= raceTuning.spurtMinStaminaRatio) return; // スタミナが一定比率以下ならスパート抽選しない

        float staminaNorm = participant.animalData.stamina / 100f;
        float chance = staminaNorm * raceTuning.spurtBonusRange; // スタミナステータスが高いほどスパートしやすい

        if (Random.value < chance)
        {
            participant.isSpurting = true;
            participant.spurtTimer = raceTuning.spurtDuration;
            participant.spurtBonusValue = staminaNorm * raceTuning.spurtBonusRange;
        }
    }

    // 運：アクシデント・ミラクル
    private static void UpdateLuckEvents(RaceParticipant participant, float deltaTime, RaceTuningConfig raceTuning)
    {
        // 運要素中はタイマーを減らすだけ
        if (participant.isAccident)
        {
            participant.accidentTimer -= deltaTime;
            if (participant.accidentTimer <= 0f) participant.isAccident = false;
            return;
        }

        if (participant.isMiracle)
        {
            participant.miracleTimer -= deltaTime;
            if (participant.miracleTimer <= 0f) participant.isMiracle = false;
            return;
        }

        // 運要素中はreturnされるため抽選しない
        float luckNorm = participant.animalData.luck / 100f;

        // アクシデント（運が高いほど発生しにくい）
        float accidentChance = raceTuning.baseAccidentChancePerSeccond * (1f - luckNorm * raceTuning.luckReducttionFactor);
        if (Random.value < accidentChance * deltaTime) // deltaTimeをかけることで、毎フレームではなく毎秒の抽選
        {
            participant.isAccident = true;
            participant.accidentTimer = raceTuning.accidentDuration;
            participant.accidentSlowFactor = Random.Range(raceTuning.accidentSlowMin, raceTuning.accidentSlowMax);
            return;
        }

        // ミラクル（運に関係なく全員共通の確率で発生）
        if (Random.value < raceTuning.miracleChancePerSecond * deltaTime)
        {
            participant.isMiracle = true;
            participant.miracleTimer = raceTuning.miracleDuration;
            participant.miracleBoost = Random.Range(raceTuning.miracleBoostMin, raceTuning.miracleBoostMax);
        }
    }
}
