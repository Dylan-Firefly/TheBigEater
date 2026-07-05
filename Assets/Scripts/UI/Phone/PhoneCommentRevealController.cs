using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneCommentRevealController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform commentListRoot;
    [SerializeField] private List<GameObject> commentItems = new List<GameObject>();

    [Header("Reveal")]
    [SerializeField] private float revealInterval = 0.45f;
    [SerializeField] private float revealDuration = 0.18f;
    [SerializeField] private Vector2 revealOffset = new Vector2(42f, 0f);
    [SerializeField] private bool useUnscaledTime = true;

    private readonly Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    private Coroutine revealRoutine;

    public bool IsRevealing { get; private set; }
    public float TotalRevealDuration => commentItems.Count == 0
        ? 0f
        : (commentItems.Count - 1) * Mathf.Max(0.01f, revealInterval) + Mathf.Max(0.01f, revealDuration);

    public event Action RevealCompleted;

    private void Awake()
    {
        PrepareItems();
        SetItemsVisible(false);
    }

    public void PrepareItems()
    {
        if (commentItems.Count == 0)
        {
            BindCommentItemsFromRoot();
        }

        StoreOriginalPositions();
    }

    public void HideAll()
    {
        StopReveal();
        SetItemsVisible(false);
    }

    public void StartReveal()
    {
        StopReveal();
        SetItemsVisible(false);
        IsRevealing = true;
        revealRoutine = StartCoroutine(RevealRoutine());
    }

    public void StopReveal()
    {
        if (revealRoutine != null)
        {
            StopAllCoroutines();
            revealRoutine = null;
        }

        IsRevealing = false;
    }

    private IEnumerator RevealRoutine()
    {
        for (int i = 0; i < commentItems.Count; i++)
        {
            StartCoroutine(RevealItem(commentItems[i]));
            if (i < commentItems.Count - 1)
            {
                yield return WaitSeconds(Mathf.Max(0.01f, revealInterval));
            }
        }

        if (commentItems.Count > 0)
        {
            yield return WaitSeconds(Mathf.Max(0.01f, revealDuration));
        }

        revealRoutine = null;
        IsRevealing = false;
        RevealCompleted?.Invoke();
    }

    private IEnumerator RevealItem(GameObject item)
    {
        if (item == null)
        {
            yield break;
        }

        RectTransform rect = item.GetComponent<RectTransform>();
        CanvasGroup group = item.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = item.AddComponent<CanvasGroup>();
        }

        Vector2 endPosition = rect != null && originalPositions.TryGetValue(rect, out Vector2 cachedPosition)
            ? cachedPosition
            : Vector2.zero;
        Vector2 startPosition = endPosition + revealOffset;

        group.alpha = 0f;
        item.SetActive(true);

        if (rect != null)
        {
            rect.anchoredPosition = startPosition;
        }

        float duration = Mathf.Max(0.01f, revealDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Smooth(Mathf.Clamp01(elapsed / duration));
            group.alpha = t;

            if (rect != null)
            {
                rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            }

            yield return null;
        }

        group.alpha = 1f;
        if (rect != null)
        {
            rect.anchoredPosition = endPosition;
        }
    }

    private void BindCommentItemsFromRoot()
    {
        if (commentListRoot == null)
        {
            return;
        }

        for (int i = 0; i < commentListRoot.childCount; i++)
        {
            Transform child = commentListRoot.GetChild(i);
            if (child != null)
            {
                commentItems.Add(child.gameObject);
            }
        }
    }

    private void StoreOriginalPositions()
    {
        originalPositions.Clear();
        foreach (GameObject item in commentItems)
        {
            if (item == null)
            {
                continue;
            }

            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null && !originalPositions.ContainsKey(rect))
            {
                originalPositions.Add(rect, rect.anchoredPosition);
            }
        }
    }

    private void SetItemsVisible(bool visible)
    {
        foreach (GameObject item in commentItems)
        {
            if (item == null)
            {
                continue;
            }

            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null && originalPositions.TryGetValue(rect, out Vector2 originalPosition))
            {
                rect.anchoredPosition = originalPosition;
            }

            CanvasGroup group = item.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = visible ? 1f : 0f;
            }

            item.SetActive(visible);
        }
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
