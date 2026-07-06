using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GetItemHintController : MonoBehaviour
{
    [Serializable]
    public class ItemHintEntry
    {
        public string itemId;
        public Sprite sprite;
    }

    [Header("UI")]
    [SerializeField] private Image hintImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Item Hint Data")]
    [SerializeField] private List<ItemHintEntry> itemHints = new List<ItemHintEntry>();

    [Header("Behavior")]
    [SerializeField, Min(0f)] private float defaultVisibleSeconds = 1.5f;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private bool resizeToSpriteRect = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool logMissingItem = true;

    [Header("Debug")]
    [SerializeField] private string debugItemId = "egg";

    private readonly Dictionary<string, ItemHintEntry> lookup = new Dictionary<string, ItemHintEntry>(StringComparer.OrdinalIgnoreCase);
    private Coroutine hideRoutine;

    public IReadOnlyList<ItemHintEntry> ItemHints => itemHints;

    private void Awake()
    {
        CacheLocalReferences();
        RebuildLookup();

        if (hideOnAwake)
        {
            HideImmediate();
        }
    }

    private void OnValidate()
    {
        CacheLocalReferences();
        RebuildLookup();
    }

    public void ShowItem(string itemId)
    {
        TryShowItem(itemId, defaultVisibleSeconds);
    }

    public bool TryShowItem(string itemId)
    {
        return TryShowItem(itemId, defaultVisibleSeconds);
    }

    public bool TryShowItem(string itemId, float visibleSeconds)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            if (logMissingItem)
            {
                Debug.LogWarning("[GetItemHintController] Item id is empty.", this);
            }

            return false;
        }

        RebuildLookup();

        if (!lookup.TryGetValue(itemId, out ItemHintEntry entry) || entry.sprite == null)
        {
            if (logMissingItem)
            {
                Debug.LogWarning($"[GetItemHintController] Missing item hint sprite for id '{itemId}'.", this);
            }

            return false;
        }

        ShowSprite(entry.sprite, visibleSeconds);
        return true;
    }

    public void ShowSprite(Sprite sprite)
    {
        ShowSprite(sprite, defaultVisibleSeconds);
    }

    public void ShowSprite(Sprite sprite, float visibleSeconds)
    {
        if (sprite == null)
        {
            if (logMissingItem)
            {
                Debug.LogWarning("[GetItemHintController] Cannot show a null sprite.", this);
            }

            return;
        }

        CacheLocalReferences();

        if (hintImage == null)
        {
            Debug.LogWarning("[GetItemHintController] Hint Image is not assigned.", this);
            return;
        }

        gameObject.SetActive(true);
        hintImage.sprite = sprite;
        hintImage.enabled = true;

        if (resizeToSpriteRect)
        {
            Rect spriteRect = sprite.rect;
            RectTransform imageRect = hintImage.rectTransform;
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteRect.width);
            imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteRect.height);
        }

        SetVisible(true);
        RestartAutoHide(visibleSeconds);
    }

    public void Hide()
    {
        StopAutoHide();
        HideImmediate();
    }

    [ContextMenu("Preview Debug Item")]
    private void PreviewDebugItem()
    {
        TryShowItem(debugItemId, defaultVisibleSeconds);
    }

    [ContextMenu("Hide")]
    private void ContextHide()
    {
        Hide();
    }

    private void RestartAutoHide(float visibleSeconds)
    {
        StopAutoHide();

        if (visibleSeconds > 0f)
        {
            hideRoutine = StartCoroutine(HideAfterDelay(visibleSeconds));
        }
    }

    private void StopAutoHide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        if (useUnscaledTime)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        hideRoutine = null;
        HideImmediate();
    }

    private void HideImmediate()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else if (hintImage != null)
        {
            hintImage.enabled = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void RebuildLookup()
    {
        lookup.Clear();

        foreach (ItemHintEntry entry in itemHints)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
            {
                continue;
            }

            lookup[entry.itemId] = entry;
        }
    }

    private void CacheLocalReferences()
    {
        if (hintImage == null)
        {
            hintImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
