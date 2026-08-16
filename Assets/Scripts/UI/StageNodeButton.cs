using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
public class StageNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string nodeId;
    [SerializeField] string displayName;
    [SerializeField] MapNodeType nodeType = MapNodeType.Combat;
    [SerializeField] EncounterData encounter;
    [Tooltip("Reward and Shop nodes. Leave empty to use the global Run Manager pool.")]
    [SerializeField] List<RewardData> rewardPool = new List<RewardData>();
    [SerializeField] bool isStartingNode;
    [SerializeField] List<StageNodeButton> nextNodes = new List<StageNodeButton>();
    [SerializeField] Button button;
    [SerializeField] TextMeshProUGUI label;
    [SerializeField] Image background;

    [Header("Hover")]
    [SerializeField] float hoverScale = 1.12f;
    [SerializeField] float scaleSpring = 16f;

    Vector3 restScale = Vector3.one;
    bool hovered;

    public string NodeId => string.IsNullOrEmpty(nodeId) ? name : nodeId;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? nodeType.ToString() : displayName;
    public MapNodeType NodeType => nodeType;
    public EncounterData Encounter => encounter;
    public IReadOnlyList<RewardData> RewardPool => rewardPool;
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
        restScale = transform.localScale;
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

        hovered = false;
        transform.localScale = restScale;
    }

    void Update()
    {
        Vector3 goal = restScale * (hovered ? hoverScale : 1f);
        float t = 1f - Mathf.Exp(-scaleSpring * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, goal, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
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
