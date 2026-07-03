using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum DanmakuTone
{
    Normal,
    Negative
}

public enum DanmakuRecycleReason
{
    Expired,
    Muted,
    Cleared
}

public class DanmakuFeedController : MonoBehaviour
{
    [System.Serializable]
    public class DanmakuMessage
    {
        public string id;
        public DanmakuTone tone = DanmakuTone.Normal;
        public Sprite sprite;
        public Color tintColor = Color.white;
        public bool useNativeSize = true;
        public Vector2 sizeOverride;
        [Min(0.01f)] public float scale = 1f;
        [TextArea(1, 2)] public string text;
        public bool enabled = true;
        public bool blocked;
        public Color textColor = Color.white;

        public bool IsNegative => tone == DanmakuTone.Negative;
    }

    private class ActiveDanmaku
    {
        public DanmakuMessage message;
        public DanmakuItemView view;
        public float height;
        public float currentY;
        public float targetY;
        public float alpha;
    }

    [Header("References")]
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private DanmakuItemView itemPrefab;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private TMP_FontAsset fontAsset;

    [Header("Messages")]
    [SerializeField] private bool filterBlockedMessages = true;
    [SerializeField] private bool loopMessages = true;
    [SerializeField] private List<DanmakuMessage> messages = new List<DanmakuMessage>
    {
        new DanmakuMessage { id = "demo_001", text = "\u4e3b\u64ad\u4e0b\u6b21\u53ef\u4ee5\u5403\u9a6c\u7ade\u5b9a\u98df\u5417" },
        new DanmakuMessage { id = "demo_002", text = "\u4e00\u53e3\u79d2\u4e86\u5c31\u5237\u5609\u5e74\u534e\uff01" },
        new DanmakuMessage { id = "demo_003", text = "\u6211\u8981\u770b\u4e09\u53e3\u4e00\u5934\u732a" },
        new DanmakuMessage { id = "demo_004", text = "\u4f60\u4eec\u90fd\u662f\u5047\u7c89\u4e1d\u6211\u548c\u4e3b\u64ad\u624d\u662f\u771f\u670b\u53cb" },
        new DanmakuMessage { id = "demo_005", text = "\u8fd9\u684c\u770b\u8d77\u6765\u4e5f\u592a\u9999\u4e86\u5427" },
        new DanmakuMessage { id = "demo_006", text = "\u4e3b\u64ad\u522b\u505c\uff0c\u7ee7\u7eed\u5403\uff01" }
    };

    [Header("Timing")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(1.15f, 1.85f);
    [SerializeField] private float firstSpawnDelay = 0.2f;
    [SerializeField] private int prewarmCount = 6;

    [Header("Layout")]
    [SerializeField] private int maxVisibleItems = 4;
    [SerializeField] private float itemHeight = 44f;
    [SerializeField] private float verticalSpacing = 14f;
    [SerializeField] private float leftPadding = 22f;
    [SerializeField] private float bottomPadding = 10f;
    [SerializeField] private float minItemWidth = 210f;
    [SerializeField] private float maxItemWidth = 500f;
    [SerializeField] private float horizontalTextPadding = 28f;
    [SerializeField] private Vector4 textPadding = new Vector4(22f, 4f, 22f, 4f);
    [SerializeField] private bool fitImagesToMaxSize = true;
    [SerializeField] private Vector2 maxImageSize = new Vector2(500f, 90f);
    [SerializeField] private float defaultImageScale = 1f;

    [Header("Motion")]
    [SerializeField] private float scrollSmoothTime = 0.16f;
    [SerializeField] private float spawnYOffset = -16f;
    [SerializeField] private float fadeSpeed = 7f;
    [SerializeField] private Color defaultBackgroundColor = new Color(0.72f, 0.33f, 0.95f, 0.88f);
    [SerializeField] private int fontSize = 24;

    private readonly Queue<DanmakuItemView> pool = new Queue<DanmakuItemView>();
    private readonly List<ActiveDanmaku> activeItems = new List<ActiveDanmaku>();
    private int nextMessageIndex;
    private float spawnTimer;
    private bool isPlaying;
    private bool isMovementPaused;

    public event Action<DanmakuItemView, DanmakuMessage> DanmakuSpawned;
    public event Action<DanmakuItemView, DanmakuMessage> NegativeDanmakuSpawned;
    public event Action<DanmakuItemView, DanmakuMessage, DanmakuRecycleReason> DanmakuRecycled;
    public event Action<DanmakuItemView, DanmakuMessage> DanmakuClicked;
    public event Action<DanmakuItemView, DanmakuMessage> DanmakuPointerEntered;
    public event Action<DanmakuItemView, DanmakuMessage> DanmakuPointerExited;

    private void Awake()
    {
        if (contentRoot == null)
        {
            contentRoot = transform as RectTransform;
        }

        DisableLayoutComponents();
        HideExistingStaticChildren();
        ConfigureContentRoot();
        Prewarm();

        spawnTimer = firstSpawnDelay;
        isPlaying = playOnAwake;
    }

    private void Update()
    {
        if (!isMovementPaused)
        {
            UpdateActiveItems();
        }

        if (!isPlaying)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            EmitNext();
            spawnTimer = UnityEngine.Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        }
    }

