using UnityEngine;

/// <summary>
/// Grants block to the card's caster.
/// </summary>
[CreateAssetMenu(fileName = "BlockEffect", menuName = "Deckbuilder/Effects/Block")]
public class BlockEffect : CardEffect
{
    [SerializeField] int amount = 8;

    public int Amount => amount;

public override void Resolve(EffectContext context)
    {
        if (context == null || context.Source == null)
            return;

        int block = amount;
        if (context.Card != null)
            block += context.Card.BlockBonus;
        context.Source.GainBlock(block);
        ICombatFeedback feedback = context.Source as ICombatFeedback;
        if (feedback != null)
            feedback.PlayBlockFeedback();
    }
}
