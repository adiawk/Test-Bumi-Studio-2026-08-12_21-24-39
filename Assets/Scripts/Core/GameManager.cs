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
        LoadScene(SceneNames.Combat);
    }

public void NotifyCombatWon()
    {
        if (runManager == null)
            return;

        StageId stage = runManager.Stages.CurrentStage;
        if (stage == StageId.Stage1Combat)
        {
            runManager.Stages.Advance();
            LoadReward();
            return;
        }

        if (stage == StageId.Stage2Combat)
        {
            runManager.Stages.Advance();
            LoadCombat();
            return;
        }

        runManager.Stages.Advance();
        runManager.SetLastRunWon(true);
        LoadResult();
    }

public void NotifyCombatLost()
    {
        if (runManager != null)
            runManager.SetLastRunWon(false);
        LoadResult();
    }

public void NotifyRewardChosen(CardData card)
    {
        if (runManager != null)
        {
            runManager.AddCardToRunDeck(card);
            runManager.Stages.Advance();
        }
        LoadCombat();
    }




    public void LoadMainMenu() => LoadScene(SceneNames.MainMenu);
    public void LoadCombat() => LoadScene(SceneNames.Combat);
    public void LoadReward() => LoadScene(SceneNames.Reward);
    public void LoadResult() => LoadScene(SceneNames.Result);

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
