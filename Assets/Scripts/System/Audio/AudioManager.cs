using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour
{
    public enum UiSound
    {
        GenericButton,
        MainMenu,
        Pop,
        PhoneOpen,
        PhoneButton,
        Back,
        Negative,
        Success
    }

    [Header("Source")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField, Min(1)] private int sourcePoolSize = 6;

    [Header("Mixer")]
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.85f;
    [SerializeField, Range(0.5f, 1.5f)] private float minPitch = 0.96f;
    [SerializeField, Range(0.5f, 1.5f)] private float maxPitch = 1.04f;

    private static AudioManager instance;
    private readonly Dictionary<UiSound, AudioClip> uiClipCache = new Dictionary<UiSound, AudioClip>();
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private int sourceIndex;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                CreateRuntimeInstance();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSourcePool();
    }

    public static void PlayUi(UiSound sound)
    {
        Instance.PlayUiSound(sound);
    }

    public static void PlayGenericButton()
    {
        PlayUi(UiSound.GenericButton);
    }

    public static void PlayMainMenu()
    {
        PlayUi(UiSound.MainMenu);
    }

    public static void PlayPop()
    {
        PlayUi(UiSound.Pop);
    }

    public static void PlayPhoneOpen()
    {
        PlayUi(UiSound.PhoneOpen);
    }

    public static void PlayPhoneButton()
    {
        PlayUi(UiSound.PhoneButton);
    }

    public static void PlayBack()
    {
        PlayUi(UiSound.Back);
    }

    public static void PlayNegative()
    {
        PlayUi(UiSound.Negative);
    }

    public static void PlaySuccess()
    {
        PlayUi(UiSound.Success);
    }

    public void PlayUiSound(UiSound sound)
    {
        EnsureSourcePool();
        AudioClip clip = GetUiClip(sound);
        AudioSource source = GetNextSource();
        if (clip == null || source == null)
        {
            return;
        }

        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip, uiVolume);
    }

    private AudioClip GetUiClip(UiSound sound)
    {
        if (uiClipCache.TryGetValue(sound, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(GetUiClipPath(sound));
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Missing UI sound: {sound}");
        }

        uiClipCache[sound] = clip;
        return clip;
    }

    private static string GetUiClipPath(UiSound sound)
    {
        const string root = "Audio/Free UI Click Sound Effects Pack/AUDIO/";
        switch (sound)
        {
            case UiSound.MainMenu:
                return root + "Button/SFX_UI_Button_Mouse_Huge_Generic_3";
            case UiSound.Pop:
                return root + "Pop/SFX_UI_Click_Designed_Pop_Generic_1";
            case UiSound.PhoneOpen:
                return root + "Pop/SFX_UI_Click_Designed_Pop_Open_1";
            case UiSound.PhoneButton:
                return root + "Wooden/SFX_UI_Click_Organic_Wooden_Thin_1";
            case UiSound.Back:
                return root + "Wooden/SFX_UI_Click_Organic_Wooden_Plastic_Negative_Back_1";
            case UiSound.Negative:
                return root + "Button/SFX_UI_Button_Organic_Plastic_Thin_Negative_1";
            case UiSound.Success:
                return root + "Crispy/SFX_UI_Click_Organic_Crispy_Pop_Generic_Open_1";
            default:
                return root + "Button/SFX_UI_Button_Organic_Plastic_Thin_Generic_1";
        }
    }

    private void EnsureSourcePool()
    {
        if (sfxSource == null)
        {
            sfxSource = CreateSfxSource();
        }

        if (!sfxSources.Contains(sfxSource))
        {
            sfxSources.Add(sfxSource);
        }

        int targetCount = Mathf.Max(1, sourcePoolSize);
        while (sfxSources.Count < targetCount)
        {
            sfxSources.Add(CreateSfxSource());
        }
    }

    private AudioSource GetNextSource()
    {
        if (sfxSources.Count == 0)
        {
            return null;
        }

        AudioSource source = sfxSources[sourceIndex];
        sourceIndex = (sourceIndex + 1) % sfxSources.Count;
        return source;
    }

    private AudioSource CreateSfxSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void CreateRuntimeInstance()
    {
        GameObject audioManagerObject = new GameObject("AudioManager");
        instance = audioManagerObject.AddComponent<AudioManager>();
    }
}
