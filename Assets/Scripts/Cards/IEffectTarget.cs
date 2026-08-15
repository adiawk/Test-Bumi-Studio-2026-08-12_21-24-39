/// <summary>
/// Combat actor that card effects can change.
/// </summary>
public interface IEffectTarget
{
    void TakeDamage(int amount);
    void GainBlock(int amount);
    void Heal(int amount);
    void ApplyStatus(StatusId id, int stacks);
}
