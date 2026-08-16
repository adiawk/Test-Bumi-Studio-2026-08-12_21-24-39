/// <summary>
/// How an incoming hit split between block and HP.
/// </summary>
public readonly struct DamageResult
{
    public readonly int Blocked;
    public readonly int HpDamage;

    public DamageResult(int blocked, int hpDamage)
    {
        Blocked = blocked;
        HpDamage = hpDamage;
    }
}
