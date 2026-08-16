using System.Collections.Generic;

/// <summary>
/// Tracks the player's place on a hand-authored chapter map.
/// </summary>
public class StageManager
{
    readonly List<string> clearedNodeIds = new List<string>();
    readonly List<RewardData> selectedRewardPool = new List<RewardData>();

    public string LastCompletedNodeId { get; private set; }
    public string SelectedNodeId { get; private set; }
    public MapNodeType SelectedNodeType { get; private set; }
    public EncounterData SelectedEncounter { get; private set; }
    public IReadOnlyList<RewardData> SelectedRewardPool => selectedRewardPool;
    public IReadOnlyList<string> ClearedNodeIds => clearedNodeIds;

    public void ResetToStart()
    {
        clearedNodeIds.Clear();
        LastCompletedNodeId = null;
        ClearSelection();
    }

    public void SelectNode(string nodeId, MapNodeType nodeType, EncounterData encounter, IReadOnlyList<RewardData> rewardPool)
    {
        SelectedNodeId = nodeId;
        SelectedNodeType = nodeType;
        SelectedEncounter = encounter;
        selectedRewardPool.Clear();
        if (rewardPool == null)
            return;

        for (int i = 0; i < rewardPool.Count; i++)
        {
            if (rewardPool[i] != null)
                selectedRewardPool.Add(rewardPool[i]);
        }
    }

    public void CompleteSelectedNode()
    {
        if (string.IsNullOrEmpty(SelectedNodeId))
            return;

        if (!clearedNodeIds.Contains(SelectedNodeId))
            clearedNodeIds.Add(SelectedNodeId);

        LastCompletedNodeId = SelectedNodeId;
        ClearSelection();
    }

    public bool IsCleared(string nodeId)
    {
        return !string.IsNullOrEmpty(nodeId) && clearedNodeIds.Contains(nodeId);
    }

    void ClearSelection()
    {
        SelectedNodeId = null;
        SelectedNodeType = MapNodeType.Combat;
        SelectedEncounter = null;
        selectedRewardPool.Clear();
    }
}
