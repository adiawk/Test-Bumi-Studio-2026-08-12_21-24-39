using UnityEngine;

/// <summary>
/// Shared HP, block, and statuses. Player and Enemy compose this instead of duplicating rules.
/// </summary>
public class Combatant : IEffectTarget
{
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public int Block { get; private set; }
    public bool IsAlive => Hp > 0;
    public StatusBag Statuses { get; } = new StatusBag();

    public void Initialize(int maxHp)
    {
        MaxHp = Mathf.Max(1, maxHp);
        Hp = MaxHp;
        Block = 0;
        Statuses.Clear();
    }

    public void SetHp(int hp)
    {
        Hp = Mathf.Clamp(hp, 0, MaxHp);
    }

    public void ClearBlock()
    {
        Block = 0;
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount);
    }

    public DamageResult ApplyDamage(int amount)
    {
        if (amount <= 0)
            return new DamageResult(0, 0);

        int blocked = Mathf.Min(Block, amount);
        Block -= blocked;
        int remaining = amount - blocked;
        if (remaining > 0)
            Hp = Mathf.Max(0, Hp - remaining);

        return new DamageResult(blocked, remaining);
    }

    public void GainBlock(int amount)
    {
        if (amount > 0)
            Block += amount;
    }

    public void Heal(int amount)
    {
        if (amount > 0)
            Hp = Mathf.Min(MaxHp, Hp + amount);
    }

    public void ApplyStatus(StatusId id, int stacks)
    {
        if (id == StatusId.StartBlock)
        {
            GainBlock(stacks);
            return;
        }

        Statuses.Apply(id, stacks);
    }
}
