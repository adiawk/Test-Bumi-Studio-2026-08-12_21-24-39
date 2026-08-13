using UnityEngine;

/// <summary>
/// Deals damage to the card's combat target.
/// </summary>
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Deckbuilder/Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] int amount = 6;

    public int Amount => amount;

    public override void Resolve(EffectContext context)
    {
        if (context == null || context.Target == null)
            return;

        context.Target.TakeDamage(amount);
    }
}
