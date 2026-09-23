using System;
using UnityEngine;

/// <summary>
/// レース中に発生するイベントを他へ伝えるイベントバス。
/// </summary>
public static class RaceEventBus
{
    public static event Action<RaceParticipant> OnSpurtStarted;
    public static event Action<RaceParticipant> OnAccidentStarted;
    public static event Action<RaceParticipant> OnMiracleStarted;
    public static event Action<RaceParticipant> OnFinished;
    public static event Action<RaceParticipant> OnStaminaDepleted;
    // 抜いた走者, 抜かれた走者, 抜いた走者の新しい順位(1始まり)
    public static event Action<RaceParticipant, RaceParticipant, int> OnOvertake;

    // 呼ぶとイベントを発動
    public static void RaiseSpurtStarted(RaceParticipant p) => OnSpurtStarted?.Invoke(p);
    public static void RaiseAccidentStarted(RaceParticipant p) => OnAccidentStarted?.Invoke(p);
    public static void RaiseMiracleStarted(RaceParticipant p) => OnMiracleStarted?.Invoke(p);
    public static void RaiseFinished(RaceParticipant p) => OnFinished?.Invoke(p);
    public static void RaiseStaminaDepleted(RaceParticipant p) => OnStaminaDepleted?.Invoke(p);
    public static void RaiseOvertake(RaceParticipant passer, RaceParticipant passed, int newRank)
        => OnOvertake?.Invoke(passer, passed, newRank);
}
