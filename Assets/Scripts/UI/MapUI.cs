using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chapter map. Uses StageNodeButton copies you place under the Scroll View Content.
/// </summary>
public class MapUI : MonoBehaviour
{
    [SerializeField] Transform nodeRoot;
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] TextMeshProUGUI coinsText;
    [SerializeField] ScrollRect scrollRect;

    readonly List<StageNodeButton> nodes = new List<StageNodeButton>();

    void Start()
    {
        CollectNodes();
        BindNodes();
        Refresh();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    void OnDestroy()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                nodes[i].Clicked -= OnNodeClicked;
        }
    }

    void CollectNodes()
    {
        nodes.Clear();
        if (nodeRoot == null)
            return;

        StageNodeButton[] found = nodeRoot.GetComponentsInChildren<StageNodeButton>(true);
        for (int i = 0; i < found.Length; i++)
            nodes.Add(found[i]);
    }

    void BindNodes()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                nodes[i].Clicked += OnNodeClicked;
        }
    }

    void Refresh()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        StageManager stages = run != null ? run.Stages : null;
        bool atStart = stages == null || string.IsNullOrEmpty(stages.LastCompletedNodeId);
        StageNodeButton lastCompleted = FindNode(stages != null ? stages.LastCompletedNodeId : null);

        for (int i = 0; i < nodes.Count; i++)
        {
            StageNodeButton node = nodes[i];
            if (node == null)
                continue;

            MapNodeVisualState state = MapNodeVisualState.Locked;
            if (stages != null && stages.IsCleared(node.NodeId))
                state = MapNodeVisualState.Cleared;
            else if (IsReachable(node, atStart, lastCompleted))
                state = MapNodeVisualState.Reachable;

            node.SetVisualState(state);
        }

        if (promptText != null)
            promptText.text = atStart ? "Select a starting room" : "Select the next room";

        if (coinsText != null)
            coinsText.text = "Coins: " + (run != null ? run.Coins : 0);
    }

    static bool IsReachable(StageNodeButton node, bool atStart, StageNodeButton lastCompleted)
    {
        if (atStart)
            return node.IsStartingNode;

        if (lastCompleted == null)
            return false;

        IReadOnlyList<StageNodeButton> next = lastCompleted.NextNodes;
        for (int i = 0; i < next.Count; i++)
        {
            if (next[i] == node)
                return true;
        }

        return false;
    }

    StageNodeButton FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
            return null;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].NodeId == nodeId)
                return nodes[i];
        }

        return null;
    }

    void OnNodeClicked(StageNodeButton node)
    {
        if (GameManager.Instance == null || node == null)
        {
            Debug.LogError("[MapUI] GameManager is missing.");
            return;
        }

        GameManager.Instance.NotifyMapNodeSelected(node);
    }
}