    [ContextMenu("Emit Next Danmaku")]
    public void EmitNext()
    {
        DanmakuMessage message = GetNextMessage();
        if (message == null)
        {
            return;
        }

        DanmakuItemView view = GetItem();
        view.ConfigureRuntime(backgroundSprite, defaultBackgroundColor, fontAsset, fontSize, textPadding);
        Vector2 itemSize = view.Bind(
            message.sprite,
            message.text,
            message.tintColor,
            message.textColor,
            itemHeight,
            minItemWidth,
            maxItemWidth,
            horizontalTextPadding,
            message.useNativeSize,
            message.sizeOverride,
            message.scale * defaultImageScale,
            fitImagesToMaxSize ? maxImageSize : Vector2.zero);
        view.SetVisible(true);
        view.SetAlpha(0f);
        view.BindMessage(message);

        ActiveDanmaku active = new ActiveDanmaku
        {
            message = message,
            view = view,
            height = itemSize.y,
            currentY = bottomPadding + spawnYOffset,
            targetY = bottomPadding,
            alpha = 0f
        };

        activeItems.Insert(0, active);
        RefreshTargets();
        view.SetPosition(new Vector2(leftPadding, active.currentY));
        TrimOverflow();
        DanmakuSpawned?.Invoke(view, message);
        if (message.IsNegative)
        {
            NegativeDanmakuSpawned?.Invoke(view, message);
        }
    }

    public void Play()
    {
        isPlaying = true;
        isMovementPaused = false;
    }

    public void Pause(bool pauseMovement = false)
    {
        isPlaying = false;
        isMovementPaused = pauseMovement;
    }

    public void SetMovementPaused(bool paused)
    {
        isMovementPaused = paused;
    }

    public void AddMessage(string id, string text)
    {
        if (IsBlank(text))
        {
            return;
        }

        messages.Add(new DanmakuMessage
        {
            id = id,
            text = text,
            tone = DanmakuTone.Normal,
            enabled = true,
            blocked = false,
            textColor = Color.white
        });
    }

    public void AddMessage(string id, Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        messages.Add(new DanmakuMessage
        {
            id = id,
            sprite = sprite,
            tone = DanmakuTone.Normal,
            tintColor = Color.white,
            useNativeSize = true,
            scale = 1f,
            enabled = true,
            blocked = false
        });
    }

    public void SetMessageBlocked(string id, bool blocked, bool removeVisibleItems = true)
    {
        if (IsBlank(id))
        {
            return;
        }

        for (int i = 0; i < messages.Count; i++)
        {
            if (messages[i] != null && messages[i].id == id)
            {
                messages[i].blocked = blocked;
            }
        }

        if (removeVisibleItems)
        {
            RemoveVisibleItemsById(id, DanmakuRecycleReason.Muted);
        }
    }

