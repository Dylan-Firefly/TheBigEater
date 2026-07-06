using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class Npc2 : MonoBehaviour
{
    [SerializeField] private float visibleAlphaThreshold = 0.01f;

    private SpriteRenderer[] spriteRenderers;
    private Collider2D[] colliders;
    private EventTrigger[] eventTriggers;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        CacheComponents();
        EnsureSpriteColliders();
        SyncInteractionWithVisibility();
    }

    private void OnEnable()
    {
        CacheComponents();
        EnsureSpriteColliders();
        SyncInteractionWithVisibility();
    }

    private void LateUpdate()
    {
        SyncInteractionWithVisibility();
    }

    public void Onclick()
    {
        Npc1 parentNpc = GetComponentInParent<Npc1>();
        if (parentNpc != null)
        {
            parentNpc.Onclick();
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideAfterClick());
    }

    private IEnumerator HideAfterClick()
    {
        yield return null;

        SetVisible(false);
        hideCoroutine = null;
    }

    public void SetVisible(bool visible)
    {
        CacheComponents();

        bool changedSprite = false;
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            Color color = spriteRenderer.color;
            color.a = visible ? 1f : 0f;
            spriteRenderer.color = color;
            changedSprite = true;
        }

        if (!changedSprite && !visible)
        {
            gameObject.SetActive(false);
            return;
        }

        SyncInteractionWithVisibility();
    }

    private void CacheComponents()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        eventTriggers = GetComponentsInChildren<EventTrigger>(true);
    }

    private void EnsureSpriteColliders()
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null || spriteRenderer.GetComponent<Collider2D>() != null)
            {
                continue;
            }

            BoxCollider2D collider = spriteRenderer.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
            {
                Bounds spriteBounds = spriteRenderer.sprite.bounds;
                collider.offset = spriteBounds.center;
                collider.size = spriteBounds.size;
            }
            else
            {
                collider.offset = Vector2.zero;
                collider.size = spriteRenderer.size;
            }
        }

        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    private void SyncInteractionWithVisibility()
    {
        bool visible = IsVisible();

        foreach (Collider2D collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }

        foreach (EventTrigger eventTrigger in eventTriggers)
        {
            if (eventTrigger != null)
            {
                eventTrigger.enabled = visible;
            }
        }
    }

    private bool IsVisible()
    {
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null && spriteRenderer.enabled && spriteRenderer.color.a > visibleAlphaThreshold)
            {
                return true;
            }
        }

        return spriteRenderers.Length == 0 && gameObject.activeInHierarchy;
    }
}
