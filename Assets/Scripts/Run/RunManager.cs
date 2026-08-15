using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns one dungeon run: map progress, HP, and the run deck.
/// Does not own combat resolution or card data.
/// </summary>
public class RunManager : MonoBehaviour
{
    readonly StageManager stageManager = new StageManager();
    readonly List<PendingCombatModifier> pendingModifiers = new List<PendingCombatModifier>();

    [SerializeField] List<CardData> starterDeck = new List<CardData>();
    [SerializeField] List<RewardData> rewardPool = new List<RewardData>();
    [SerializeField] int startingMaxHp = 50;
    [SerializeField] EncounterData stage1Encounter;
    [SerializeField] EncounterData stage2Encounter;
    [SerializeField] EncounterData bossEncounter;

    readonly List<RuntimeCard> runDeck = new List<RuntimeCard>();

    public IReadOnlyList<RuntimeCard> RunDeck => runDeck;
    public IReadOnlyList<RewardData> RewardPool => rewardPool;
    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public bool LastRunWon { get; private set; }
    public StageManager Stages => stageManager;
    public bool IsRunActive { get; private set; }

    public EncounterData GetCurrentEncounter()
    {
        if (stageManager.SelectedEncounter != null)
            return stageManager.SelectedEncounter;

        return GetEncounterForNodeType(stageManager.SelectedNodeType);
    }

    public EncounterData GetEncounterForNodeType(MapNodeType nodeType)
    {
        if (nodeType == MapNodeType.Boss)
            return bossEncounter;
        if (nodeType == MapNodeType.Elite)
            return stage2Encounter;
        return stage1Encounter;
    }

    public void StartNewRun()
    {
        IsRunActive = true;
        LastRunWon = false;
        MaxHp = startingMaxHp;
        CurrentHp = startingMaxHp;
        pendingModifiers.Clear();
        runDeck.Clear();
        if (starterDeck != null)
        {
            for (int i = 0; i < starterDeck.Count; i++)
            {
                if (starterDeck[i] != null)
                    runDeck.Add(new RuntimeCard(starterDeck[i]));
            }
        }
        stageManager.ResetToStart();
    }

    public void EndRun()
    {
        IsRunActive = false;
        pendingModifiers.Clear();
        Debug.Log("[RunManager] Run ended.");
    }

    public void SetHp(int hp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
    }

    public void HealRun(int amount)
    {
        if (amount > 0)
            SetHp(CurrentHp + amount);
    }

    public void AddCardToRunDeck(CardData card)
    {
        if (card != null)
            runDeck.Add(new RuntimeCard(card));
    }

    public void QueueNextCombatModifier(StatusId status, int stacks, bool applyToPlayer)
    {
        if (stacks == 0)
            return;
        pendingModifiers.Add(new PendingCombatModifier(status, stacks, applyToPlayer));
    }

    public void ApplyPendingCombatModifiers(Player player, IReadOnlyList<Enemy> enemies)
    {
        if (pendingModifiers.Count == 0)
            return;

        for (int i = 0; i < pendingModifiers.Count; i++)
        {
            PendingCombatModifier mod = pendingModifiers[i];
            if (mod.Stacks == 0)
                continue;

            if (mod.ApplyToPlayer)
            {
                if (player != null)
                    player.ApplyStatus(mod.Status, mod.Stacks);
                continue;
            }

            if (enemies == null)
                continue;

            for (int e = 0; e < enemies.Count; e++)
            {
                Enemy enemy = enemies[e];
                if (enemy != null && enemy.IsAlive)
                    enemy.ApplyStatus(mod.Status, mod.Stacks);
            }
        }

        pendingModifiers.Clear();
    }

    public bool TryApplyReward(RewardData reward)
    {
        if (reward == null)
            return false;

        switch (reward.Kind)
        {
            case RewardKind.Card:
                AddCardToRunDeck(reward.Card);
                return reward.Card != null;
            case RewardKind.HealRun:
                HealRun(reward.Amount);
                return true;
            case RewardKind.NextCombatPlayerBuff:
                QueueNextCombatModifier(reward.Status, reward.Amount, true);
                return true;
            case RewardKind.NextCombatEnemyDebuff:
                QueueNextCombatModifier(reward.Status, reward.Amount, false);
                return true;
            default:
                return false;
        }
    }

    public void SetLastRunWon(bool won)
    {
        LastRunWon = won;
    }
}
