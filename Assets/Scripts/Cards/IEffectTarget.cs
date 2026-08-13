/// <summary>
/// Combat actor that card effects can change. Player and Enemy implement this later.
/// </summary>
public interface IEffectTarget
{
    void TakeDamage(int amount);
    void GainBlock(int amount);
    void Heal(int amount);
}
