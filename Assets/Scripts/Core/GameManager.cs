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

    public int GrantCombatClearReward()
    {
        if (runManager == null)
            return 0;

        return runManager.GrantCombatClearReward(runManager.Stages.SelectedNodeType);
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

    public bool NotifyShopPurchase(RewardData reward)
    {
        if (runManager == null || reward == null)
            return false;
        if (!runManager.TrySpendCoins(reward.CoinCost))
            return false;

        runManager.TryApplyReward(reward);
        return true;
    }

    public bool NotifyUpgradePurchased(RuntimeCard card)
    {
        if (runManager == null || card == null || !card.CanUpgrade)
            return false;
        if (!runManager.TrySpendCoins(runManager.UpgradeCoinCost))
            return false;

        card.Upgrade();
        return true;
    }

    public void NotifyShopClosed()
    {
        CompleteVisit();
    }

    public void CompleteVisit()
    {
        if (runManager != null)
            runManager.Stages.CompleteSelectedNode();
        LoadMap();
    }

    static bool IsVisitNode(MapNodeType nodeType)
    {
        return nodeType == MapNodeType.Reward || nodeType == MapNodeType.Shop || nodeType == MapNodeType.Upgrade;
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
