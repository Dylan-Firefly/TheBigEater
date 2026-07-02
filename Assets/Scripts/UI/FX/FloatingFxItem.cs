using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingFxItem : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private RectTransform visualRoot;

    private RectTransform rectTransform;
    private Action<FloatingFxItem> onFinished;
    private Vector2 startPosition;
    private Vector2 velocity;
    private Vector2 acceleration;
    private float lifetime;
    private float elapsed;
    private float startScale;
    private float endScale;
    private float startRotation;
    private float rotationSpeed;
    private bool isPlaying;
    private RectTransform imageRectTransform;
    private RectTransform textRectTransform;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (visualRoot == null)
        {
            visualRoot = rectTransform;
        }

        if (image == null)
        {
            image = GetComponentInChildren<Image>(true);
        }

        if (image != null)
        {
            imageRectTransform = image.rectTransform;
        }

        if (textLabel == null)
        {
            textLabel = GetComponentInChildren<TMP_Text>(true);
        }

        if (textLabel != null)
        {
            textRectTransform = textLabel.rectTransform;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float t = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifetime);
        float easeOut = 1f - Mathf.Pow(1f - t, 2f);

        rectTransform.anchoredPosition = startPosition + velocity * elapsed + 0.5f * acceleration * elapsed * elapsed;

        if (visualRoot != null)
        {
            float scale = Mathf.Lerp(startScale, endScale, easeOut);
            visualRoot.localScale = Vector3.one * scale;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, startRotation + rotationSpeed * elapsed);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
        }

        if (elapsed >= lifetime)
        {
            Finish();
        }
    }

    public void Play(
        Sprite sprite,
        string text,
        Color color,
        Vector2 anchoredPosition,
        Vector2 initialVelocity,
        Vector2 itemAcceleration,
        float itemLifetime,
        float itemStartScale,
        float itemEndScale,
        Vector2 targetSize,
        bool useNativeSize,
        float itemRotationSpeed,
        Action<FloatingFxItem> finishedCallback)
    {
        onFinished = finishedCallback;
        startPosition = anchoredPosition;
        velocity = initialVelocity;
        acceleration = itemAcceleration;
        lifetime = Mathf.Max(0.01f, itemLifetime);
        elapsed = 0f;
        startScale = itemStartScale;
        endScale = itemEndScale;
        startRotation = UnityEngine.Random.Range(-12f, 12f);
        rotationSpeed = itemRotationSpeed;
        isPlaying = true;

        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.one;

        if (visualRoot != null)
        {
            visualRoot.localScale = Vector3.one * startScale;
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, startRotation);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        bool hasSprite = sprite != null;
        bool hasText = !hasSprite && !string.IsNullOrWhiteSpace(text);

        if (image != null)
        {
            image.gameObject.SetActive(hasSprite);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;

            if (hasSprite)
            {
                ApplyImageSize(targetSize, useNativeSize);
            }
        }

        if (textLabel != null)
        {
            textLabel.gameObject.SetActive(hasText);
            textLabel.text = text;
            textLabel.color = color;
            textLabel.raycastTarget = false;

            if (hasText)
            {
                ApplyTextSize(targetSize);
            }
        }

        gameObject.SetActive(true);
    }

    private void Finish()
    {
        isPlaying = false;
        gameObject.SetActive(false);
        onFinished?.Invoke(this);
    }

    private void ApplyImageSize(Vector2 targetSize, bool useNativeSize)
    {
        if (image == null)
        {
            return;
        }

        if (useNativeSize)
        {
            image.SetNativeSize();
            if (visualRoot != null && imageRectTransform != null)
            {
                visualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageRectTransform.rect.width);
                visualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, imageRectTransform.rect.height);
            }

            return;
        }

        ApplySize(targetSize);
    }

    private void ApplyTextSize(Vector2 targetSize)
    {
        ApplySize(targetSize);
    }

    private void ApplySize(Vector2 targetSize)
    {
        if (targetSize.x <= 0f || targetSize.y <= 0f)
        {
            return;
        }

        if (visualRoot != null)
        {
            visualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            visualRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
        }

        if (imageRectTransform != null)
        {
            imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
        }

        if (textRectTransform != null)
        {
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
        }
    }
}
