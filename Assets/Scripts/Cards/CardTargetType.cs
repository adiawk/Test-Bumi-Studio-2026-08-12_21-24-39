/// <summary>
/// Who a card is aimed at when played. Individual effects still choose
/// source vs target from EffectContext (damage hits the target, block hits the caster).
/// </summary>
public enum CardTargetType
{
    None,
    Self,
    Enemy
}
