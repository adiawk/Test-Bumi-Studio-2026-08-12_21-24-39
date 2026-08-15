using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent app entry point. Owns scene routing only.
/// Does not own combat rules, cards, or deck state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] RunManager runManager;

    public RunManager Run => runManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StartNewRun()
    {
        if (runManager == null)
            runManager = GetComponent<RunManager>();

        runManager.StartNewRun();
        LoadMap();
    }

    public void NotifyMapNodeSelected(StageNodeButton node)
    {
        if (runManager == null || node == null)
            return;

        EncounterData encounter = node.Encounter;
        if (encounter == null && !IsVisitNode(node.NodeType))
            encounter = runManager.GetEncounterForNodeType(node.NodeType);

        runManager.Stages.SelectNode(node.NodeId, node.NodeType, encounter);

        if (IsVisitNode(node.NodeType))
            LoadReward();
        else
            LoadCombat();
    }

    public void NotifyCombatWon()
    {
        if (runManager == null)
            return;

        MapNodeType nodeType = runManager.Stages.SelectedNodeType;
        runManager.Stages.CompleteSelectedNode();

        if (nodeType == MapNodeType.Boss)
        {
            runManager.SetLastRunWon(true);
            LoadResult();
            return;
        }

        LoadMap();
    }

    public void NotifyCombatLost()
    {
        if (runManager != null)
            runManager.SetLastRunWon(false);
        LoadResult();
    }

    public void NotifyRewardChosen(RewardData reward)
    {
        if (runManager != null)
        {
            runManager.TryApplyReward(reward);
            runManager.Stages.CompleteSelectedNode();
        }
        LoadMap();
    }

    public void NotifyShopPurchase(RewardData reward)
    {
        if (runManager != null)
            runManager.TryApplyReward(reward);
    }

    public void NotifyShopClosed()
    {
        if (runManager != null)
            runManager.Stages.CompleteSelectedNode();
        LoadMap();
    }

    static bool IsVisitNode(MapNodeType nodeType)
    {
        return nodeType == MapNodeType.Reward || nodeType == MapNodeType.Shop;
    }

    public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
    public void LoadMap() => LoadScene(SceneNames.Map);
    public void LoadCombat() => LoadScene(SceneNames.Combat);
    public void LoadReward() => LoadScene(SceneNames.Reward);
    public void LoadResult() => LoadScene(SceneNames.Result);

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
