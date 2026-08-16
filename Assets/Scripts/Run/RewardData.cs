using UnityEngine;

/// <summary>
/// One selectable reward option: card, heal, or next-combat buff/debuff.
/// </summary>
[CreateAssetMenu(fileName = "RewardData", menuName = "Deckbuilder/Rewards/Reward Data")]
public class RewardData : ScriptableObject
{
    [SerializeField] RewardKind kind = RewardKind.Card;
    [SerializeField] string displayName = "Reward";
    [SerializeField, TextArea] string description;
    [SerializeField] CardData card;
    [SerializeField] int amount = 1;
    [SerializeField] StatusId status = StatusId.Strength;
    [SerializeField] int coinCost;

    public RewardKind Kind => kind;
    public CardData Card => card;
    public int Amount => amount;
    public StatusId Status => status;
    public int CoinCost => Mathf.Max(0, coinCost);

    public string DisplayName
    {
        get
        {
            if (kind == RewardKind.Card && card != null && !string.IsNullOrEmpty(card.CardName))
                return card.CardName;
            return displayName;
        }
    }

    public string Description
    {
        get
        {
            if (!string.IsNullOrEmpty(description))
                return description;
            if (kind == RewardKind.Card && card != null)
                return card.Description;
            return string.Empty;
        }
    }

    public string OfferLabel
    {
        get
        {
            if (kind == RewardKind.Card && card != null)
                return card.CardName + "\n" + card.Cost + " energy\n" + card.Description;
            return DisplayName + "\n" + Description;
        }
    }

    public string ShopOfferLabel
    {
        get { return OfferLabel + "\n" + CoinCost + " Coins"; }
    }
}
