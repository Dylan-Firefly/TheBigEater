using System.Collections.Generic;
using UnityEngine;

public class FloatingFxEmitter : MonoBehaviour
{
    [System.Serializable]
    public class FloatingFxEntry
    {
        public string name;
        [Min(0)] public int weight = 10;
        public Sprite sprite;
        public string text;
        public Color color = Color.white;
        public bool useNativeSize;
        public Vector2 targetSize = new Vector2(64f, 64f);
        public Vector2 startScaleRange = new Vector2(0.8f, 1.2f);
        public Vector2 endScaleRange = new Vector2(0.9f, 1.1f);
        public Vector2 lifetimeRange = new Vector2(0.45f, 0.8f);
        public Vector2 velocityXRange = new Vector2(-160f, 160f);
        public Vector2 velocityYRange = new Vector2(140f, 300f);
        public Vector2 rotationSpeedRange = new Vector2(-45f, 45f);
        public bool useSideSpread = true;
        public Vector2 spawnSideOffsetRange = new Vector2(0f, 45f);

        public bool HasVisual => sprite != null || !string.IsNullOrWhiteSpace(text);
    }

    [Header("References")]
    [SerializeField] private RectTransform fxParent;
    [SerializeField] private FloatingFxItem itemPrefab;

    [Header("Burst")]
    [SerializeField] private int prewarmCount = 24;
    [SerializeField] private int minBurstCount = 6;
    [SerializeField] private int maxBurstCount = 10;
    [SerializeField] private Vector2 spawnJitter = new Vector2(90f, 40f);
    [SerializeField] private Vector2 acceleration = new Vector2(0f, -100f);

    [Header("Weighted Pool")]
    [SerializeField] private List<FloatingFxEntry> entries = new List<FloatingFxEntry>();

    private readonly Queue<FloatingFxItem> pool = new Queue<FloatingFxItem>();
    private Canvas parentCanvas;

    private void Awake()
    {
        if (fxParent == null)
        {
            fxParent = transform.parent as RectTransform;
        }

        if (fxParent != null)
        {
            parentCanvas = fxParent.GetComponentInParent<Canvas>();
        }

        Prewarm();
    }

    public void EmitBurst()
    {
        Vector2 origin = GetEmitterPositionInFxParent();
        EmitBurst(origin);
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
        if (itemPrefab == null || fxParent == null)
        {
            return;
        }

        int count = Random.Range(minBurstCount, maxBurstCount + 1);
        for (int i = 0; i < count; i++)
        {
            EmitOne(localOrigin);
        }
    }

    private void EmitOne(Vector2 localOrigin)
    {
        FloatingFxEntry entry = PickEntry();
        if (entry == null)
        {
            return;
        }

        FloatingFxItem item = GetItem();
        Vector2 position;
        Vector2 velocity;
        if (entry.useSideSpread)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            float sideOffset = Random.Range(entry.spawnSideOffsetRange.x, entry.spawnSideOffsetRange.y);
            float sideSpeed = Mathf.Abs(Random.Range(entry.velocityXRange.x, entry.velocityXRange.y));

            position = localOrigin + new Vector2(
                side * sideOffset + Random.Range(-spawnJitter.x, spawnJitter.x),
                Random.Range(-spawnJitter.y, spawnJitter.y));

            velocity = new Vector2(
                side * sideSpeed,
                Random.Range(entry.velocityYRange.x, entry.velocityYRange.y));
        }
        else
        {
            position = localOrigin + new Vector2(
                Random.Range(-spawnJitter.x, spawnJitter.x),
                Random.Range(-spawnJitter.y, spawnJitter.y));

            velocity = new Vector2(
                Random.Range(entry.velocityXRange.x, entry.velocityXRange.y),
                Random.Range(entry.velocityYRange.x, entry.velocityYRange.y));
        }

        float lifetime = Random.Range(entry.lifetimeRange.x, entry.lifetimeRange.y);
        float startScale = Random.Range(entry.startScaleRange.x, entry.startScaleRange.y);
        float endScale = Random.Range(entry.endScaleRange.x, entry.endScaleRange.y);
        float rotationSpeed = Random.Range(entry.rotationSpeedRange.x, entry.rotationSpeedRange.y);

        item.Play(
            entry.sprite,
            entry.text,
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

    private FloatingFxEntry PickEntry()
    {
        int totalWeight = 0;
        foreach (FloatingFxEntry entry in entries)
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
        foreach (FloatingFxEntry entry in entries)
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
        if (itemPrefab == null || fxParent == null)
        {
            return;
        }

        for (int i = pool.Count; i < prewarmCount; i++)
        {
            FloatingFxItem item = Instantiate(itemPrefab, fxParent);
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

        FloatingFxItem item = Instantiate(itemPrefab, fxParent);
        item.gameObject.SetActive(false);
        return item;
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
