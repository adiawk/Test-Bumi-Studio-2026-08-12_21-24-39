using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu buttons only. No gameplay rules.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button startNewRunButton;

    void Awake()
    {
        if (startNewRunButton != null)
            startNewRunButton.onClick.AddListener(OnStartNewRunClicked);
    }

    void OnDestroy()
    {
        if (startNewRunButton != null)
            startNewRunButton.onClick.RemoveListener(OnStartNewRunClicked);
    }

    void OnStartNewRunClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] GameManager is missing.");
            return;
        }

        GameManager.Instance.StartNewRun();
    }
}
