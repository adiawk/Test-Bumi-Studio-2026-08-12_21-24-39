using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combat-only cheat buttons for killing the player or all enemies.
/// </summary>
public class CombatDebugUI : MonoBehaviour
{
    [SerializeField] CombatManager combatManager;
    [SerializeField] Button killPlayerButton;
    [SerializeField] Button killEnemiesButton;

    void Awake()
    {
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

        if (killPlayerButton != null)
            killPlayerButton.onClick.AddListener(OnKillPlayerClicked);
        if (killEnemiesButton != null)
            killEnemiesButton.onClick.AddListener(OnKillEnemiesClicked);
    }

    void OnDestroy()
    {
        if (killPlayerButton != null)
            killPlayerButton.onClick.RemoveListener(OnKillPlayerClicked);
        if (killEnemiesButton != null)
            killEnemiesButton.onClick.RemoveListener(OnKillEnemiesClicked);
    }

    void OnKillPlayerClicked()
    {
        if (combatManager == null)
        {
            Debug.LogError("[CombatDebugUI] CombatManager is missing.");
            return;
        }

        combatManager.DebugKillPlayer();
    }

    void OnKillEnemiesClicked()
    {
        if (combatManager == null)
        {
            Debug.LogError("[CombatDebugUI] CombatManager is missing.");
            return;
        }

        combatManager.DebugKillAllEnemies();
    }
}
