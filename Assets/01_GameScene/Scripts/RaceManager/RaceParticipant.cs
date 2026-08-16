using System;
using UnityEngine;

/// <summary>
/// レース中における一等の動的な状態。
/// AnimalDataとは別に、進行度や現在速度など
/// レース中に変化する値を保持するためのクラス。
/// </summary>

[Serializable]
public class RaceParticipant
{
    public AnimalData animalData;
    public int laneIndex;   // 0から始まるレーン番号

    // レース開始時に確定する値(RaceSimulator.Initializeで設定される。)
    public float maxSpeed;
    public float accelerarion;
    public float initialStamina;
    public float earlyPhaseEnd;     // このprogressまでが加速フェーズ
    public float latePhaseStart;    // このprogress以降が終盤フェーズ

    // 進行状態
    public float progress;  // 0.0fから1.0fまでの範囲で、ゴールまでの進行度を表す
    public float currentSpeed;
    public float currentStamina;
    public bool isFinished;
    public int finishRank = -1; // ゴールした順位。ゴールしていない場合は-1

    //ラストスパート関連
    public bool hasRolledLatePhase; // 終盤突入時のスパート抽選を済ませたか
    public bool isSpurting;
    public float  spurtTimer;
    public float spurtBonusValue;

    // 運：アクシデント
    public bool isAccident;
    public float accidentTimer;
    public float accidentSlowFactor;

    // 運：ミラクル
    public bool isMiracle;
    public float miracleTimer;
    public float miracleBoost;
}
