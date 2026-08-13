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
/// One chapter-map node. Duplicate this prefab in the map Scroll View and wire Next Nodes.
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

    public void SetVisualState(MapNodeVisualState state)
    {
        if (button != null)
            button.interactable = state == MapNodeVisualState.Reachable;

        if (background != null)
        {
            if (state == MapNodeVisualState.Reachable)
                background.color = new Color(0.35f, 0.42f, 0.28f, 1f);
            else if (state == MapNodeVisualState.Cleared)
                background.color = new Color(0.18f, 0.18f, 0.22f, 1f);
            else
                background.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
        }

        if (label != null)
        {
            string suffix = state == MapNodeVisualState.Cleared ? "\nCleared" : "";
            label.text = DisplayName + suffix;
        }
    }

    void OnClicked()
    {
        Clicked?.Invoke(this);
    }
}
