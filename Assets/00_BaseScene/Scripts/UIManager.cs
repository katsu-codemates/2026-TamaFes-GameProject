using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private CameraFocus cameraFocus;

    [SerializeField] private GameObject animalInfoParent;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI luckText;

    private bool isShowing = false;

    void Awake()
    {
        Instance = this;
    }

    public void ShowAnimalInfo(AnimalData data)
    {
        if (isShowing == true) return;

        isShowing = true;
        animalInfoParent.SetActive(true);
        nameText.text = "name:" + data.animalName;
        speedText.text = "speed:" + data.speed.ToString();
        powerText.text = "power:" + data.power.ToString();
        luckText.text = "luck:" + data.luck.ToString();
    }

    public void HideAnimalInfo()
    {
        if (isShowing == false) return;

        animalInfoParent.SetActive(false);
        isShowing = false;
    }

}
