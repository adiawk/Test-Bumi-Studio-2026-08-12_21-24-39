using UnityEngine;

/// <summary>
/// Restores HP to the card's caster.
/// </summary>
[CreateAssetMenu(fileName = "HealEffect", menuName = "Deckbuilder/Effects/Heal")]
public class HealEffect : CardEffect
{
    [SerializeField] int amount = 5;

    public int Amount => amount;

public override void Resolve(EffectContext context)
    {
        if (context == null || context.Source == null)
            return;

        context.Source.Heal(amount);
        ICombatFeedback feedback = context.Source as ICombatFeedback;
        if (feedback != null)
            feedback.PlayHealFeedback();
    }
}
