using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// レースの先頭を追従するカメラの制御クラス。
/// </summary>

public class RaceCameraController : MonoBehaviour
{
    [Header("斜め上から見下ろす角度。固定")]
    [SerializeField] private Vector3 fixedAngles = new Vector3(35f, -135f, 0f);

    [Header("先頭からどれだけ後方に位置するか")]
    [SerializeField] private float distanceBehind = 10f;

    [Header("カメラの高さ")]
    [SerializeField] private float height = 5f;

    [Header("カメラの追従の滑らかさ")]
    [SerializeField] private float smoothTime = 0.3f;

    private Vector3 velocity;
    private List<RaceParticipant> participants;

    private void Awake()
    {
        transform.eulerAngles = fixedAngles;
    }

    public void SetParticipants(List<RaceParticipant> list)
    {
        participants = list;
    }

    private void LateUpdate()
    {
        if (participants == null || participants.Count == 0) return;

        // 先頭の参加者を見つける
        RaceParticipant leader = GetLeader();
        Vector3 leaderPosition = RaceTrack.GetWorldPosition(leader.progress, leader.laneIndex, participants.Count);

        Vector3 targetPosition = leaderPosition
            - transform.forward * distanceBehind
            + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    private RaceParticipant GetLeader()
    {
        RaceParticipant leader = participants[0];
        foreach (var participant in participants)
        {
            if (participant.progress > leader.progress)
            {
                leader = participant;
            }
        }
        return leader;
    }
}
