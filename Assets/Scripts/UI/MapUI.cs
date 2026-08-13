using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chapter map. Uses nodes you place under the Scroll View Content.
/// If Content has no nodes, a 3-path sample chapter is spawned from the prefab.
/// </summary>
public class MapUI : MonoBehaviour
{
    [SerializeField] Transform nodeRoot;
    [SerializeField] StageNodeButton nodePrefab;
    [SerializeField] TextMeshProUGUI promptText;
    [SerializeField] ScrollRect scrollRect;

    readonly List<StageNodeButton> nodes = new List<StageNodeButton>();

    void Start()
    {
        CollectOrSpawnNodes();
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

    void CollectOrSpawnNodes()
    {
        nodes.Clear();
        if (nodeRoot != null)
            nodeRoot.GetComponentsInChildren<StageNodeButton>(true, nodes);

        if (nodes.Count == 0)
            SpawnDefaultChapter();
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

    void SpawnDefaultChapter()
    {
        if (nodePrefab == null || nodeRoot == null)
        {
            Debug.LogWarning("[MapUI] Place StageNodeButton prefabs under Content, or assign Node Prefab and Node Root.");
            return;
        }

        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        EncounterData normal = run != null ? run.GetEncounterForNodeType(MapNodeType.Combat) : null;
        EncounterData elite = run != null ? run.GetEncounterForNodeType(MapNodeType.Elite) : null;
        EncounterData boss = run != null ? run.GetEncounterForNodeType(MapNodeType.Boss) : null;

        StageNodeButton a1 = SpawnNode("path_a1", "Path A", MapNodeType.Combat, normal, true, new Vector2(-280f, 180f));
        StageNodeButton b1 = SpawnNode("path_b1", "Path B", MapNodeType.Combat, normal, true, new Vector2(0f, 180f));
        StageNodeButton c1 = SpawnNode("path_c1", "Path C", MapNodeType.Elite, elite, true, new Vector2(280f, 180f));
        StageNodeButton a2 = SpawnNode("path_a2", "Fight", MapNodeType.Combat, elite, false, new Vector2(-280f, 520f));
        StageNodeButton b2 = SpawnNode("path_b2", "Reward", MapNodeType.Reward, null, false, new Vector2(0f, 520f));
        StageNodeButton c2 = SpawnNode("path_c2", "Fight", MapNodeType.Combat, elite, false, new Vector2(280f, 520f));
        StageNodeButton bossNode = SpawnNode("boss", "Boss", MapNodeType.Boss, boss, false, new Vector2(0f, 880f));

        a1.AddNextNode(a2);
        b1.AddNextNode(b2);
        c1.AddNextNode(c2);
        a2.AddNextNode(bossNode);
        b2.AddNextNode(bossNode);
        c2.AddNextNode(bossNode);

        nodes.Add(a1);
        nodes.Add(b1);
        nodes.Add(c1);
        nodes.Add(a2);
        nodes.Add(b2);
        nodes.Add(c2);
        nodes.Add(bossNode);
    }

    StageNodeButton SpawnNode(string id, string labelText, MapNodeType type, EncounterData encounter, bool starting, Vector2 anchoredPos)
    {
        StageNodeButton node = Instantiate(nodePrefab, nodeRoot);
        node.name = id;
        node.Configure(id, labelText, type, encounter, starting);

        RectTransform rect = node.transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = anchoredPos;

        return node;
    }
}
