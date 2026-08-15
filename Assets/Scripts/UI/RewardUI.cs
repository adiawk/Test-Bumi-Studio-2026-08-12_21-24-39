using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reward: pick one offer and return to the map.
/// Shop: click cards or heals as many times as you want, then Leave.
/// </summary>
public class RewardUI : MonoBehaviour
{
    [SerializeField] Button cardChoice1Button;
    [SerializeField] Button cardChoice2Button;
    [SerializeField] Button cardChoice3Button;
    [SerializeField] Button continueButton;
    readonly RewardData[] offered = new RewardData[3];
    bool isShop;
    TextMeshProUGUI titleText;

    void Start()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        isShop = run != null && run.Stages.SelectedNodeType == MapNodeType.Shop;
        titleText = FindTitleText();

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(isShop);
            continueButton.onClick.AddListener(OnContinueClicked);
            if (isShop)
            {
                TextMeshProUGUI leaveLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (leaveLabel != null)
                    leaveLabel.text = "Leave";
            }
        }

        if (isShop)
            OfferShop();
        else
            OfferRewards();

        BindChoice(cardChoice1Button, 0);
        BindChoice(cardChoice2Button, 1);
        BindChoice(cardChoice3Button, 2);
        RefreshTitle();
    }

    void OfferRewards()
    {
        List<RewardData> pool = CopyPool();
        for (int i = 0; i < 3; i++)
        {
            offered[i] = pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
            if (offered[i] != null && pool.Count > 1)
                pool.Remove(offered[i]);
        }
    }

    void OfferShop()
    {
        List<RewardData> cards = CopyPool(RewardKind.Card);
        List<RewardData> heals = CopyPool(RewardKind.HealRun);

        offered[0] = TakeRandom(cards);
        offered[1] = TakeRandom(cards);
        offered[2] = TakeRandom(heals);
        if (offered[2] == null)
            offered[2] = TakeRandom(cards);
    }

    static List<RewardData> CopyPool()
    {
        return CopyMatching(null);
    }

    static List<RewardData> CopyPool(RewardKind kind)
    {
        return CopyMatching(kind);
    }

    static List<RewardData> CopyMatching(RewardKind? kind)
    {
        var pool = new List<RewardData>();
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        IReadOnlyList<RewardData> source = run != null ? run.RewardPool : null;
        if (source == null)
            return pool;

        for (int i = 0; i < source.Count; i++)
        {
            RewardData reward = source[i];
            if (reward == null)
                continue;
            if (kind.HasValue && reward.Kind != kind.Value)
                continue;
            pool.Add(reward);
        }

        return pool;
    }

    static RewardData TakeRandom(List<RewardData> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int index = Random.Range(0, pool.Count);
        RewardData picked = pool[index];
        pool.RemoveAt(index);
        return picked;
    }

    void BindChoice(Button button, int index)
    {
        if (button == null)
            return;

        bool hasReward = offered[index] != null;
        button.interactable = hasReward;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null && hasReward)
            label.text = offered[index].OfferLabel;

        int captured = index;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Choose(captured));
    }

    void Choose(int index)
    {
        if (offered[index] == null || GameManager.Instance == null)
            return;

        if (isShop)
        {
            GameManager.Instance.NotifyShopPurchase(offered[index]);
            RefreshTitle();
            return;
        }

        GameManager.Instance.NotifyRewardChosen(offered[index]);
    }

    void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    void RefreshTitle()
    {
        if (titleText == null)
            return;

        if (!isShop)
        {
            titleText.text = "Choose a Card";
            return;
        }

        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        if (run == null)
        {
            titleText.text = "Shop";
            return;
        }

        titleText.text = "Shop  HP " + run.CurrentHp + "/" + run.MaxHp;
    }

    TextMeshProUGUI FindTitleText()
    {
        Transform title = transform.Find("Title");
        if (title == null)
            return null;
        return title.GetComponent<TextMeshProUGUI>();
    }

    void OnContinueClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[RewardUI] GameManager is missing.");
            return;
        }

        if (isShop)
        {
            GameManager.Instance.NotifyShopClosed();
            return;
        }

        GameManager.Instance.NotifyRewardChosen(null);
    }
}