    public void RemoveVisibleDanmaku(DanmakuItemView view, DanmakuRecycleReason reason = DanmakuRecycleReason.Muted)
    {
        if (view == null)
        {
            return;
        }

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i].view == view)
            {
                ActiveDanmaku active = activeItems[i];
                activeItems.RemoveAt(i);
                Recycle(active, reason);
                RefreshTargets();
                return;
            }
        }
    }

    public void ClearVisible()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            Recycle(activeItems[i], DanmakuRecycleReason.Cleared);
        }

        activeItems.Clear();
    }

    private DanmakuMessage GetNextMessage()
    {
        if (messages.Count == 0)
        {
            return null;
        }

        int attempts = 0;
        while (attempts < messages.Count)
        {
            if (nextMessageIndex >= messages.Count)
            {
                if (!loopMessages)
                {
                    return null;
                }

                nextMessageIndex = 0;
            }

            DanmakuMessage message = messages[nextMessageIndex];
            nextMessageIndex++;
            attempts++;

            if (CanUseMessage(message))
            {
                return message;
            }
        }

        return null;
    }

    private bool CanUseMessage(DanmakuMessage message)
    {
        if (message == null || !message.enabled)
        {
            return false;
        }

        if (message.sprite == null && IsBlank(message.text))
        {
            return false;
        }

        return !filterBlockedMessages || !message.blocked;
    }

    private void UpdateActiveItems()
    {
        float lerpSpeed = scrollSmoothTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / scrollSmoothTime);

        for (int i = 0; i < activeItems.Count; i++)
        {
            ActiveDanmaku active = activeItems[i];
            active.currentY = Mathf.Lerp(active.currentY, active.targetY, lerpSpeed);
            active.alpha = Mathf.MoveTowards(active.alpha, 1f, fadeSpeed * Time.deltaTime);
            active.view.SetPosition(new Vector2(leftPadding, active.currentY));
            active.view.SetAlpha(active.alpha);
        }
    }

    private void TrimOverflow()
    {
        while (activeItems.Count > maxVisibleItems)
        {
            ActiveDanmaku oldest = activeItems[activeItems.Count - 1];
            activeItems.RemoveAt(activeItems.Count - 1);
            Recycle(oldest, DanmakuRecycleReason.Expired);
        }
    }

    private void RemoveVisibleItemsById(string id, DanmakuRecycleReason reason)
    {
        bool removedAny = false;
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i].message != null && activeItems[i].message.id == id)
            {
                Recycle(activeItems[i], reason);
                activeItems.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny)
        {
            RefreshTargets();
        }
    }

    private void RefreshTargets()
    {
        float y = bottomPadding;
        for (int i = 0; i < activeItems.Count; i++)
        {
            activeItems[i].targetY = y;
            y += activeItems[i].height + verticalSpacing;
        }
    }

    private void Prewarm()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = pool.Count; i < prewarmCount; i++)
        {
            DanmakuItemView item = CreateItem();
            item.SetVisible(false);
            pool.Enqueue(item);
        }
    }

    private DanmakuItemView GetItem()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        return CreateItem();
    }

    private DanmakuItemView CreateItem()
    {
        DanmakuItemView item;
        if (itemPrefab != null)
        {
            item = Instantiate(itemPrefab, contentRoot);
        }
        else
        {
            GameObject itemObject = new GameObject("DanmakuItem", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            itemObject.transform.SetParent(contentRoot, false);
            item = itemObject.AddComponent<DanmakuItemView>();
        }

        item.PointerEntered += HandleItemPointerEntered;
        item.PointerExited += HandleItemPointerExited;
        item.Clicked += HandleItemClicked;
        item.ConfigureRuntime(backgroundSprite, defaultBackgroundColor, fontAsset, fontSize, textPadding);
        item.RectTransform.anchorMin = new Vector2(0f, 0f);
        item.RectTransform.anchorMax = new Vector2(0f, 0f);
        item.RectTransform.pivot = new Vector2(0f, 0f);
        return item;
    }

    private void Recycle(ActiveDanmaku active, DanmakuRecycleReason reason)
    {
        if (active == null || active.view == null)
        {
            return;
        }

        DanmakuMessage message = active.message;
        DanmakuRecycled?.Invoke(active.view, message, reason);
        active.view.ClearBinding();
        active.view.SetVisible(false);
        pool.Enqueue(active.view);
    }

    private void HandleItemPointerEntered(DanmakuItemView item)
    {
        if (item != null && item.CurrentMessage != null)
        {
            DanmakuPointerEntered?.Invoke(item, item.CurrentMessage);
        }
    }

    private void HandleItemPointerExited(DanmakuItemView item)
    {
        if (item != null && item.CurrentMessage != null)
        {
            DanmakuPointerExited?.Invoke(item, item.CurrentMessage);
        }
    }

    private void HandleItemClicked(DanmakuItemView item)
    {
        if (item != null && item.CurrentMessage != null)
        {
            DanmakuClicked?.Invoke(item, item.CurrentMessage);
        }
    }

    private void ConfigureContentRoot()
    {
        if (contentRoot == null)
        {
            return;
        }

        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.pivot = Vector2.zero;
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;
    }

    private void DisableLayoutComponents()
    {
        if (contentRoot == null)
        {
            return;
        }

        LayoutGroup layoutGroup = contentRoot.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter sizeFitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.enabled = false;
        }
    }

    private void HideExistingStaticChildren()
    {
        if (contentRoot == null)
        {
            return;
        }

        for (int i = 0; i < contentRoot.childCount; i++)
        {
            contentRoot.GetChild(i).gameObject.SetActive(false);
        }
    }

    private static bool IsBlank(string value)
    {
        return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
    }
}
