using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuModerationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DanmakuFeedController feedController;
    [SerializeField] private DanmakuModerationPopup moderationPopup;

    [Header("First Negative Guide")]
    [SerializeField] private GameObject firstNegativePromptRoot;
    [SerializeField] private Button firstNegativeConfirmButton;
    [SerializeField] private RectTransform guideRoot;
    [SerializeField] private Vector2 guideOffset = new Vector2(-96f, 10f);
    [SerializeField] private bool autoPlaceGuide = true;
    [SerializeField] private bool preferGuideRightSide = true;
    [SerializeField] private float guideGap = 18f;
    [SerializeField] private float guideEdgePadding = 8f;
    [SerializeField] private Vector2 guideAutoOffset;
    [SerializeField] private bool pauseFeedDuringFirstPrompt = true;
    [SerializeField] private bool pauseTimeScaleDuringFirstPrompt;
    [SerializeField] private bool showGuideAfterFirstPrompt = true;

    [Header("Moderation")]
    [SerializeField] private bool unlockModerationOnFirstNegative = true;
    [SerializeField] private bool moderationUnlocked;
    [SerializeField] private bool allowNormalDanmakuMute = true;

    [Header("Persistence")]
    [SerializeField] private bool rememberFirstPromptWithPlayerPrefs = true;
    [SerializeField] private string firstPromptPlayerPrefsKey = "TheBigEater.Danmaku.FirstNegativeGuideSeen";
    [SerializeField] private bool rememberMutedIdsWithPlayerPrefs = true;
    [SerializeField] private string mutedIdsPlayerPrefsKey = "TheBigEater.Danmaku.MutedIds";

    private readonly HashSet<string> mutedIds = new HashSet<string>();
    private DanmakuItemView guidedTarget;
    private Canvas parentCanvas;
    private RectTransform guideParent;
    private bool firstNegativePromptShown;
    private bool firstNegativePromptOpen;
    private bool didPauseTimeScale;
    private float previousTimeScale = 1f;

    public int NegativeSeenCount { get; private set; }
    public int NegativeMutedCount { get; private set; }
    public int NormalMutedCount { get; private set; }
    public int MissedNegativeCount { get; private set; }
    public int MutedIdCount => mutedIds.Count;
    public bool ModerationUnlocked => moderationUnlocked;

    public event Action<DanmakuFeedController.DanmakuMessage> NegativeAppeared;
    public event Action<DanmakuFeedController.DanmakuMessage> NegativeMuted;
    public event Action<DanmakuFeedController.DanmakuMessage> NormalMuted;
    public event Action<DanmakuFeedController.DanmakuMessage> NegativeMissed;
    public event Action<DanmakuFeedController.DanmakuMessage> MuteCancelled;

    private void Awake()
    {
        if (feedController == null)
        {
            feedController = GetComponentInChildren<DanmakuFeedController>(true);
        }

        LoadPersistentState();
        ApplyMutedIdsToFeed(false);

        parentCanvas = GetComponentInParent<Canvas>();
        guideParent = guideRoot != null ? guideRoot.parent as RectTransform : null;
        SetFirstNegativePromptVisible(false);
        SetGuideVisible(false);
    }

    private void OnEnable()
    {
        if (feedController != null)
        {
            feedController.DanmakuClicked += HandleDanmakuClicked;
            feedController.NegativeDanmakuSpawned += HandleNegativeDanmakuSpawned;
            feedController.DanmakuRecycled += HandleDanmakuRecycled;
        }

        if (firstNegativeConfirmButton != null)
        {
            firstNegativeConfirmButton.onClick.AddListener(ConfirmFirstNegativePrompt);
        }
    }

    private void OnDisable()
    {
        if (feedController != null)
        {
            feedController.DanmakuClicked -= HandleDanmakuClicked;
            feedController.NegativeDanmakuSpawned -= HandleNegativeDanmakuSpawned;
            feedController.DanmakuRecycled -= HandleDanmakuRecycled;
        }

        if (firstNegativeConfirmButton != null)
        {
            firstNegativeConfirmButton.onClick.RemoveListener(ConfirmFirstNegativePrompt);
        }

        RestoreTimeScaleIfNeeded();
    }

    private void LateUpdate()
    {
        if (guideRoot == null || !guideRoot.gameObject.activeSelf)
        {
            return;
        }

        if (guidedTarget == null || !guidedTarget.isActiveAndEnabled || !guidedTarget.HasMessage)
        {
            SetGuideVisible(false);
            return;
        }

        FollowGuideTarget();
    }

    public void UnlockModeration()
    {
        moderationUnlocked = true;
    }

    public void LockModeration()
    {
        moderationUnlocked = false;
        moderationPopup?.Hide();
    }

    public bool IsMuted(string id)
    {
        return !string.IsNullOrEmpty(id) && mutedIds.Contains(id);
    }

    public void MuteById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        mutedIds.Add(id);
        SaveMutedIds();
        feedController?.SetMessageBlocked(id, true, true);
    }

    private void HandleNegativeDanmakuSpawned(
        DanmakuItemView item,
        DanmakuFeedController.DanmakuMessage message)
    {
        NegativeSeenCount++;
        NegativeAppeared?.Invoke(message);

        if (firstNegativePromptShown)
        {
            return;
        }

        firstNegativePromptShown = true;
        SaveFirstPromptSeen();
        guidedTarget = item;

        if (unlockModerationOnFirstNegative)
        {
            moderationUnlocked = true;
        }

        if (pauseFeedDuringFirstPrompt && feedController != null)
        {
            feedController.Pause(true);
        }

        if (pauseTimeScaleDuringFirstPrompt)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            didPauseTimeScale = true;
        }

        firstNegativePromptOpen = true;
        SetFirstNegativePromptVisible(true);

        if (firstNegativePromptRoot == null)
        {
            ConfirmFirstNegativePrompt();
        }
    }

    private void ConfirmFirstNegativePrompt()
    {
        AudioManager.PlayGenericButton();
        firstNegativePromptOpen = false;
        SetFirstNegativePromptVisible(false);

        if (unlockModerationOnFirstNegative)
        {
            moderationUnlocked = true;
        }

        if (feedController != null)
        {
            feedController.Play();
        }

        RestoreTimeScaleIfNeeded();

        if (showGuideAfterFirstPrompt && guidedTarget != null && guidedTarget.HasMessage)
        {
            SetGuideVisible(true);
            FollowGuideTarget();
        }
    }

    private void HandleDanmakuClicked(
        DanmakuItemView item,
        DanmakuFeedController.DanmakuMessage message)
    {
        if (!moderationUnlocked || firstNegativePromptOpen || item == null || message == null)
        {
            return;
        }

        if (!allowNormalDanmakuMute && !message.IsNegative)
        {
            return;
        }

        if (guidedTarget == item)
        {
            SetGuideVisible(false);
        }

        AudioManager.PlayGenericButton();
        if (moderationPopup != null)
        {
            moderationPopup.Show(item, MuteDanmaku, CancelMute);
        }
    }

    private void MuteDanmaku(DanmakuItemView item)
    {
        if (item == null || item.CurrentMessage == null)
        {
            return;
        }

        DanmakuFeedController.DanmakuMessage message = item.CurrentMessage;
        if (string.IsNullOrEmpty(message.id))
        {
            feedController?.RemoveVisibleDanmaku(item, DanmakuRecycleReason.Muted);
            return;
        }

        bool isNewMutedId = mutedIds.Add(message.id);
        SaveMutedIds();

        if (isNewMutedId && message.IsNegative)
        {
            NegativeMutedCount++;
            NegativeMuted?.Invoke(message);
        }
        else if (isNewMutedId)
        {
            NormalMutedCount++;
            NormalMuted?.Invoke(message);
        }

        if (guidedTarget == item)
        {
            guidedTarget = null;
            SetGuideVisible(false);
        }

        feedController?.SetMessageBlocked(message.id, true, true);
    }

    private void CancelMute(DanmakuItemView item)
    {
        if (item != null && item.CurrentMessage != null)
        {
            MuteCancelled?.Invoke(item.CurrentMessage);
        }
    }

    private void HandleDanmakuRecycled(
        DanmakuItemView item,
        DanmakuFeedController.DanmakuMessage message,
        DanmakuRecycleReason reason)
    {
        if (moderationPopup != null && moderationPopup.IsShowingFor(item))
        {
            moderationPopup.Hide();
        }

        if (guidedTarget == item)
        {
            guidedTarget = null;
            SetGuideVisible(false);
        }

        if (message != null && message.IsNegative && reason == DanmakuRecycleReason.Expired && !message.blocked)
        {
            MissedNegativeCount++;
            NegativeMissed?.Invoke(message);
        }
    }

    private void LoadPersistentState()
    {
        if (rememberFirstPromptWithPlayerPrefs && !string.IsNullOrEmpty(firstPromptPlayerPrefsKey))
        {
            if (PlayerPrefs.GetInt(firstPromptPlayerPrefsKey, 0) == 1)
            {
                firstNegativePromptShown = true;
                if (unlockModerationOnFirstNegative)
                {
                    moderationUnlocked = true;
                }
            }
        }

        if (!rememberMutedIdsWithPlayerPrefs || string.IsNullOrEmpty(mutedIdsPlayerPrefsKey))
        {
            return;
        }

        string savedIds = PlayerPrefs.GetString(mutedIdsPlayerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(savedIds))
        {
            return;
        }

        string[] ids = savedIds.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i].Trim();
            if (!string.IsNullOrEmpty(id))
            {
                mutedIds.Add(id);
            }
        }
    }

    private void SaveFirstPromptSeen()
    {
        if (!rememberFirstPromptWithPlayerPrefs || string.IsNullOrEmpty(firstPromptPlayerPrefsKey))
        {
            return;
        }

        PlayerPrefs.SetInt(firstPromptPlayerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    private void SaveMutedIds()
    {
        if (!rememberMutedIdsWithPlayerPrefs || string.IsNullOrEmpty(mutedIdsPlayerPrefsKey))
        {
            return;
        }

        if (mutedIds.Count == 0)
        {
            PlayerPrefs.DeleteKey(mutedIdsPlayerPrefsKey);
        }
        else
        {
            PlayerPrefs.SetString(mutedIdsPlayerPrefsKey, string.Join("\n", mutedIds));
        }

        PlayerPrefs.Save();
    }

    private void ApplyMutedIdsToFeed(bool removeVisibleItems)
    {
        if (feedController == null)
        {
            return;
        }

        foreach (string id in mutedIds)
        {
            feedController.SetMessageBlocked(id, true, removeVisibleItems);
        }
    }

    [ContextMenu("Reset Danmaku Moderation Memory")]
    private void ResetPersistentModerationMemory()
    {
        if (!string.IsNullOrEmpty(firstPromptPlayerPrefsKey))
        {
            PlayerPrefs.DeleteKey(firstPromptPlayerPrefsKey);
        }

        if (!string.IsNullOrEmpty(mutedIdsPlayerPrefsKey))
        {
            PlayerPrefs.DeleteKey(mutedIdsPlayerPrefsKey);
        }

        PlayerPrefs.Save();
        mutedIds.Clear();
        firstNegativePromptShown = false;
    }

    private void SetFirstNegativePromptVisible(bool visible)
    {
        if (firstNegativePromptRoot != null)
        {
            firstNegativePromptRoot.SetActive(visible);
        }
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!didPauseTimeScale)
        {
            return;
        }

        Time.timeScale = previousTimeScale;
        didPauseTimeScale = false;
    }

    private void SetGuideVisible(bool visible)
    {
        if (guideRoot != null)
        {
            guideRoot.gameObject.SetActive(visible);
        }
    }

    private void FollowGuideTarget()
    {
        if (guideRoot == null || guidedTarget == null || guideParent == null)
        {
            return;
        }

        Camera camera = GetCanvasCamera();
        if (autoPlaceGuide)
        {
            PlaceGuideAutomatically(camera);
            return;
        }

        Vector3[] corners = new Vector3[4];
        guidedTarget.RectTransform.GetWorldCorners(corners);
        Vector3 followWorldPoint = (corners[0] + corners[1]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, followWorldPoint);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenPoint, camera, out Vector2 localPoint))
        {
            guideRoot.anchoredPosition = localPoint + guideOffset;
        }
    }

    private void PlaceGuideAutomatically(Camera camera)
    {
        if (!TryGetTargetRectInGuideParent(camera, out Rect targetRect))
        {
            return;
        }

        Rect parentBounds = guideParent.rect;
        Vector2 guideSize = GetRectSize(guideRoot);
        Vector2 pivot = guideRoot.pivot;

        float rightX = targetRect.xMax + guideGap + guideSize.x * pivot.x;
        float leftX = targetRect.xMin - guideGap - guideSize.x * (1f - pivot.x);
        bool rightFits = FitsHorizontally(rightX, guideSize, pivot, parentBounds);
        bool leftFits = FitsHorizontally(leftX, guideSize, pivot, parentBounds);

        bool placeRight = preferGuideRightSide ? rightFits || !leftFits : !leftFits && rightFits;
        float x = placeRight ? rightX : leftX;
        float y = targetRect.center.y + (pivot.y - 0.5f) * guideSize.y;

        if (!rightFits && !leftFits)
        {
            x = targetRect.center.x + (pivot.x - 0.5f) * guideSize.x;
            float aboveY = targetRect.yMax + guideGap + guideSize.y * pivot.y;
            float belowY = targetRect.yMin - guideGap - guideSize.y * (1f - pivot.y);
            y = FitsVertically(aboveY, guideSize, pivot, parentBounds) ? aboveY : belowY;
        }

        Vector2 position = new Vector2(x, y) + guideAutoOffset;
        guideRoot.anchoredPosition = ClampPivotToParent(position, guideSize, pivot, parentBounds);
    }

    private bool TryGetTargetRectInGuideParent(Camera camera, out Rect rect)
    {
        rect = default;
        if (guidedTarget == null || guideParent == null)
        {
            return false;
        }

        Vector3[] worldCorners = new Vector3[4];
        guidedTarget.RectTransform.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(guideParent, screenPoint, camera, out Vector2 localPoint))
            {
                return false;
            }

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private bool FitsHorizontally(float pivotX, Vector2 size, Vector2 pivot, Rect bounds)
    {
        float minX = pivotX - size.x * pivot.x;
        float maxX = pivotX + size.x * (1f - pivot.x);
        return minX >= bounds.xMin + guideEdgePadding && maxX <= bounds.xMax - guideEdgePadding;
    }

    private bool FitsVertically(float pivotY, Vector2 size, Vector2 pivot, Rect bounds)
    {
        float minY = pivotY - size.y * pivot.y;
        float maxY = pivotY + size.y * (1f - pivot.y);
        return minY >= bounds.yMin + guideEdgePadding && maxY <= bounds.yMax - guideEdgePadding;
    }

    private Vector2 ClampPivotToParent(Vector2 position, Vector2 size, Vector2 pivot, Rect bounds)
    {
        float minX = bounds.xMin + guideEdgePadding + size.x * pivot.x;
        float maxX = bounds.xMax - guideEdgePadding - size.x * (1f - pivot.x);
        float minY = bounds.yMin + guideEdgePadding + size.y * pivot.y;
        float maxY = bounds.yMax - guideEdgePadding - size.y * (1f - pivot.y);

        if (minX > maxX)
        {
            position.x = bounds.center.x;
        }
        else
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }

        if (minY > maxY)
        {
            position.y = bounds.center.y;
        }
        else
        {
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        return position;
    }

    private static Vector2 GetRectSize(RectTransform rectTransform)
    {
        Vector2 size = rectTransform.rect.size;
        if (size.x <= 0f)
        {
            size.x = Mathf.Abs(rectTransform.sizeDelta.x);
        }

        if (size.y <= 0f)
        {
            size.y = Mathf.Abs(rectTransform.sizeDelta.y);
        }

        return size;
    }

    private Camera GetCanvasCamera()
    {
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return parentCanvas.worldCamera;
    }
}
