using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combat victory overlay. Continue routes back to the map (or Result for boss).
/// </summary>
public class CombatWinUI : MonoBehaviour
{
    [SerializeField] GameObject panelRoot;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI rewardText;
    [SerializeField] TextMeshProUGUI coinsText;
    [SerializeField] Button continueButton;

    void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    public void Show(int reward, int totalCoins)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = "Victory";
        if (rewardText != null)
            rewardText.text = "+" + reward + " Coins";
        if (coinsText != null)
            coinsText.text = "Coins: " + totalCoins;
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    void OnContinueClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CombatWinUI] GameManager is missing.");
            return;
        }

        GameManager.Instance.NotifyCombatWon();
    }
}
