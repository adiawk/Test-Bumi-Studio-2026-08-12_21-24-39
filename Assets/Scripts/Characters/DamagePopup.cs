using TMPro;
using UnityEngine;

/// <summary>
/// Floating combat number. Prefab is optional; CharacterFeedback can spawn a dummy TMP.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [SerializeField] float lifetime = 0.7f;
    [SerializeField] float rise = 0.85f;
    [SerializeField] TextMeshPro label;

    float age;
    Vector3 start;
    Color color;

    public void Play(int amount, Color popupColor)
    {
        if (label == null)
            label = GetComponent<TextMeshPro>();
        if (label == null)
            label = gameObject.AddComponent<TextMeshPro>();

        ConfigureLabel(label);
        color = popupColor;
        label.text = amount.ToString();
        label.color = color;
        start = transform.position;
        transform.localScale = Vector3.one;
        age = 0f;
        enabled = true;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);
        transform.position = start + Vector3.up * (rise * t);
        transform.localScale = Vector3.one * Mathf.Lerp(1.12f, 0.92f, t);
        if (label != null)
        {
            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, t * t);
            label.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }

    public static DamagePopup CreateDummy(Vector3 worldPosition)
    {
        var go = new GameObject("DamagePopup");
        go.transform.position = worldPosition;
        var popup = go.AddComponent<DamagePopup>();
        popup.label = go.AddComponent<TextMeshPro>();
        ConfigureLabel(popup.label);
        return popup;
    }

    static void ConfigureLabel(TextMeshPro tmp)
    {
        if (tmp == null)
            return;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
            tmp.font = font;

        tmp.fontSize = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = new Vector2(3f, 1.2f);

        Renderer meshRenderer = tmp.GetComponent<Renderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 45;
    }
}
