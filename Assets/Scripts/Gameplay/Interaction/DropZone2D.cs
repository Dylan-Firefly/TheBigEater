using UnityEngine;

public abstract class DropZone2D : MonoBehaviour
{
    [SerializeField] private bool acceptDrops = true;
    [SerializeField] private bool autoCreateTriggerCollider = true;

    public bool AcceptDrops
    {
        get => acceptDrops;
        set => acceptDrops = value;
    }

    protected virtual void Awake()
    {
        EnsureTriggerCollider();
    }

    public bool TryDrop(WorldDraggable2D draggable)
    {
        if (!acceptDrops || draggable == null)
        {
            return false;
        }

        return HandleDrop(draggable);
    }

    protected abstract bool HandleDrop(WorldDraggable2D draggable);

    private void EnsureTriggerCollider()
    {
        if (!autoCreateTriggerCollider)
        {
            return;
        }

        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D == null)
        {
            collider2D = gameObject.AddComponent<BoxCollider2D>();
        }

        collider2D.isTrigger = true;
    }
}
