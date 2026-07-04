using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldDraggable2D : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Drag")]
    [SerializeField] private bool interactable = true;
    [SerializeField] private Camera dragCamera;
    [SerializeField] private LayerMask dropZoneMask = ~0;
    [SerializeField] private float dropProbeRadius = 0.05f;
    [SerializeField] private bool alsoCheckDraggedColliderOverlap = true;
    [SerializeField] private bool returnToStartWhenRejected = true;
    [SerializeField] private bool keepOriginalParent = true;

    [Header("Runtime Helpers")]
    [SerializeField] private bool autoCreateCollider = true;
    [SerializeField] private bool raiseSortingOrderWhileDragging = true;
    [SerializeField] private int draggingSortingOrder = 50;

    private SpriteRenderer spriteRenderer;
    private Collider2D ownCollider;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Vector3 pointerOffset;
    private int originalSortingOrder;
    private bool isDragging;

    public bool IsInteractable => interactable;
    public bool IsDragging => isDragging;
    public Vector3 OriginalPosition => originalPosition;

    public event Action<WorldDraggable2D> DragBegan;
    public event Action<WorldDraggable2D> DragEnded;
    public event Action<WorldDraggable2D, DropZone2D> DroppedOnZone;
    public event Action<WorldDraggable2D> DropRejected;

    private void Awake()
    {
        CacheReferences();
        CaptureHomeTransform();
    }

    private void OnEnable()
    {
        CaptureHomeTransform();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dragCamera == null)
        {
            dragCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable)
        {
            return;
        }

        CacheReferences();
        isDragging = true;
        Vector3 pointerWorld = ScreenToWorld(eventData.position);
        pointerOffset = transform.position - pointerWorld;

        if (spriteRenderer != null && raiseSortingOrderWhileDragging)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            spriteRenderer.sortingOrder = draggingSortingOrder;
        }

        DragBegan?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || !isDragging)
        {
            return;
        }

        transform.position = ScreenToWorld(eventData.position) + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        RestoreSortingOrder();

        DropZone2D zone = FindDropZone(ScreenToWorld(eventData.position));
        if (zone == null && alsoCheckDraggedColliderOverlap)
        {
            zone = FindDropZoneOverlappingDraggedCollider();
        }

        if (zone != null && zone.TryDrop(this))
        {
            DroppedOnZone?.Invoke(this, zone);
        }
        else
        {
            DropRejected?.Invoke(this);
            if (returnToStartWhenRejected)
            {
                ReturnHome();
            }
        }

        DragEnded?.Invoke(this);
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        CacheReferences();
        if (ownCollider != null)
        {
            ownCollider.enabled = value;
        }
    }

    public void CaptureHomeTransform()
    {
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
    }

    public void ReturnHome()
    {
        if (keepOriginalParent && originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalScale;
    }

    public void MoveTo(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    private DropZone2D FindDropZone(Vector3 worldPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPosition, dropProbeRadius, dropZoneMask);
        for (int i = 0; i < hits.Length; i++)
        {
            DropZone2D zone = hits[i].GetComponentInParent<DropZone2D>();
            if (zone != null && zone.enabled && zone.gameObject.activeInHierarchy)
            {
                return zone;
            }
        }

        return null;
    }

    private DropZone2D FindDropZoneOverlappingDraggedCollider()
    {
        CacheReferences();
        if (ownCollider == null)
        {
            return null;
        }

        Bounds bounds = ownCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, transform.eulerAngles.z, dropZoneMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == ownCollider)
            {
                continue;
            }

            DropZone2D zone = hits[i].GetComponentInParent<DropZone2D>();
            if (zone != null && zone.enabled && zone.gameObject.activeInHierarchy)
            {
                return zone;
            }
        }

        return null;
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        Camera camera = dragCamera != null ? dragCamera : Camera.main;
        if (camera == null)
        {
            return transform.position;
        }

        float depth = Mathf.Abs(camera.transform.position.z - transform.position.z);
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        world.z = transform.position.z;
        return world;
    }

    private void RestoreSortingOrder()
    {
        if (spriteRenderer != null && raiseSortingOrderWhileDragging)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
        }
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (ownCollider == null)
        {
            ownCollider = GetComponent<Collider2D>();
            if (ownCollider == null && autoCreateCollider)
            {
                ownCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }
    }
}
