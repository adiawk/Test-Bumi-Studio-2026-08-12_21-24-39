using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Victory/defeat screen. New Run returns to the main menu.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI resultText;
    [SerializeField] Button newRunButton;

    void Awake()
    {
        if (newRunButton != null)
            newRunButton.onClick.AddListener(OnNewRunClicked);
    }

void Start()
    {
        bool won = GameManager.Instance != null && GameManager.Instance.Run != null && GameManager.Instance.Run.LastRunWon;
        SetResult(won ? "Victory" : "Defeat");
    }


    void OnDestroy()
    {
        if (newRunButton != null)
            newRunButton.onClick.RemoveListener(OnNewRunClicked);
    }

    public void SetResult(string message)
    {
        if (resultText != null)
            resultText.text = message;
    }

void OnNewRunClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ResultUI] GameManager is missing.");
            return;
        }

        GameManager.Instance.StartNewRun();
    }
}
