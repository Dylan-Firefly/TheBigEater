using System;
using UnityEngine;
using UnityEngine.UI;

public class PhoneCommonPageScrollTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private GameObject resultPrompt;

    [Header("Trigger")]
    [SerializeField, Range(0f, 1f)] private float bottomThreshold = 0.12f;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool resetScrollToTop = true;
    [SerializeField] private bool hidePromptOnReset = true;

    private bool hasTriggered;

    public bool HasTriggered => hasTriggered;
    public event Action ResultPromptUnlocked;

    private void OnEnable()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.AddListener(HandleScrollChanged);
        }
    }

    private void OnDisable()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;

        if (hidePromptOnReset)
        {
            SetActive(resultPrompt, false);
        }

        if (scrollRect != null && resetScrollToTop)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ForceUnlock()
    {
        UnlockResultPrompt();
    }

    public void ValidateReferences(UnityEngine.Object owner)
    {
        if (scrollRect == null)
        {
            Debug.LogWarning($"[{nameof(PhoneCommonPageScrollTrigger)}] Missing reference: scrollRect.", owner);
        }

        if (resultPrompt == null)
        {
            Debug.LogWarning($"[{nameof(PhoneCommonPageScrollTrigger)}] Missing reference: resultPrompt.", owner);
        }
    }

    private void HandleScrollChanged(Vector2 normalizedPosition)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (scrollRect != null && scrollRect.verticalNormalizedPosition <= bottomThreshold)
        {
            UnlockResultPrompt();
        }
    }

    private void UnlockResultPrompt()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        hasTriggered = true;
        SetActive(resultPrompt, true);
        ResultPromptUnlocked?.Invoke();
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
