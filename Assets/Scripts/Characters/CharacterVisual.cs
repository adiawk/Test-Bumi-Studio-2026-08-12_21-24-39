using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies layered world sprites under Visual/Sprite(n), or a per-enemy Animator prefab.
/// Combat rules stay on Player or Enemy on this same root.
/// Skips dummy sprite overwrite when an Animator is present (custom character).
/// </summary>
public class CharacterVisual : MonoBehaviour
{
    [SerializeField] Transform visualRoot;
    [SerializeField] CharacterStatusUI statusPrefab;
    [SerializeField] Vector3 statusOffset = new Vector3(0f, 1.9f, 0f);
    [SerializeField] Color selectedTint = new Color(1.2f, 1.15f, 0.65f, 1f);
    [SerializeField] Color deadTint = new Color(0.35f, 0.35f, 0.4f, 0.65f);

    SpriteRenderer[] layers = System.Array.Empty<SpriteRenderer>();
    Color[] baseColors = System.Array.Empty<Color>();
    bool selected;
    bool alive = true;
    bool usesAnimator;

    void Awake()
    {
        EnsureStatus();
    }

    public void BindIfNeeded()
    {
        if (visualRoot == null)
        {
            Transform found = transform.Find("Visual");
            visualRoot = found != null ? found : transform;
        }

        usesAnimator = visualRoot.GetComponentInChildren<Animator>(true) != null;
        layers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[layers.Length];
        for (int i = 0; i < layers.Length; i++)
            baseColors[i] = layers[i].color;
    }

    public void ApplyLayers(IReadOnlyList<Sprite> sprites)
    {
        BindIfNeeded();
        EnsureStatus();
        if (usesAnimator)
            return;
        if (sprites == null)
            return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (i < sprites.Count && sprites[i] != null)
            {
                layers[i].sprite = sprites[i];
                layers[i].enabled = true;
            }
        }

        ApplyTint();
    }

    public void ApplyVisualPrefab(GameObject prefab)
    {
        BindIfNeeded();
        EnsureStatus();
        if (prefab == null)
            return;

        HideDummySpriteLayers();
        GameObject instance = Instantiate(prefab, visualRoot);
        instance.name = prefab.name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        BindIfNeeded();
    }

    void HideDummySpriteLayers()
    {
        if (visualRoot == null)
            return;

        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = visualRoot.GetChild(i);
            if (child.GetComponentInChildren<Animator>(true) != null)
                continue;
            if (child.GetComponent<SpriteRenderer>() != null)
                child.gameObject.SetActive(false);
        }
    }

    void EnsureStatus()
    {
        if (GetComponentInChildren<CharacterStatusUI>(true) != null)
            return;
        if (statusPrefab == null)
            return;

        CharacterStatusUI status = Instantiate(statusPrefab, transform);
        status.name = "CharacterStatus";
        status.transform.localPosition = statusOffset;
        status.transform.localRotation = Quaternion.identity;
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        ApplyTint();
    }

    public void SetAlive(bool isAlive)
    {
        alive = isAlive;
        if (!alive)
            selected = false;
        ApplyTint();
    }

    public void HideBody()
    {
        BindIfNeeded();
        if (visualRoot != null && visualRoot != transform)
            visualRoot.gameObject.SetActive(false);

        CharacterStatusUI status = GetComponentInChildren<CharacterStatusUI>(true);
        if (status != null)
            status.gameObject.SetActive(false);
    }

    void ApplyTint()
    {
        if (layers == null || layers.Length == 0)
            BindIfNeeded();
        if (usesAnimator)
            return;

        for (int i = 0; i < layers.Length; i++)
        {
            Color color = i < baseColors.Length ? baseColors[i] : Color.white;
            if (!alive)
                color = deadTint;
            else if (selected)
                color = selectedTint;
            layers[i].color = color;
        }
    }
}
