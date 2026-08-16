using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the shared UI click SFX when this Button is pressed.
/// </summary>
[RequireComponent(typeof(Button))]
public class UiButtonClick : MonoBehaviour
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    void OnClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();
    }
}
