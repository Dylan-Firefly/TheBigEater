using UnityEngine;

[DisallowMultipleComponent]
public class CursorManager : MonoBehaviour
{
    private const string DefaultCursorResourcePath = "Cursor/DefaultCursor";
    private static readonly Vector2 DefaultHotspot = new Vector2(34f, 4f);

    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 hotspot = new Vector2(34f, 4f);
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    private static CursorManager instance;
    private static Texture2D cachedDefaultCursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyDefaultCursorOnLoad()
    {
        ApplyDefaultCursor();
    }

    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (applyOnAwake)
        {
            ApplyCursor();
        }
    }

    public void ApplyCursor()
    {
        Texture2D texture = cursorTexture != null ? cursorTexture : LoadDefaultCursor();
        if (texture == null)
        {
            return;
        }

        Cursor.SetCursor(texture, hotspot, cursorMode);
    }

    public static void ApplyDefaultCursor()
    {
        Texture2D texture = LoadDefaultCursor();
        if (texture != null)
        {
            Cursor.SetCursor(texture, DefaultHotspot, CursorMode.Auto);
        }
    }

    public static void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static Texture2D LoadDefaultCursor()
    {
        if (cachedDefaultCursor == null)
        {
            cachedDefaultCursor = Resources.Load<Texture2D>(DefaultCursorResourcePath);
            if (cachedDefaultCursor == null)
            {
                Debug.LogWarning($"[CursorManager] Missing cursor texture at Resources/{DefaultCursorResourcePath}.");
            }
        }

        return cachedDefaultCursor;
    }
}
