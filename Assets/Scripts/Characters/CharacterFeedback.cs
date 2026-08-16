using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns dummy (or custom) VFX, plays Animator triggers, and shakes Visual.
/// Drop in prefabs, sprites, AudioClips, and an Animator. Empty trigger names are skipped.
/// </summary>
public class CharacterFeedback : MonoBehaviour
{
    [Header("Anchor")]
    [SerializeField] Transform visualRoot;
    [SerializeField] Transform shakeRoot;
    [SerializeField] Animator animator;

    [Header("Animator triggers (must match your controller)")]
    [SerializeField] string attackTrigger = "Attack";
    [SerializeField] string blockTrigger = "Block";
    [SerializeField] string healTrigger = "Heal";
    [SerializeField] string hitTrigger = "Hit";
    [SerializeField] string dieTrigger = "Die";

    [Header("Prefabs (optional — leave empty to use dummy sprites)")]
    [SerializeField] VfxOneShot hitPrefab;
    [SerializeField] VfxOneShot blockPrefab;
    [SerializeField] VfxOneShot healPrefab;
    [SerializeField] DamagePopup damagePopupPrefab;

    [Header("Damage popup")]
    [SerializeField] float mixedHitDelay = 0.2f;
    [SerializeField] Vector3 damagePopupOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] float damagePopupJitter = 0.18f;

    [Header("Dummy / override sprites")]
    [SerializeField] Sprite hitSprite;
    [SerializeField] Sprite blockSprite;
    [SerializeField] Sprite healSprite;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip attackSfx;
    [SerializeField] AudioClip hitSfx;
    [SerializeField] AudioClip blockSfx;
    [SerializeField] AudioClip healSfx;

    [Header("Shake")]
    [SerializeField] float shakeDuration = 0.18f;
    [SerializeField] float shakeStrength = 0.12f;
    [SerializeField] float deathHideDelay = 0.4f;

    public float DeathHideDelay => Mathf.Max(shakeDuration, deathHideDelay);

    static readonly Color DamageRed = new Color(1f, 0.18f, 0.12f, 1f);
    static readonly Color BlockBlue = new Color(0.22f, 0.55f, 1f, 1f);

    Coroutine shakeRoutine;
    Vector3 visualRest = Vector3.zero;
    bool died;

    public bool HasAnimator => animator != null;

    void Awake()
    {
        Bind();
    }

    void Bind()
    {
        if (visualRoot == null)
        {
            Transform found = transform.Find("Visual");
            visualRoot = found != null ? found : transform;
        }

        if (shakeRoot == null)
        {
            Transform shake = transform.Find("Visual/Shake");
            if (shake == null && visualRoot != null)
                shake = visualRoot.Find("Shake");
            shakeRoot = shake != null ? shake : visualRoot;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (shakeRoot != null)
            visualRest = shakeRoot.localPosition;
    }

    public void PlayAttackFeedback()
    {
        PlayAnim(attackTrigger);
        PlayClip(attackSfx);
    }

    public void PlayHitFeedback()
    {
        PlayHpHit(0);
    }

    public void PlayIncomingHit(int blocked, int hpDamage)
    {
        if (blocked <= 0 && hpDamage <= 0)
            return;

        if (blocked > 0)
            PlayBlockedHit(blocked);

        if (hpDamage <= 0)
            return;

        if (blocked > 0 && isActiveAndEnabled)
            StartCoroutine(PlayHpHitAfterDelay(hpDamage));
        else
            PlayHpHit(hpDamage);
    }

    public void PlayBlockFeedback()
    {
        PlayAnim(blockTrigger);
        Spawn(blockPrefab, blockSprite, blockSfx, BlockBlue, 0f);
    }

    public void PlayHealFeedback()
    {
        PlayAnim(healTrigger);
        Spawn(healPrefab, healSprite, healSfx, new Color(0.4f, 1f, 0.5f, 0.95f), 0f);
    }

    public void PlayDeathFeedback()
    {
        if (died)
            return;
        died = true;
        PlayAnim(dieTrigger);
    }

    IEnumerator PlayHpHitAfterDelay(int amount)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, mixedHitDelay));
        PlayHpHit(amount);
    }

    void PlayBlockedHit(int amount)
    {
        PlayAnim(blockTrigger);
        Spawn(blockPrefab, blockSprite, blockSfx, BlockBlue, 0f);
        SpawnDamagePopup(amount, BlockBlue);
    }

    void PlayHpHit(int amount)
    {
        PlayAnim(hitTrigger);
        Spawn(hitPrefab, hitSprite, hitSfx, DamageRed, 45f);
        Shake();
        if (amount > 0)
            SpawnDamagePopup(amount, DamageRed);
    }

    void SpawnDamagePopup(int amount, Color color)
    {
        Vector3 pos = visualRoot != null ? visualRoot.position : transform.position;
        pos += damagePopupOffset;
        pos += (Vector3)(Random.insideUnitCircle * damagePopupJitter);

        DamagePopup popup = damagePopupPrefab != null
            ? Instantiate(damagePopupPrefab, pos, Quaternion.identity)
            : DamagePopup.CreateDummy(pos);
        popup.Play(amount, color);
    }

    void PlayAnim(string trigger)
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null || string.IsNullOrEmpty(trigger))
            return;
        if (!HasTrigger(animator, trigger))
            return;

        animator.ResetTrigger(trigger);
        animator.SetTrigger(trigger);
    }

    static bool HasTrigger(Animator anim, string trigger)
    {
        AnimatorControllerParameter[] parameters = anim.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger
                && parameters[i].name == trigger)
                return true;
        }

        return false;
    }

    void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(clip);
            return;
        }

        Vector3 pos = visualRoot != null ? visualRoot.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos);
    }

    void Spawn(VfxOneShot prefab, Sprite sprite, AudioClip clip, Color color, float zAngle)
    {
        Transform parent = visualRoot != null ? visualRoot : transform;
        VfxOneShot vfx;
        if (prefab != null)
            vfx = Instantiate(prefab, parent.position, Quaternion.Euler(0f, 0f, zAngle), parent);
        else
        {
            var go = new GameObject("Vfx", typeof(SpriteRenderer), typeof(VfxOneShot));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            vfx = go.GetComponent<VfxOneShot>();
        }

        vfx.Play(sprite, clip, color);
    }

    void Shake()
    {
        if (!isActiveAndEnabled)
            return;
        if (shakeRoot == null)
            Bind();
        if (shakeRoot == null)
            return;
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            shakeRoot.localPosition = visualRest + (Vector3)(Random.insideUnitCircle * shakeStrength);
            yield return null;
        }

        shakeRoot.localPosition = visualRest;
        shakeRoutine = null;
    }
}
