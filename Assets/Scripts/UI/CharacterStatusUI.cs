using TMPro;
using UnityEngine;

/// <summary>
/// World-space HP / block / intent billboard parented to a character root.
/// </summary>
public class CharacterStatusUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI hpText;
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

        if (intentText != null)
            intentText.gameObject.SetActive(showIntent && enemy != null);
    }

    void LateUpdate()
    {
        if (enemy != null)
        {
            SetLine(nameText, enemy.DisplayName);
            SetLine(hpText, "HP " + enemy.Hp + "/" + enemy.MaxHp);
            SetLine(blockText, "Block " + enemy.Block);
            SetLine(intentText, enemy.IntentLabel);
            return;
        }

        if (player != null)
        {
            SetLine(nameText, "Player");
            SetLine(hpText, "HP " + player.Hp + "/" + player.MaxHp);
            SetLine(blockText, "Block " + player.Block);
            if (intentText != null)
                intentText.gameObject.SetActive(false);
        }
    }

    static void SetLine(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value;
    }
}
