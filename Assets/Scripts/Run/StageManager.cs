using System.Collections.Generic;

/// <summary>
/// Tracks the player's place on a hand-authored chapter map.
/// </summary>
public class StageManager
{
    readonly List<string> clearedNodeIds = new List<string>();

    public string LastCompletedNodeId { get; private set; }
    public string SelectedNodeId { get; private set; }
    public MapNodeType SelectedNodeType { get; private set; }
    public EncounterData SelectedEncounter { get; private set; }
    public IReadOnlyList<string> ClearedNodeIds => clearedNodeIds;

    public void ResetToStart()
    {
        clearedNodeIds.Clear();
        LastCompletedNodeId = null;
        ClearSelection();
    }

    public void SelectNode(string nodeId, MapNodeType nodeType, EncounterData encounter)
    {
        SelectedNodeId = nodeId;
        SelectedNodeType = nodeType;
        SelectedEncounter = encounter;
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
    }
}
