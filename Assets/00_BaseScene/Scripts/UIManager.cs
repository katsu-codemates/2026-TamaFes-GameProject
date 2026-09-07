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
    [SerializeField] private TextMeshProUGUI widomText;
    [SerializeField] private TextMeshProUGUI luckText;
    [SerializeField] private TextMeshProUGUI staminaText;

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
        nameText.text = data.animalName;
        speedText.text = "はやさ:" + data.speed.ToString();
        powerText.text = "ちから:" + data.power.ToString();
        widomText.text = "かしこさ:" + data.wisdom.ToString();
        luckText.text = "うんのよさ:" + data.luck.ToString();
        staminaText.text = "スタミナ:" + data.stamina.ToString();
    }

    public void HideAnimalInfo()
    {
        if (isShowing == false) return;

        animalInfoParent.SetActive(false);
        isShowing = false;
    }

}
