using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// レースの先頭を追従するカメラの制御クラス。
/// </summary>

//  変更予定内容
//      ・水平移動のみでなく、カメラ設置ポイントからのフォーカスを交える
//      ・運イベント発生時、該当者にカメラが寄るようにする
//      ・ゴール時はゴールラインを垂直に映すようにする
//      ・手振れを追加する
//      ・フォーカス対象を場合によって動的に変更できるようにする   
//            
//  方針
//      CinemachineBrainによる仮想カメラを使用（各場所または走者に対し配置し、slerpによる滑らかなカメラ移動を行う）   
//      手振れはパーリンノイズを使用
//      フォーカス対象は運やスタミナ切れ等のイベントによって変更する
//

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
