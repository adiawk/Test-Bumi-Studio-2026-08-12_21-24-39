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

    public void ActivateDelayedStatuses()
    {
        int pending = GetStacks(StatusId.VulnerableNext);
        if (pending <= 0)
            return;

        Apply(StatusId.VulnerableNext, -pending);
        int current = GetStacks(StatusId.Vulnerable);
        if (current > 0)
            Apply(StatusId.Vulnerable, -current);
        Apply(StatusId.Vulnerable, 1);
    }

    public void ExpireTimedStatuses()
    {
        int vulnerable = GetStacks(StatusId.Vulnerable);
        if (vulnerable > 0)
            Apply(StatusId.Vulnerable, -vulnerable);

        int stun = GetStacks(StatusId.Stun);
        if (stun > 0)
            Apply(StatusId.Stun, -1);
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

            string label = StatusLabel(pair.Key, pair.Value);
            if (!string.IsNullOrEmpty(label))
                parts.Add(label);
        }

        return parts.Count == 0 ? string.Empty : string.Join(" ", parts);
    }

    static string StatusLabel(StatusId id, int stacks)
    {
        switch (id)
        {
            case StatusId.Vulnerable: return "Vuln";
            case StatusId.VulnerableNext: return "Vuln next";
            case StatusId.Stun: return "Stn";
            default: return ShortName(id) + stacks;
        }
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
            case StatusId.Stun: return "Stn";
            case StatusId.VulnerableNext: return "Vuln next";
            default: return id.ToString();
        }
    }
}
