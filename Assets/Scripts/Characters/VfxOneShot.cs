using UnityEngine;

/// <summary>
/// One-shot world VFX. Swap the SpriteRenderer sprite or replace this prefab.
/// Assign AudioClip here or on CharacterFeedback. Playback goes through AudioManager.
/// </summary>
public class VfxOneShot : MonoBehaviour
{
    [SerializeField] float lifetime = 0.35f;
    [SerializeField] AudioClip sfx;
    [SerializeField] float startScale = 0.8f;
    [SerializeField] float endScale = 1.35f;

    SpriteRenderer spriteRenderer;
    float age;
    Color baseColor = Color.white;

    public void Play(Sprite sprite, AudioClip clip, Color color)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (sprite != null)
            spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 40;
        baseColor = color;
        spriteRenderer.color = color;

        AudioClip playClip = clip != null ? clip : sfx;
        PlaySfx(playClip);

        transform.localScale = Vector3.one * startScale;
        age = 0f;
        enabled = true;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        if (spriteRenderer != null)
        {
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, t);
            spriteRenderer.color = c;
        }

        if (t >= 1f)
            Destroy(gameObject);
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }
}
