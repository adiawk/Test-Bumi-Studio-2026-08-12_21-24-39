using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space HP / block / intent billboard parented to a character root.
/// </summary>
public class CharacterStatusUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI hpText;
    [SerializeField] Slider hpSlider;
    [SerializeField] TextMeshProUGUI blockText;
    [SerializeField] TextMeshProUGUI intentText;
    [SerializeField] bool showIntent = true;

    Player player;
    Enemy enemy;
    Canvas canvas;

    void Awake()
    {
        player = GetComponentInParent<Player>();
        enemy = GetComponentInParent<Enemy>();
        canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        if (hpSlider == null)
            hpSlider = GetComponentInChildren<Slider>(true);
        if (hpSlider != null)
            hpSlider.interactable = false;

        if (intentText != null)
            intentText.gameObject.SetActive(showIntent && enemy != null);
    }

    void LateUpdate()
    {
        if (enemy != null)
        {
            SetLine(nameText, enemy.DisplayName);
            SetHp(enemy.Hp, enemy.MaxHp);
            SetLine(blockText, FormatBlockAndStatus(enemy.Block, enemy.Statuses));
            SetLine(intentText, enemy.IntentLabel);
            return;
        }

        if (player != null)
        {
            SetLine(nameText, "Player");
            SetHp(player.Hp, player.MaxHp);
            SetLine(blockText, FormatBlockAndStatus(player.Block, player.Statuses));
            if (intentText != null)
                intentText.gameObject.SetActive(false);
        }
    }

    static string FormatBlockAndStatus(int block, StatusBag statuses)
    {
        string status = statuses != null ? statuses.BuildLabel() : string.Empty;
        if (string.IsNullOrEmpty(status))
            return block.ToString();
        return block + " | " + status;
    }

    void SetHp(int current, int max)
    {
        SetLine(hpText, current + "/" + max);
        if (hpSlider == null)
            return;

        hpSlider.minValue = 0f;
        hpSlider.maxValue = Mathf.Max(1, max);
        hpSlider.wholeNumbers = true;
        hpSlider.value = Mathf.Clamp(current, 0, max);
    }

    static void SetLine(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
