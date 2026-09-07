using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject animalInfoParent;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI widomText;
    [SerializeField] private TextMeshProUGUI luckText;
    [SerializeField] private TextMeshProUGUI staminaText;

    void Awake()
    {
        HideAnimalInfo();
    }

    public void ShowAnimalInfo(AnimalData data)
    {
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
        animalInfoParent.SetActive(false);
    }

}
