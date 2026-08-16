using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent run HUD for HP and Coins. Hidden on Main Menu; follows App across other scenes.
/// Place this on a Canvas prefab and assign it to GameManager.
/// </summary>
public class RunHud : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] TextMeshProUGUI coinsText;

    Player combatPlayer;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void LateUpdate()
    {
        if (canvas == null || !canvas.gameObject.activeSelf)
            return;

        if (combatPlayer == null)
            combatPlayer = FindFirstObjectByType<Player>();

        Refresh();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        combatPlayer = null;
        ApplyScene(scene.name);
    }

    void ApplyScene(string sceneName)
    {
        bool show = sceneName != SceneNames.MainMenu;
        if (canvas != null)
            canvas.gameObject.SetActive(show);
        else
            gameObject.SetActive(show);
    }

    void Refresh()
    {
        RunManager run = GameManager.Instance != null ? GameManager.Instance.Run : null;
        int hp = 0;
        int maxHp = 0;
        int coins = 0;
        if (run != null)
        {
            hp = run.CurrentHp;
            maxHp = run.MaxHp;
            coins = run.Coins;
        }

        if (combatPlayer != null)
        {
            hp = combatPlayer.Hp;
            maxHp = combatPlayer.MaxHp;
        }

        if (hpText != null)
            hpText.text = "HP " + hp + "/" + maxHp;
        if (coinsText != null)
            coinsText.text = "Coins " + coins;
    }
}
