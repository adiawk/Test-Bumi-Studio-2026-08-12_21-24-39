using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat piles only: draw, hand, discard.
/// Does not own card effects or run progression.
/// </summary>
public class DeckManager : MonoBehaviour, ICardDrawer
{
    [SerializeField] List<CardData> startingCards = new List<CardData>();

    readonly List<RuntimeCard> drawPile = new List<RuntimeCard>();
    readonly List<RuntimeCard> hand = new List<RuntimeCard>();
    readonly List<RuntimeCard> discardPile = new List<RuntimeCard>();

    public IReadOnlyList<RuntimeCard> DrawPile => drawPile;
    public IReadOnlyList<RuntimeCard> Hand => hand;
    public IReadOnlyList<RuntimeCard> DiscardPile => discardPile;

    public void SetupCombatDeck()
    {
        SetupCombatDeckFromData(startingCards);
    }

    public void SetupCombatDeck(IReadOnlyList<RuntimeCard> cards)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        if (cards == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                drawPile.Add(cards[i]);
        }

        Shuffle(drawPile);
    }

    public void SetupCombatDeckFromData(IReadOnlyList<CardData> cards)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();

        if (cards == null)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null)
                drawPile.Add(new RuntimeCard(cards[i]));
        }

        Shuffle(drawPile);
    }

    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
                RecycleDiscardIntoDrawPile();

            if (drawPile.Count == 0)
                break;

            int last = drawPile.Count - 1;
            hand.Add(drawPile[last]);
            drawPile.RemoveAt(last);
        }
    }

    public void AddCardToDeck(CardData data)
    {
        if (data == null)
            return;

        discardPile.Add(new RuntimeCard(data));
    }

    public bool RemoveCardFromHand(RuntimeCard card)
    {
        return card != null && hand.Remove(card);
    }

    public void Discard(RuntimeCard card)
    {
        if (card == null)
            return;

        hand.Remove(card);
        if (!discardPile.Contains(card))
            discardPile.Add(card);
    }

    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }

    public void Shuffle(List<RuntimeCard> pile)
    {
        if (pile == null || pile.Count <= 1)
            return;

        for (int i = pile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            RuntimeCard swap = pile[i];
            pile[i] = pile[j];
            pile[j] = swap;
        }
    }

    void RecycleDiscardIntoDrawPile()
    {
        if (discardPile.Count == 0)
            return;

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
    }
}
