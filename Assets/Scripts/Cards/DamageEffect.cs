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

        int damage = amount;
        if (context.Card != null)
            damage += context.Card.DamageBonus;
        Player sourcePlayer = context.Source as Player;
        if (sourcePlayer != null)
            damage = sourcePlayer.Statuses.ModifyOutgoingAttack(damage);

        Enemy targetEnemy = context.Target as Enemy;
        if (targetEnemy != null)
            damage = targetEnemy.Statuses.ModifyIncomingAttack(damage);
        else
        {
            Player targetPlayer = context.Target as Player;
            if (targetPlayer != null)
                damage = targetPlayer.Statuses.ModifyIncomingAttack(damage);
        }

        context.Target.TakeDamage(damage);

        ICombatFeedback sourceFx = context.Source as ICombatFeedback;
        if (sourceFx != null)
            sourceFx.PlayAttackFeedback();
    }
}
