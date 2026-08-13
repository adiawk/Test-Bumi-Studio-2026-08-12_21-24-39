using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static card definition. New cards are data assets, not new gameplay classes.
/// </summary>
[CreateAssetMenu(fileName = "CardData", menuName = "Deckbuilder/Cards/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] string cardName = "New Card";
    [SerializeField, TextArea] string description;
    [SerializeField] int cost;
    [SerializeField] CardTargetType targetType = CardTargetType.Enemy;
    [SerializeField] List<CardEffect> effects = new List<CardEffect>();

    public string CardName => cardName;
    public string Description => description;
    public int Cost => cost;
    public CardTargetType TargetType => targetType;
    public IReadOnlyList<CardEffect> Effects => effects;

    public void Resolve(EffectContext context)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null)
                effects[i].Resolve(context);
        }
    }
}
