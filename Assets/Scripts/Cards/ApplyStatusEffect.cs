using UnityEngine;

/// <summary>
/// Applies a status to the card's combat target.
/// </summary>
[CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Deckbuilder/Effects/Apply Status")]
public class ApplyStatusEffect : CardEffect
{
    [SerializeField] StatusId status = StatusId.VulnerableNext;
    [SerializeField] int stacks = 1;

    public StatusId Status => status;
    public int Stacks => stacks;

    public override void Resolve(EffectContext context)
    {
        if (context == null || context.Target == null || stacks == 0)
            return;

        context.Target.ApplyStatus(status, stacks);
    }
}
