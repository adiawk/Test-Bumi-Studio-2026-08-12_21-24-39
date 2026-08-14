/// <summary>
/// Status applied when the next combat starts, then consumed.
/// </summary>
public struct PendingCombatModifier
{
    public StatusId Status;
    public int Stacks;
    public bool ApplyToPlayer;

    public PendingCombatModifier(StatusId status, int stacks, bool applyToPlayer)
    {
        Status = status;
        Stacks = stacks;
        ApplyToPlayer = applyToPlayer;
    }
}
