using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Map-only cheat buttons for restoring HP and adding coins.
/// </summary>
public class MapDebugUI : MonoBehaviour
{
    [SerializeField] Button healButton;
    [SerializeField] Button addCoinsButton;
    [SerializeField] int coinAmount = 50;

    void Awake()
    {
        if (healButton != null)
            healButton.onClick.AddListener(OnHealClicked);
        if (addCoinsButton != null)
            addCoinsButton.onClick.AddListener(OnAddCoinsClicked);
    }

    void OnDestroy()
    {
        if (healButton != null)
            healButton.onClick.RemoveListener(OnHealClicked);
        if (addCoinsButton != null)
            addCoinsButton.onClick.RemoveListener(OnAddCoinsClicked);
    }

    void OnHealClicked()
    {
        RunManager run = GetRun();
        if (run == null)
            return;

        run.HealRun(run.MaxHp);
        Debug.Log("[MapDebugUI] Healed to " + run.CurrentHp + "/" + run.MaxHp);
    }

    void OnAddCoinsClicked()
    {
        RunManager run = GetRun();
        if (run == null)
            return;

        run.AddCoins(coinAmount);
        Debug.Log("[MapDebugUI] Added " + coinAmount + " coins. Total: " + run.Coins);
    }

    static RunManager GetRun()
    {
        if (GameManager.Instance == null || GameManager.Instance.Run == null)
        {
            Debug.LogError("[MapDebugUI] GameManager or Run is missing.");
            return null;
        }

        return GameManager.Instance.Run;
    }
}
