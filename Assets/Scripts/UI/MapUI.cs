using System.Collections;
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
    [SerializeField] ScrollRect scrollRect;

    [Header("Connection Lines (Editor)")]
    [SerializeField] RectTransform connectionRoot;
    [SerializeField] Sprite lineSprite;
    [SerializeField] Color lineColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] float lineThickness = 8f;
    [SerializeField] float lineInset = 40f;
    [SerializeField] Image.Type lineImageType = Image.Type.Simple;
    [SerializeField] float linePixelsPerUnitMultiplier = 1f;

    readonly List<StageNodeButton> nodes = new List<StageNodeButton>();

    void Start()
    {
        CollectNodes();
        BindNodes();
        Refresh();
        StartCoroutine(SnapToActiveNodeNextFrame());
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

        WarnDuplicateNodeIds();
    }

    void WarnDuplicateNodeIds()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            StageNodeButton node = nodes[i];
            if (node == null)
                continue;

            for (int j = i + 1; j < nodes.Count; j++)
            {
                StageNodeButton other = nodes[j];
                if (other != null && other.NodeId == node.NodeId)
                    Debug.LogError("[MapUI] Duplicate nodeId '" + node.NodeId + "' on " + node.name + " and " + other.name + ".");
            }
        }
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

    IEnumerator SnapToActiveNodeNextFrame()
    {
        yield return null;
        SnapToActiveNode();
    }

    void SnapToActiveNode()
    {
        StageNodeButton focus = FindActiveFocusNode();
        if (focus != null)
            SnapTo(focus.transform as RectTransform);
    }

    StageNodeButton FindActiveFocusNode()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        StageManager stages = run != null ? run.Stages : null;
        bool atStart = stages == null || string.IsNullOrEmpty(stages.LastCompletedNodeId);
        StageNodeButton lastCompleted = FindNode(stages != null ? stages.LastCompletedNodeId : null);

        StageNodeButton first = null;
        int count = 0;
        float sumY = 0f;
        for (int i = 0; i < nodes.Count; i++)
        {
            StageNodeButton node = nodes[i];
            if (node == null || !IsReachable(node, atStart, lastCompleted))
                continue;

            sumY += node.transform.position.y;
            count++;
            if (first == null)
                first = node;
        }

        if (count == 0)
            return lastCompleted;
        if (count == 1)
            return first;

        float averageY = sumY / count;
        StageNodeButton best = first;
        float bestDist = Mathf.Abs(first.transform.position.y - averageY);
        for (int i = 0; i < nodes.Count; i++)
        {
            StageNodeButton node = nodes[i];
            if (node == null || !IsReachable(node, atStart, lastCompleted))
                continue;

            float dist = Mathf.Abs(node.transform.position.y - averageY);
            if (dist < bestDist)
            {
                best = node;
                bestDist = dist;
            }
        }

        return best;
    }

    void SnapTo(RectTransform target)
    {
        if (scrollRect == null || target == null || scrollRect.content == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.StopMovement();

        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport != null
            ? scrollRect.viewport
            : (RectTransform)scrollRect.transform;

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float scrollable = contentHeight - viewportHeight;
        if (scrollable <= 1f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        float targetY = content.InverseTransformPoint(target.position).y;
        float visibleBottom = Mathf.Clamp(targetY - viewportHeight * 0.5f, 0f, scrollable);
        scrollRect.verticalNormalizedPosition = visibleBottom / scrollable;
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

#if UNITY_EDITOR
    [ContextMenu("Refresh Connection Lines")]
    void RefreshConnectionLines()
    {
        if (nodeRoot == null)
        {
            Debug.LogWarning("[MapUI] nodeRoot is not assigned.");
            return;
        }

        EnsureConnectionRoot();
        ClearConnectionLines();

        StageNodeButton[] found = nodeRoot.GetComponentsInChildren<StageNodeButton>(true);
        int created = 0;
        for (int i = 0; i < found.Length; i++)
        {
            StageNodeButton from = found[i];
            if (from == null)
                continue;

            IReadOnlyList<StageNodeButton> next = from.NextNodes;
            for (int j = 0; j < next.Count; j++)
            {
                StageNodeButton to = next[j];
                if (to == null || to == from)
                    continue;

                if (CreateLine(from.transform as RectTransform, to.transform as RectTransform, from.name, to.name))
                    created++;
            }
        }

        connectionRoot.SetAsFirstSibling();
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log($"[MapUI] Refreshed {created} connection line(s).");
    }

    void EnsureConnectionRoot()
    {
        if (connectionRoot != null)
            return;

        GameObject go = new GameObject("ConnectionLines", typeof(RectTransform));
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create ConnectionLines");
        connectionRoot = go.GetComponent<RectTransform>();
        connectionRoot.SetParent(nodeRoot, false);
        connectionRoot.anchorMin = Vector2.zero;
        connectionRoot.anchorMax = Vector2.one;
        connectionRoot.pivot = new Vector2(0.5f, 0.5f);
        connectionRoot.offsetMin = Vector2.zero;
        connectionRoot.offsetMax = Vector2.zero;
        connectionRoot.SetAsFirstSibling();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    void ClearConnectionLines()
    {
        for (int i = connectionRoot.childCount - 1; i >= 0; i--)
            UnityEditor.Undo.DestroyObjectImmediate(connectionRoot.GetChild(i).gameObject);
    }

    bool CreateLine(RectTransform from, RectTransform to, string fromName, string toName)
    {
        if (from == null || to == null)
            return false;

        Vector2 a = connectionRoot.InverseTransformPoint(from.position);
        Vector2 b = connectionRoot.InverseTransformPoint(to.position);
        Vector2 delta = b - a;
        float length = delta.magnitude;
        if (length < 1f)
            return false;

        Vector2 dir = delta / length;
        a += dir * lineInset;
        b -= dir * lineInset;
        delta = b - a;
        length = delta.magnitude;
        if (length < 1f)
            return false;

        GameObject go = new GameObject($"Line_{fromName}_{toName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Connection Line");
        go.transform.SetParent(connectionRoot, false);

        Image image = go.GetComponent<Image>();
        image.sprite = lineSprite;
        image.color = lineColor;
        image.type = lineImageType;
        image.pixelsPerUnitMultiplier = linePixelsPerUnitMultiplier;
        image.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(length, lineThickness);
        rt.anchoredPosition = (a + b) * 0.5f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        rt.localScale = Vector3.one;
        return true;
    }
#endif
}
