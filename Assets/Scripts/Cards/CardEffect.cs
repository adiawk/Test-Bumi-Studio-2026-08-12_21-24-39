using UnityEngine;

/// <summary>
/// One reusable gameplay effect. Cards compose these instead of per-card subclasses.
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    public abstract void Resolve(EffectContext context);
}
