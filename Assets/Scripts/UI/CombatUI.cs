using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combat HUD. Shows live stats, piles, and hand.
/// </summary>
public class CombatUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerHpText;
    [SerializeField] TextMeshProUGUI playerBlockText;
    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI enemyHpText;
    [SerializeField] TextMeshProUGUI enemyIntentText;
    [SerializeField] TextMeshProUGUI drawPileText;
    [SerializeField] TextMeshProUGUI discardPileText;
    [SerializeField] Transform handArea;
    [SerializeField] Button endTurnButton;
    [SerializeField] TurnManager turnManager;
    [SerializeField] Player player;

    [SerializeField] CombatManager combatManager;
    int spawnedHandCount = -1;
    RuntimeCard spawnedFirstCard;

    void Awake()
    {
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }

    void OnDestroy()
    {
        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
    }

    void LateUpdate()
    {
        Refresh();
        RefreshHand();
    }

void Refresh()
    {
        Player combatPlayer = player;
        if (combatPlayer == null && combatManager != null)
            combatPlayer = combatManager.Player;

        DeckManager deck = combatPlayer != null ? combatPlayer.Deck : null;
        if (deck == null && combatManager != null)
            deck = combatManager.Deck;

        if (energyText != null && combatPlayer != null)
        {
            string energy = "Energy\n" + combatPlayer.Energy + "/" + combatPlayer.MaxEnergy;
            if (combatManager != null && combatManager.PendingCard != null)
                energy += "\nPick target";
            energyText.text = energy;
        }

        if (deck != null)
        {
            if (drawPileText != null)
                drawPileText.text = "Deck\n" + deck.DrawPile.Count;
            if (discardPileText != null)
            {
                string top = deck.DiscardPile.Count > 0 ? deck.DiscardPile[deck.DiscardPile.Count - 1].Name : "Empty";
                discardPileText.text = "Discard\n" + deck.DiscardPile.Count + "\n" + top;
            }
        }
    }

    void RefreshHand()
    {
        Player combatPlayer = player;
        if (combatPlayer == null && combatManager != null)
            combatPlayer = combatManager.Player;
        if (handArea == null || combatPlayer == null || combatPlayer.Deck == null)
            return;

        var hand = combatPlayer.Deck.Hand;
        RuntimeCard first = hand.Count > 0 ? hand[0] : null;
        if (spawnedHandCount == hand.Count && spawnedFirstCard == first)
            return;

        for (int i = handArea.childCount - 1; i >= 0; i--)
            Destroy(handArea.GetChild(i).gameObject);

        for (int i = 0; i < hand.Count; i++)
            CardView.Create(handArea, hand[i], combatManager);

        spawnedHandCount = hand.Count;
        spawnedFirstCard = first;
    }

    void OnEndTurnClicked()
    {
        if (combatManager != null)
            combatManager.RequestEndTurn();
    }
}

