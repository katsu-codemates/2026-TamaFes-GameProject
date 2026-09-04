using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// レース狩猟時に呼び出され、最終順位をならべて表示させるクラス。
/// </summary>
public class RaceResultScreen : MonoBehaviour
{
    [Header("この画面全体のルート")]
    [SerializeField] private GameObject screenRoot;

    [Header("結果行のプレハブ")]
    [SerializeField] private GameObject resultRowPrefab;

    [Header("結果行を並べる親オブジェクト")]
    [SerializeField] private Transform rowContainer;

    private void Awake()
    {
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 最終着順を受け取って表示する。
    /// </summary>
    public void Show(List<RaceParticipant> finishedOrder)
    {
        ClearRows();
        
        if (screenRoot != null)
        {
            screenRoot.SetActive(true);
        }

        foreach (var participant in finishedOrder)
        {
            GameObject row = Instantiate(resultRowPrefab, rowContainer);
            RaceResultRowView view = row.GetComponent<RaceResultRowView>();
            view.Setup(participant);
        }

    }

    private void ClearRows()
    {
        for (int i = rowContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(rowContainer.GetChild(i).gameObject);
        }
    }

}
