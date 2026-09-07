using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 現在選択されている動物を管理する。
/// 動物の選択・選択解除に伴う処理をまとめて行う。
/// </summary>
public class AnimalSelectionManager : MonoBehaviour
{
    [Header("各種管理クラス")]
    [SerializeField] private CameraFocus cameraFocus;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private IllustrationManager illustrationManager;

    private AnimalDataHolder selectedAnimal;

    public bool IsSelected => selectedAnimal != null;
    public AnimalDataHolder SelectedAnimal => selectedAnimal;

    /// <summary>
    /// 動物を選択する。
    /// </summary>
    public void SelectAnimal(AnimalDataHolder animal)
    {
        if (selectedAnimal == animal || animal.Data == null)
        {
            return;
        }

        if (selectedAnimal != null)
        {
            Unselect();
        }

        selectedAnimal = animal;

        // 詳細情報表示処理開始
        cameraFocus.StartFocus(animal.transform);
        illustrationManager.MakeTransparentAllImages(exceptionImageId: animal.Data.createdAt);
        uiManager.ShowAnimalInfo(animal.Data);
    }

    /// <summary>
    /// 現在選択中の動物を解除する。
    /// </summary>
    public void Unselect()
    {
        if (selectedAnimal == null) return;

        uiManager.HideAnimalInfo();
        illustrationManager.ResetTransparencyAllImages();
        cameraFocus.Unfocus();

        selectedAnimal = null;
    }

    private void Update()
    {
        if (!IsSelected) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Unselect();
        }
    }
}
