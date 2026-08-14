using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stackable combat statuses living on a combatant for one fight.
/// </summary>
public class StatusBag
{
    readonly Dictionary<StatusId, int> stacks = new Dictionary<StatusId, int>();

    public void Clear()
    {
        stacks.Clear();
    }

    public void Apply(StatusId id, int amount)
    {
        if (amount == 0)
            return;

        int current = GetStacks(id);
        int next = current + amount;
        if (next <= 0)
            stacks.Remove(id);
        else
            stacks[id] = next;
    }

    public int GetStacks(StatusId id)
    {
        return stacks.TryGetValue(id, out int value) ? value : 0;
    }

    public int ModifyOutgoingAttack(int amount)
    {
        if (amount <= 0)
            return 0;

        int result = amount + GetStacks(StatusId.Strength);
        if (GetStacks(StatusId.Weak) > 0)
            result = Mathf.FloorToInt(result * 0.75f);
        return Mathf.Max(0, result);
    }

    public int ModifyIncomingAttack(int amount)
    {
        if (amount <= 0)
            return 0;

        if (GetStacks(StatusId.Vulnerable) > 0)
            return Mathf.FloorToInt(amount * 1.5f);
        return amount;
    }

    public string BuildLabel()
    {
        if (stacks.Count == 0)
            return string.Empty;

        var parts = new List<string>(stacks.Count);
        foreach (KeyValuePair<StatusId, int> pair in stacks)
        {
            if (pair.Value <= 0)
                continue;
            parts.Add(ShortName(pair.Key) + pair.Value);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }

    static string ShortName(StatusId id)
    {
        switch (id)
        {
            case StatusId.Strength: return "Str";
            case StatusId.Weak: return "Wk";
            case StatusId.Vulnerable: return "Vuln";
            case StatusId.StartBlock: return "Blk";
            case StatusId.ExtraEnergy: return "En";
            default: return id.ToString();
        }
    }
}
