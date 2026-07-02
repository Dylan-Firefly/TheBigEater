using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFountainFxEmitter : MonoBehaviour
{
    [System.Serializable]
    public class FountainFxEntry
    {
        public string name;
        [Min(0)] public int weight = 10;
        public Sprite sprite;
        public Color color = Color.white;
        public bool useNativeSize;
        public Vector2 targetSize = new Vector2(42f, 42f);
        public Vector2 startScaleRange = new Vector2(0.8f, 1.05f);
        public Vector2 endScaleRange = new Vector2(1f, 1.25f);
        public Vector2 lifetimeRange = new Vector2(0.55f, 0.9f);
        public Vector2 rotationSpeedRange = new Vector2(-20f, 20f);

        public bool HasVisual => sprite != null;
    }

    [Header("References")]
    [SerializeField] private RectTransform fxParent;
    [SerializeField] private FloatingFxItem itemPrefab;
    [SerializeField] private Button triggerButton;
    [SerializeField] private bool bindButtonOnAwake;

    [Header("Emission")]
    [SerializeField] private int prewarmCount = 24;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loopAutomatically = true;
    [SerializeField] private bool continuousEmission = true;
    [SerializeField] private Vector2 autoSprayIntervalRange = new Vector2(1.1f, 1.8f);
    [SerializeField] private float firstAutoSprayDelay = 0.15f;
    [SerializeField] private bool sprayOnClick = true;
    [SerializeField] private float sprayDuration = 0.28f;
    [SerializeField] private float particlesPerSecond = 34f;
    [SerializeField] private int minBurstCount = 5;
    [SerializeField] private int maxBurstCount = 9;
    [SerializeField] private Vector2 spawnJitter = new Vector2(20f, 8f);

    [Header("Motion")]
    [SerializeField] private Vector2 velocityXRange = new Vector2(-55f, 55f);
    [SerializeField] private Vector2 velocityYRange = new Vector2(160f, 285f);
    [SerializeField] private Vector2 acceleration = new Vector2(0f, -135f);

    [Header("Weighted Pool")]
    [SerializeField] private List<FountainFxEntry> entries = new List<FountainFxEntry>();

    private readonly Queue<FloatingFxItem> pool = new Queue<FloatingFxItem>();
    private Canvas parentCanvas;
    private float sprayTimer;
    private float autoSprayTimer;
    private float emissionAccumulator;
    private bool isPlaying;

    private void Awake()
    {
        if (fxParent == null)
        {
            fxParent = transform.parent as RectTransform;
        }

        if (triggerButton == null)
        {
            triggerButton = GetComponent<Button>();
        }

        if (fxParent != null)
        {
            parentCanvas = fxParent.GetComponentInParent<Canvas>();
        }

        Prewarm();
        isPlaying = playOnAwake;
        autoSprayTimer = Mathf.Max(0f, firstAutoSprayDelay);

        if (bindButtonOnAwake && triggerButton != null)
        {
            triggerButton.onClick.AddListener(Play);
        }
    }

    private void OnDestroy()
    {
        if (bindButtonOnAwake && triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(Play);
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        if (continuousEmission)
        {
            EmitOverTime();
            return;
        }

        UpdateAutoSpray();
        if (sprayTimer <= 0f)
        {
            return;
        }

        EmitOverTime();
    }

    public void StartAuto()
    {
        isPlaying = true;
    }

    public void StopAuto(bool clearCurrentSpray = false)
    {
        isPlaying = false;
        if (clearCurrentSpray)
        {
            sprayTimer = 0f;
            emissionAccumulator = 0f;
        }
    }

    public void Play()
    {
        if (sprayOnClick)
        {
            EmitSpray();
            return;
        }

        EmitBurst();
    }

    public void EmitSpray()
    {
        sprayTimer = Mathf.Max(0.01f, sprayDuration);
        emissionAccumulator = 0f;
    }

    private void UpdateAutoSpray()
    {
        if (!loopAutomatically)
        {
            return;
        }

        autoSprayTimer -= Time.deltaTime;
        if (autoSprayTimer > 0f)
        {
            return;
        }

        EmitSpray();
        autoSprayTimer = Random.Range(autoSprayIntervalRange.x, autoSprayIntervalRange.y);
    }

    private void EmitOverTime()
    {
        emissionAccumulator += Mathf.Max(0f, particlesPerSecond) * Time.deltaTime;

        Vector2 origin = GetEmitterPositionInFxParent();
        while (emissionAccumulator >= 1f)
        {
            EmitOne(origin);
            emissionAccumulator -= 1f;
        }

        if (!continuousEmission)
        {
            sprayTimer -= Time.deltaTime;
        }
    }

    public void EmitBurst()
    {
        Vector2 origin = GetEmitterPositionInFxParent();
        int count = Random.Range(minBurstCount, maxBurstCount + 1);
        for (int i = 0; i < count; i++)
        {
            EmitOne(origin);
        }
    }

    public void EmitAtScreenPosition(Vector2 screenPosition)
    {
        if (fxParent == null)
        {
            return;
        }

        Camera camera = GetCanvasCamera();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fxParent, screenPosition, camera, out Vector2 localPoint);
        EmitBurst(localPoint);
    }

    public void EmitBurst(Vector2 localOrigin)
    {
        int count = Random.Range(minBurstCount, maxBurstCount + 1);
        for (int i = 0; i < count; i++)
        {
            EmitOne(localOrigin);
        }
    }

    private void EmitOne(Vector2 localOrigin)
    {
        if (fxParent == null)
        {
            return;
        }

        FountainFxEntry entry = PickEntry();
        if (entry == null)
        {
            return;
        }

        FloatingFxItem item = GetItem();
        Vector2 position = localOrigin + new Vector2(
            Random.Range(-spawnJitter.x, spawnJitter.x),
            Random.Range(-spawnJitter.y, spawnJitter.y));

        Vector2 velocity = new Vector2(
            Random.Range(velocityXRange.x, velocityXRange.y),
            Random.Range(velocityYRange.x, velocityYRange.y));

        float lifetime = Random.Range(entry.lifetimeRange.x, entry.lifetimeRange.y);
        float startScale = Random.Range(entry.startScaleRange.x, entry.startScaleRange.y);
        float endScale = Random.Range(entry.endScaleRange.x, entry.endScaleRange.y);
        float rotationSpeed = Random.Range(entry.rotationSpeedRange.x, entry.rotationSpeedRange.y);

        item.Play(
            entry.sprite,
            null,
            entry.color,
            position,
            velocity,
            acceleration,
            lifetime,
            startScale,
            endScale,
            entry.targetSize,
            entry.useNativeSize,
            rotationSpeed,
            ReturnToPool);
    }

    private FountainFxEntry PickEntry()
    {
        int totalWeight = 0;
        foreach (FountainFxEntry entry in entries)
        {
            if (entry != null && entry.weight > 0 && entry.HasVisual)
            {
                totalWeight += entry.weight;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        foreach (FountainFxEntry entry in entries)
        {
            if (entry == null || entry.weight <= 0 || !entry.HasVisual)
            {
                continue;
            }

            if (roll < entry.weight)
            {
                return entry;
            }

            roll -= entry.weight;
        }

        return null;
    }

    private void Prewarm()
    {
        if (fxParent == null)
        {
            return;
        }

        for (int i = pool.Count; i < prewarmCount; i++)
        {
            FloatingFxItem item = CreateItem();
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
    }

    private FloatingFxItem GetItem()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        FloatingFxItem item = CreateItem();
        item.gameObject.SetActive(false);
        return item;
    }

    private FloatingFxItem CreateItem()
    {
        if (itemPrefab != null)
        {
            return Instantiate(itemPrefab, fxParent);
        }

        GameObject itemObject = new GameObject("UIFountainFxItem", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        itemObject.transform.SetParent(fxParent, false);
        return itemObject.AddComponent<FloatingFxItem>();
    }

    private void ReturnToPool(FloatingFxItem item)
    {
        pool.Enqueue(item);
    }

    private Vector2 GetEmitterPositionInFxParent()
    {
        if (fxParent == null)
        {
            return Vector2.zero;
        }

        RectTransform emitterRect = transform as RectTransform;
        if (emitterRect == null)
        {
            return Vector2.zero;
        }

        Camera camera = GetCanvasCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, emitterRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(fxParent, screenPoint, camera, out Vector2 localPoint);
        return localPoint;
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas == null)
        {
            parentCanvas = fxParent != null ? fxParent.GetComponentInParent<Canvas>() : null;
        }

        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return parentCanvas.worldCamera;
    }
}
