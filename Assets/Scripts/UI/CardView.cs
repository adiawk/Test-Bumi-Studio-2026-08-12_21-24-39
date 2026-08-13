using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One clickable card in the hand. Disabled when the player cannot pay its cost.
/// </summary>
public class CardView : MonoBehaviour
{
    RuntimeCard card;
    CombatManager combatManager;
    Button button;

    public void Bind(RuntimeCard runtimeCard, CombatManager combat)
    {
        card = runtimeCard;
        combatManager = combat;

        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }

void LateUpdate()
    {
        if (combatManager == null || card == null)
            return;

        if (button != null)
            button.interactable = combatManager.CanPlayCard(card);

        Image background = GetComponent<Image>();
        if (background != null)
        {
            bool pending = combatManager.PendingCard == card;
            background.color = pending
                ? new Color(0.42f, 0.36f, 0.16f, 1f)
                : new Color(0.18f, 0.2f, 0.28f, 1f);
        }
    }

void OnClicked()
    {
        if (combatManager != null)
            combatManager.BeginPlay(card);
    }

    public static GameObject Create(Transform parent, RuntimeCard runtimeCard, CombatManager combat)
    {
        var root = new GameObject("Card_" + runtimeCard.Name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CardView));
        root.transform.SetParent(parent, false);

        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170, 210);

        var image = root.GetComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.28f, 1f);

        AddLabel(root.transform, "Cost", "Cost " + runtimeCard.Cost, new Vector2(0, 80), 18);
        AddLabel(root.transform, "Name", runtimeCard.Name, new Vector2(0, 40), 20);
        string description = runtimeCard.Data != null ? runtimeCard.Data.Description : string.Empty;
        AddLabel(root.transform, "Desc", description, new Vector2(0, -30), 16);

        root.GetComponent<CardView>().Bind(runtimeCard, combat);
        return root;
    }

    static void AddLabel(Transform parent, string name, string text, Vector2 pos, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 70);
        rt.anchoredPosition = pos;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
    }
}
