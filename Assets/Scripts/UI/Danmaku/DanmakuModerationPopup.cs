using System;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuModerationPopup : MonoBehaviour
{
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private Button muteButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Vector2 followOffset = new Vector2(80f, 12f);
    [SerializeField] private bool autoPlaceNearDanmaku = true;
    [SerializeField] private bool preferRightSide = true;
    [SerializeField] private float horizontalGap = 16f;
    [SerializeField] private float edgePadding = 8f;
    [SerializeField] private float verticalOffset;
    [SerializeField] private bool hideWhenTargetUnavailable = true;

    private DanmakuItemView target;
    private Action<DanmakuItemView> muteRequested;
    private Action<DanmakuItemView> cancelRequested;
    private Canvas parentCanvas;
    private RectTransform parentRect;

    public bool IsVisible => popupRoot != null && popupRoot.gameObject.activeSelf;
    public DanmakuItemView Target => target;

    private void Awake()
    {
        if (popupRoot == null)
        {
            popupRoot = transform as RectTransform;
        }

        parentCanvas = GetComponentInParent<Canvas>();
        parentRect = popupRoot != null ? popupRoot.parent as RectTransform : null;

        if (muteButton != null)
        {
            muteButton.onClick.AddListener(HandleMuteClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (muteButton != null)
        {
            muteButton.onClick.RemoveListener(HandleMuteClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }
    }

    private void LateUpdate()
    {
        if (!IsVisible)
        {
            return;
        }

        if (!IsTargetAvailable())
        {
            if (hideWhenTargetUnavailable)
            {
                Hide();
            }

            return;
        }

        FollowTarget();
    }

    public void Show(
        DanmakuItemView newTarget,
        Action<DanmakuItemView> onMuteRequested,
        Action<DanmakuItemView> onCancelRequested)
    {
        if (newTarget == null || !newTarget.HasMessage)
        {
            Hide();
            return;
        }

        target = newTarget;
        muteRequested = onMuteRequested;
        cancelRequested = onCancelRequested;

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(true);
            FollowTarget();
        }
    }

    public void Hide()
    {
        target = null;
        muteRequested = null;
        cancelRequested = null;

        if (popupRoot != null)
        {
            popupRoot.gameObject.SetActive(false);
        }
    }

    public bool IsShowingFor(DanmakuItemView item)
    {
        return IsVisible && target == item;
    }

    private void HandleMuteClicked()
    {
        DanmakuItemView currentTarget = target;
        Action<DanmakuItemView> currentMuteRequested = muteRequested;
        Hide();
        if (currentTarget != null)
        {
            currentMuteRequested?.Invoke(currentTarget);
        }
    }

    private void HandleCancelClicked()
    {
        DanmakuItemView currentTarget = target;
        Action<DanmakuItemView> currentCancelRequested = cancelRequested;
        Hide();
        if (currentTarget != null)
        {
            currentCancelRequested?.Invoke(currentTarget);
        }
    }

    private void FollowTarget()
    {
        if (popupRoot == null || target == null || parentRect == null)
        {
            return;
        }

        if (autoPlaceNearDanmaku)
        {
            PlacePopupAutomatically();
            return;
        }

        Camera camera = GetCanvasCamera();
        Vector3[] corners = new Vector3[4];
        target.RectTransform.GetWorldCorners(corners);
        Vector3 followWorldPoint = (corners[1] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, followWorldPoint);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out Vector2 localPoint))
        {
            popupRoot.anchoredPosition = localPoint + followOffset;
        }
    }

    private void PlacePopupAutomatically()
    {
        if (!TryGetTargetRectInParent(out Rect targetRect))
        {
            return;
        }

        Rect parentBounds = parentRect.rect;
        Vector2 popupSize = GetRectSize(popupRoot);
        Vector2 pivot = popupRoot.pivot;
        float centerY = targetRect.center.y + verticalOffset;

        float rightX = targetRect.xMax + horizontalGap + popupSize.x * pivot.x;
        float leftX = targetRect.xMin - horizontalGap - popupSize.x * (1f - pivot.x);
        bool rightFits = FitsHorizontally(rightX, popupSize, pivot, parentBounds);
        bool leftFits = FitsHorizontally(leftX, popupSize, pivot, parentBounds);

        bool placeRight = preferRightSide ? rightFits || !leftFits : !leftFits && rightFits;
        float x = placeRight ? rightX : leftX;
        float y = centerY + (pivot.y - 0.5f) * popupSize.y;

        if (!rightFits && !leftFits)
        {
            x = targetRect.center.x + (pivot.x - 0.5f) * popupSize.x;
            float aboveY = targetRect.yMax + horizontalGap + popupSize.y * pivot.y;
            float belowY = targetRect.yMin - horizontalGap - popupSize.y * (1f - pivot.y);
            y = FitsVertically(aboveY, popupSize, pivot, parentBounds) ? aboveY : belowY;
        }

        popupRoot.anchoredPosition = ClampPivotToParent(new Vector2(x, y), popupSize, pivot, parentBounds);
    }

    private bool TryGetTargetRectInParent(out Rect rect)
    {
        rect = default;
        if (target == null || parentRect == null)
        {
            return false;
        }

        Camera camera = GetCanvasCamera();
        Vector3[] worldCorners = new Vector3[4];
        target.RectTransform.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, camera, out Vector2 localPoint))
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
        return minX >= bounds.xMin + edgePadding && maxX <= bounds.xMax - edgePadding;
    }

    private bool FitsVertically(float pivotY, Vector2 size, Vector2 pivot, Rect bounds)
    {
        float minY = pivotY - size.y * pivot.y;
        float maxY = pivotY + size.y * (1f - pivot.y);
        return minY >= bounds.yMin + edgePadding && maxY <= bounds.yMax - edgePadding;
    }

    private Vector2 ClampPivotToParent(Vector2 position, Vector2 size, Vector2 pivot, Rect bounds)
    {
        float minX = bounds.xMin + edgePadding + size.x * pivot.x;
        float maxX = bounds.xMax - edgePadding - size.x * (1f - pivot.x);
        float minY = bounds.yMin + edgePadding + size.y * pivot.y;
        float maxY = bounds.yMax - edgePadding - size.y * (1f - pivot.y);

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

    private bool IsTargetAvailable()
    {
        return target != null && target.isActiveAndEnabled && target.HasMessage;
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
