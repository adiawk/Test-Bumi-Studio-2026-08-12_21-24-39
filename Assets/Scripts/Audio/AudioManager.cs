using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent BGM and UI click SFX. Lives on App with GameManager.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    [SerializeField] AudioClip combatBgm;
    [SerializeField] AudioClip shopBgm;
    [SerializeField] AudioClip defaultBgm;
    [SerializeField] [Range(0f, 1f)] float bgmVolume = 0.6f;

    [Header("SFX")]
    [SerializeField] AudioClip buttonClickSfx;
    [SerializeField] AudioClip cardHoverSfx;
    [SerializeField] AudioClip cardGrabSfx;
    [SerializeField] AudioClip cardPlaySfx;
    [SerializeField] AudioClip cardCancelSfx;
    [SerializeField] AudioClip cardTargetSelectSfx;
    [SerializeField] [Range(0f, 1f)] float sfxVolume = 0.8f;

    AudioSource bgmSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
        EnsureSources();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayCardHover()
    {
        PlaySfx(cardHoverSfx);
    }

    public void PlayCardGrab()
    {
        PlaySfx(cardGrabSfx);
    }

    public void PlayCardPlay()
    {
        PlaySfx(cardPlaySfx);
    }

    public void PlayCardCancel()
    {
        PlaySfx(cardCancelSfx);
    }

    public void PlayCardTargetSelect()
    {
        PlaySfx(cardTargetSelectSfx);
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSources();
        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    void PlayForScene(string sceneName)
    {
        PlayBgm(ResolveClip(sceneName));
    }

    AudioClip ResolveClip(string sceneName)
    {
        if (sceneName == SceneNames.Combat)
            return combatBgm;

        if (sceneName == SceneNames.Reward)
        {
            MapNodeType nodeType = MapNodeType.Reward;
            if (GameManager.Instance != null && GameManager.Instance.Run != null)
                nodeType = GameManager.Instance.Run.Stages.SelectedNodeType;

            if (nodeType == MapNodeType.Shop || nodeType == MapNodeType.Upgrade)
                return shopBgm;
        }

        return defaultBgm;
    }

    void PlayBgm(AudioClip clip)
    {
        EnsureSources();
        if (bgmSource == null)
            return;

        bgmSource.volume = bgmVolume;

        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.Play();
    }

    void EnsureSources()
    {
        if (bgmSource == null)
            bgmSource = CreateSource("BgmSource", true, bgmVolume);
        if (sfxSource == null)
            sfxSource = CreateSource("SfxSource", false, sfxVolume);
    }

    AudioSource CreateSource(string childName, bool loop, float volume)
    {
        Transform existing = transform.Find(childName);
        AudioSource source = existing != null ? existing.GetComponent<AudioSource>() : null;
        if (source == null)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            source = go.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }
}
