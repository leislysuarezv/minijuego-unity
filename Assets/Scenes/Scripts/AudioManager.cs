using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [SerializeField] private AudioCatalog audioCatalog;
    [SerializeField] [Range(0f, 1f)] private float startVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float paintingLoopVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float paintingSecondaryVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float transitionVolume = 1f;

    private AudioSource startSource;
    private AudioSource paintingLoopSource;
    private AudioSource paintingSecondarySource;
    private AudioSource transitionSource;
    private int lastStartSoundSceneHandle = -1;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();

                if (instance == null)
                {
                    GameObject audioManagerObject = new GameObject(nameof(AudioManager));
                    instance = audioManagerObject.AddComponent<AudioManager>();
                }
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        Instance.Initialize();
    }

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CursorInputRouter.Instance.Pressed += HandlePaintingStarted;
        CursorInputRouter.Instance.Released += HandlePaintingStopped;
        ScoreManager.PhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (CursorInputRouter.HasInstance)
        {
            CursorInputRouter.Instance.Pressed -= HandlePaintingStarted;
            CursorInputRouter.Instance.Released -= HandlePaintingStopped;
        }

        ScoreManager.PhaseChanged -= HandlePhaseChanged;
    }

    public void PlayFinishSound()
    {
        PlayTransitionClip(audioCatalog != null ? audioCatalog.finishClip : null);
    }

    public void PlayResultsSound()
    {
        PlayTransitionClip(audioCatalog != null ? audioCatalog.resultsClip : null);
    }

    public void PlayStartSoundForCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid())
        {
            return;
        }

        PlayStartSound(activeScene.handle);
    }

    public void StopPaintingAudio()
    {
        if (paintingLoopSource != null)
            paintingLoopSource.Stop();

        if (paintingSecondarySource != null)
            paintingSecondarySource.Stop();
    }

    public void StopStartSound()
    {
        if (startSource != null)
            startSource.Stop();
    }

    private void Initialize()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioCatalog == null)
        {
            audioCatalog = Resources.Load<AudioCatalog>("AudioCatalog");
        }

        EnsureAudioSources();

        if (audioCatalog == null)
        {
            Debug.LogWarning("AudioManager could not load Resources/AudioCatalog. Transition sounds will stay silent until the catalog is assigned.");
            return;
        }

        if (audioCatalog.paintingLoopClip == null)
        {
            Debug.LogWarning("AudioManager is missing the painting loop clip. Add loop.mp3 to the AudioCatalog asset to enable the held painting loop.");
        }

        if (audioCatalog.startClip == null)
        {
            Debug.LogWarning("AudioManager is missing the start clip. Add inicio.mp3 to the AudioCatalog asset to enable the intro sound.");
        }
    }

    private void EnsureAudioSources()
    {
        if (startSource == null)
        {
            startSource = CreateConfiguredSource("StartSource", false, startVolume);
        }

        if (paintingLoopSource == null)
        {
            paintingLoopSource = CreateConfiguredSource("PaintingLoopSource", true, paintingLoopVolume);
        }

        if (paintingSecondarySource == null)
        {
            paintingSecondarySource = CreateConfiguredSource("PaintingSecondarySource", false, paintingSecondaryVolume);
        }

        if (transitionSource == null)
        {
            transitionSource = CreateConfiguredSource("TransitionSource", false, transitionVolume);
        }
    }

    private AudioSource CreateConfiguredSource(string sourceName, bool shouldLoop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource audioSource = sourceObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = shouldLoop;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        if (scene.handle != SceneManager.GetActiveScene().handle)
        {
            return;
        }

        // Scene initialization is the intro cue entry point, before any pointer press.
        PlayStartSound(scene.handle);
    }

    private void HandlePaintingStarted(Vector3 _)
    {
        if (ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting || audioCatalog == null)
        {
            return;
        }

        // Stop the intro if the player begins interacting before it fully finishes.
        StopStartSound();

        // The main paint layer is the only looped sound in the system.
        if (audioCatalog.paintingLoopClip != null && !paintingLoopSource.isPlaying)
        {
            paintingLoopSource.clip = audioCatalog.paintingLoopClip;
            paintingLoopSource.Play();
        }

        // The secondary paint clip starts with the gesture but does not loop.
        if (audioCatalog.paintingSecondaryClip != null)
        {
            paintingSecondarySource.clip = audioCatalog.paintingSecondaryClip;
            paintingSecondarySource.time = 0f;
            paintingSecondarySource.Play();
        }
    }

    private void HandlePaintingStopped(Vector3 _)
    {
        StopPaintingAudio();
    }

    private void HandlePhaseChanged(ScoreManager.GamePhase newPhase)
    {
        if (newPhase != ScoreManager.GamePhase.Painting)
        {
            StopStartSound();
            StopPaintingAudio();
        }
    }

    private void PlayStartSound(int sceneHandle)
    {
        if (sceneHandle == lastStartSoundSceneHandle)
        {
            return;
        }

        lastStartSoundSceneHandle = sceneHandle;

        if (audioCatalog == null || audioCatalog.startClip == null || startSource == null)
        {
            return;
        }

        // PlayOneShot keeps the intro isolated from looped paint audio and one-off transitions.
        startSource.Stop();
        startSource.PlayOneShot(audioCatalog.startClip);
    }

    private void PlayTransitionClip(AudioClip clip)
    {
        if (clip == null || transitionSource == null)
        {
            return;
        }

        // Reusing one transition source prevents duplicated finish / results playback.
        transitionSource.Stop();
        transitionSource.clip = clip;
        transitionSource.Play();
    }
}

