using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MapNodeVisualState
{
    Locked,
    Reachable,
    Cleared
}

/// <summary>
/// One chapter-map node. Place copies in the map Scroll View and wire Next Nodes.
/// </summary>
public class StageNodeButton : MonoBehaviour
{
    [SerializeField] string nodeId;
    [SerializeField] string displayName;
    [SerializeField] MapNodeType nodeType = MapNodeType.Combat;
    [SerializeField] EncounterData encounter;
    [SerializeField] bool isStartingNode;
    [SerializeField] List<StageNodeButton> nextNodes = new List<StageNodeButton>();
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] Image background;

    public string NodeId => string.IsNullOrEmpty(nodeId) ? name : nodeId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? nodeType.ToString() : displayName;
    public MapNodeType NodeType => nodeType;
    public EncounterData Encounter => encounter;
    public bool IsStartingNode => isStartingNode;
    public IReadOnlyList<StageNodeButton> NextNodes => nextNodes;

    public event Action<StageNodeButton> Clicked;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (label == null)
            label = GetComponentInChildren<TextMeshProUGUI>();
        if (background == null)
            background = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    public void Configure(string id, string labelText, MapNodeType type, EncounterData data, bool starting)
    {
        nodeId = id;
        displayName = labelText;
        nodeType = type;
        encounter = data;
        isStartingNode = starting;
        RefreshLabel();
    }

    public void AddNextNode(StageNodeButton node)
    {
        if (node != null && !nextNodes.Contains(node))
            nextNodes.Add(node);
    }

    public void SetVisualState(MapNodeVisualState state)
    {
        if (button != null)
            button.interactable = state == MapNodeVisualState.Reachable;

        if (background != null)
        {
            switch (state)
            {
                case MapNodeVisualState.Reachable:
                    background.color = new Color(0.35f, 0.42f, 0.28f, 1f);
                    break;
                case MapNodeVisualState.Cleared:
                    background.color = new Color(0.18f, 0.18f, 0.22f, 1f);
                    break;
                default:
                    background.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
                    break;
            }
        }

        RefreshLabel(state);
    }

    public void RefreshLabel(MapNodeVisualState state = MapNodeVisualState.Locked)
    {
        if (label == null)
            return;

        string suffix = state == MapNodeVisualState.Cleared ? "\nCleared" : "";
        label.text = DisplayName + suffix;
    }

    void OnClicked()
    {
        Clicked?.Invoke(this);
    }
}
