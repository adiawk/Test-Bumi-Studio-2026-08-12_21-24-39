using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reward: pick one offer and return to the map.
/// Shop: click cards or heals as many times as you want, then Leave.
/// Upgrade: click run-deck damage/block cards for +2, then Leave.
/// </summary>
public class RewardUI : MonoBehaviour
{
    [SerializeField] Transform rewardRoot;
    [SerializeField] Transform offerGrid;
    [SerializeField] UICard cardPrefab;
    [SerializeField] Button continueButton;
    readonly RewardData[] offered = new RewardData[3];
    readonly List<Button> choiceButtons = new List<Button>();
    readonly List<Button> upgradeButtons = new List<Button>();
    bool isShop;
    bool isUpgrade;
    TextMeshProUGUI titleText;

    void Start()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        MapNodeType nodeType = run != null ? run.Stages.SelectedNodeType : MapNodeType.Reward;
        isShop = nodeType == MapNodeType.Shop;
        isUpgrade = nodeType == MapNodeType.Upgrade;
        titleText = FindTitleText();

        bool showLeave = isShop || isUpgrade;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(showLeave);
            continueButton.onClick.AddListener(OnContinueClicked);
            if (showLeave)
            {
                TextMeshProUGUI leaveLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                if (leaveLabel != null)
                    leaveLabel.text = "Leave";
            }
        }

        if (isUpgrade)
        {
            SetRoot(rewardRoot, false);
            SetRoot(offerGrid, true);
            OfferUpgrades();
            RefreshUpgradeAffordability();
            RefreshTitle();
            return;
        }

        if (isShop)
        {
            SetRoot(rewardRoot, false);
            SetRoot(offerGrid, true);
            OfferShop();
            SpawnChoices(offerGrid);
            RefreshShopAffordability();
        }
        else
        {
            SetRoot(rewardRoot, true);
            SetRoot(offerGrid, false);
            OfferRewards();
            SpawnChoices(rewardRoot);
        }

        RefreshTitle();
    }

    void OfferRewards()
    {
        List<RewardData> pool = CopyPool();
        if (TryOfferExact(pool))
            return;

        for (int i = 0; i < 3; i++)
        {
            offered[i] = pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
            if (offered[i] != null && pool.Count > 1)
                pool.Remove(offered[i]);
        }
    }

    void OfferShop()
    {
        List<RewardData> pool = CopyPool();
        if (TryOfferExact(pool))
            return;

        List<RewardData> cards = CopyPool(RewardKind.Card);
        List<RewardData> heals = CopyPool(RewardKind.HealRun);

        offered[0] = TakeRandom(cards);
        offered[1] = TakeRandom(cards);
        offered[2] = TakeRandom(heals);
        if (offered[2] == null)
            offered[2] = TakeRandom(cards);
    }

    bool TryOfferExact(List<RewardData> pool)
    {
        if (pool.Count == 0 || pool.Count > 3)
            return false;

        for (int i = 0; i < 3; i++)
            offered[i] = i < pool.Count ? pool[i] : null;
        return true;
    }

    void SpawnChoices(Transform parent)
    {
        choiceButtons.Clear();
        if (parent == null)
            return;

        List<Button> cards = CollectCardButtons(parent);
        if (cards.Count != 3)
        {
            ClearChildren(parent);
            cards.Clear();
            for (int i = 0; i < 3; i++)
            {
                Button card = SpawnCard(parent);
                if (card != null)
                    cards.Add(card);
            }
        }

        for (int i = 0; i < cards.Count; i++)
        {
            BindChoice(cards[i], i);
            choiceButtons.Add(cards[i]);
        }
    }

    static List<Button> CollectCardButtons(Transform parent)
    {
        var cards = new List<Button>();
        if (parent == null)
            return cards;

        for (int i = 0; i < parent.childCount; i++)
        {
            Button button = parent.GetChild(i).GetComponent<Button>();
            if (button != null)
                cards.Add(button);
        }

        return cards;
    }

    void OfferUpgrades()
    {
        upgradeButtons.Clear();
        ClearChildren(offerGrid);
        if (cardPrefab == null || offerGrid == null)
            return;

        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        IReadOnlyList<RuntimeCard> deck = run != null ? run.RunDeck : null;
        if (deck == null)
            return;

        for (int i = 0; i < deck.Count; i++)
        {
            RuntimeCard card = deck[i];
            if (card == null || !card.CanUpgrade)
                continue;

            Button clone = SpawnCard(offerGrid);
            if (clone == null)
                continue;

            BindUpgradeCard(clone, card);
            upgradeButtons.Add(clone);
        }
    }

    Button SpawnCard(Transform parent)
    {
        if (cardPrefab == null || parent == null)
            return null;

        UICard view = Instantiate(cardPrefab, parent);
        view.gameObject.SetActive(true);
        return view.Button;
    }

    void BindUpgradeCard(Button button, RuntimeCard card)
    {
        RefreshUpgradeLabel(button, card);
        button.onClick.RemoveAllListeners();
        RuntimeCard captured = card;
        Button capturedButton = button;
        button.onClick.AddListener(() =>
        {
            if (GameManager.Instance == null)
                return;
            if (!GameManager.Instance.NotifyUpgradePurchased(captured))
                return;

            RefreshUpgradeLabel(capturedButton, captured);
            RefreshUpgradeAffordability();
            RefreshTitle();
        });
    }

    static void RefreshUpgradeLabel(Button button, RuntimeCard card)
    {
        if (button == null || card == null)
            return;

        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        int cost = run != null ? run.UpgradeCoinCost : 0;
        ApplyCardLabel(button, card.Name, card.Cost.ToString(), card.Description, cost + " Coins");
    }

    void RefreshUpgradeAffordability()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        int coins = run != null ? run.Coins : 0;
        int cost = run != null ? run.UpgradeCoinCost : 0;
        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            if (upgradeButtons[i] != null)
                upgradeButtons[i].interactable = coins >= cost;
        }
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
        IReadOnlyList<RewardData> source = ResolvePool(run);
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

    static IReadOnlyList<RewardData> ResolvePool(RunManager run)
    {
        if (run == null)
            return null;

        IReadOnlyList<RewardData> nodePool = run.Stages.SelectedRewardPool;
        if (nodePool != null && nodePool.Count > 0)
            return nodePool;

        return run.RewardPool;
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

        RewardData reward = offered[index];
        bool hasReward = reward != null;
        button.interactable = hasReward;
        if (hasReward)
            ApplyOfferLabel(button, reward, isShop);

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
            if (!GameManager.Instance.NotifyShopPurchase(offered[index]))
                return;

            RefreshShopAffordability();
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

        if (isUpgrade)
        {
            titleText.text = "Upgrade";
            return;
        }

        if (!isShop)
        {
            titleText.text = "Choose a Card";
            return;
        }

        titleText.text = "Shop";
    }

    void RefreshShopAffordability()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
            RefreshChoiceAffordability(choiceButtons[i], i);
    }

    void RefreshChoiceAffordability(Button button, int index)
    {
        if (button == null)
            return;

        RewardData reward = index >= 0 && index < offered.Length ? offered[index] : null;
        if (reward == null)
        {
            button.interactable = false;
            return;
        }

        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        int coins = run != null ? run.Coins : 0;
        button.interactable = coins >= reward.CoinCost;
    }

    static void ApplyOfferLabel(Button button, RewardData reward, bool shop)
    {
        if (reward == null)
            return;

        string energy = reward.Kind == RewardKind.Card && reward.Card != null
            ? reward.Card.Cost.ToString()
            : string.Empty;
        string extra = shop ? reward.CoinCost + " Coins" : null;
        ApplyCardLabel(button, reward.DisplayName, energy, reward.Description, extra);
    }

    static void ApplyCardLabel(Button button, string displayName, string energy, string description, string extra)
    {
        if (button == null)
            return;

        UICard view = button.GetComponent<UICard>();
        if (view != null)
        {
            view.BindOffer(displayName, description, energy, extra);
            return;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
            return;

        string text = displayName ?? string.Empty;
        if (!string.IsNullOrEmpty(energy))
            text += "\n" + energy + " energy";
        if (!string.IsNullOrEmpty(description))
            text += "\n" + description;
        if (!string.IsNullOrEmpty(extra))
            text += "\n" + extra;
        label.text = text;
    }

    static void SetRoot(Transform root, bool active)
    {
        if (root != null)
            root.gameObject.SetActive(active);
    }

    static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
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

        if (isShop || isUpgrade)
        {
            GameManager.Instance.CompleteVisit();
            return;
        }

        GameManager.Instance.NotifyRewardChosen(null);
    }
}
