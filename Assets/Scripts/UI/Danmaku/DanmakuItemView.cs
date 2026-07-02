using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuItemView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private RectTransform labelRect;

    private RectTransform rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            EnsureReferences();
            return rectTransform;
        }
    }

    private void Awake()
    {
        EnsureReferences();
    }

    public void ConfigureRuntime(
        Sprite backgroundSprite,
        Color backgroundColor,
        TMP_FontAsset fontAsset,
        int fontSize,
        Vector4 textPadding)
    {
        EnsureReferences();

        if (background != null)
        {
            background.sprite = backgroundSprite;
            background.color = backgroundColor;
            background.type = backgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.raycastTarget = false;
        }

        if (label != null)
        {
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Midline;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            if (fontAsset != null)
            {
                label.font = fontAsset;
            }
        }

        ApplyTextPadding(textPadding);
    }

    public float Bind(
        string message,
        Color textColor,
        float itemHeight,
        float minWidth,
        float maxWidth,
        float horizontalPadding)
    {
        EnsureReferences();

        if (label != null)
        {
            label.gameObject.SetActive(true);
            label.text = message;
            label.color = textColor;
            label.ForceMeshUpdate();
        }

        float preferredWidth = minWidth;
        if (label != null)
        {
            preferredWidth = label.GetPreferredValues(message, 0f, itemHeight).x + horizontalPadding * 2f;
        }

        float width = Mathf.Clamp(preferredWidth, minWidth, maxWidth);
        rectTransform.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Horizontal, width);
        rectTransform.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Vertical, itemHeight);
        return width;
    }

    public Vector2 Bind(
        Sprite sprite,
        string message,
        Color tintColor,
        Color textColor,
        float itemHeight,
        float minWidth,
        float maxWidth,
        float horizontalPadding,
        bool useNativeSize,
        Vector2 sizeOverride,
        float scale,
        Vector2 maxImageSize)
    {
        EnsureReferences();

        if (sprite != null)
        {
            return BindSprite(sprite, tintColor, itemHeight, useNativeSize, sizeOverride, scale, maxImageSize);
        }

        float width = Bind(message, textColor, itemHeight, minWidth, maxWidth, horizontalPadding);
        return new Vector2(width, itemHeight);
    }

    public void SetPosition(Vector2 anchoredPosition)
    {
        EnsureReferences();
        rectTransform.anchoredPosition = anchoredPosition;
    }

    public void SetAlpha(float alpha)
    {
        EnsureReferences();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    private void EnsureReferences()
    {
        if (rectTransform == null)
        {
            rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (background == null)
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }
        }

        if (label == null)
        {
            label = GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                GameObject labelObject = new GameObject("Text", typeof(RectTransform));
                labelObject.transform.SetParent(transform, false);
                label = labelObject.AddComponent<TextMeshProUGUI>();
            }
        }

        if (labelRect == null && label != null)
        {
            labelRect = label.rectTransform;
        }
    }

    private void ApplyTextPadding(Vector4 padding)
    {
        if (labelRect == null)
        {
            return;
        }

        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(padding.x, padding.y);
        labelRect.offsetMax = new Vector2(-padding.z, -padding.w);
    }

    private Vector2 BindSprite(
        Sprite sprite,
        Color tintColor,
        float fallbackHeight,
        bool useNativeSize,
        Vector2 sizeOverride,
        float scale,
        Vector2 maxImageSize)
    {
        if (label != null)
        {
            label.gameObject.SetActive(false);
        }

        if (background != null)
        {
            background.gameObject.SetActive(true);
            background.sprite = sprite;
            background.color = tintColor;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.raycastTarget = false;
        }

        Vector2 size = ResolveSpriteSize(sprite, fallbackHeight, useNativeSize, sizeOverride);
        size *= Mathf.Max(0.01f, scale);
        size = FitSize(size, maxImageSize);
        ApplyRootSize(size);
        return size;
    }

    private Vector2 ResolveSpriteSize(Sprite sprite, float fallbackHeight, bool useNativeSize, Vector2 sizeOverride)
    {
        if (sizeOverride.x > 0f && sizeOverride.y > 0f)
        {
            return sizeOverride;
        }

        if (useNativeSize && sprite != null)
        {
            float pixelsPerUnit = background != null ? background.pixelsPerUnit : 1f;
            if (pixelsPerUnit <= 0f)
            {
                pixelsPerUnit = 1f;
            }

            return sprite.rect.size / pixelsPerUnit;
        }

        float height = Mathf.Max(1f, fallbackHeight);
        float width = sprite != null && sprite.rect.height > 0f
            ? height * sprite.rect.width / sprite.rect.height
            : height;
        return new Vector2(width, height);
    }

    private Vector2 FitSize(Vector2 size, Vector2 maxSize)
    {
        if (maxSize.x <= 0f && maxSize.y <= 0f)
        {
            return size;
        }

        float scale = 1f;
        if (maxSize.x > 0f && size.x > maxSize.x)
        {
            scale = Mathf.Min(scale, maxSize.x / size.x);
        }

        if (maxSize.y > 0f && size.y > maxSize.y)
        {
            scale = Mathf.Min(scale, maxSize.y / size.y);
        }

        return size * scale;
    }

    private void ApplyRootSize(Vector2 size)
    {
        rectTransform.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Horizontal, size.x);
        rectTransform.SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Vertical, size.y);
    }
}
