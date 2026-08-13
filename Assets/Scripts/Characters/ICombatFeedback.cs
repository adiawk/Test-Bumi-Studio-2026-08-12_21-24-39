/// <summary>
/// Optional presentation hooks for combat actors. Gameplay effects stay on IEffectTarget.
/// </summary>
public interface ICombatFeedback
{
    void PlayAttackFeedback();
    void PlayHitFeedback();
    void PlayBlockFeedback();
    void PlayHealFeedback();
}
