using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns one dungeon run: map progress, HP, and the run deck.
/// Does not own combat resolution or card data.
/// </summary>
public class RunManager : MonoBehaviour
{
    readonly StageManager stageManager = new StageManager();

    [SerializeField] List<CardData> starterDeck = new List<CardData>();
    [SerializeField] List<CardData> rewardPool = new List<CardData>();
    [SerializeField] int startingMaxHp = 50;
    [SerializeField] EncounterData stage1Encounter;
    [SerializeField] EncounterData stage2Encounter;
    [SerializeField] EncounterData bossEncounter;

    readonly List<CardData> runDeck = new List<CardData>();

    public IReadOnlyList<CardData> RunDeck => runDeck;
    public IReadOnlyList<CardData> RewardPool => rewardPool;
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
        runDeck.Clear();
        if (starterDeck != null)
        {
            for (int i = 0; i < starterDeck.Count; i++)
            {
                if (starterDeck[i] != null)
                    runDeck.Add(starterDeck[i]);
            }
        }
        stageManager.ResetToStart();
    }

    public void EndRun()
    {
        IsRunActive = false;
        Debug.Log("[RunManager] Run ended.");
    }

    public void SetHp(int hp)
    {
        CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
    }

    public void AddCardToRunDeck(CardData card)
    {
        if (card != null)
            runDeck.Add(card);
    }

    public void SetLastRunWon(bool won)
    {
        LastRunWon = won;
    }
}
