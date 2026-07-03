using UnityEngine;

public class IngredientItem : MonoBehaviour
{
    [SerializeField] private string ingredientId;
    [SerializeField] private Sprite rawSprite;
    [SerializeField] private Sprite cookedSprite;
    [SerializeField] private bool hideAfterCooked = true;

    private SpriteRenderer spriteRenderer;
    private WorldDraggable2D draggable;

    public string IngredientId => ingredientId;
    public Sprite CookedSprite => cookedSprite;
    public bool HideAfterCooked => hideAfterCooked;
    public WorldDraggable2D Draggable => draggable;

    private void Awake()
    {
        CacheReferences();
        if (rawSprite == null && spriteRenderer != null)
        {
            rawSprite = spriteRenderer.sprite;
        }
    }

    public void ResetForCooking()
    {
        CacheReferences();
        gameObject.SetActive(true);

        if (spriteRenderer != null && rawSprite != null)
        {
            spriteRenderer.sprite = rawSprite;
        }

        if (draggable != null)
        {
            draggable.SetInteractable(true);
            draggable.ReturnHome();
        }
    }

    public void ShowCookedState()
    {
        CacheReferences();
        if (spriteRenderer != null && cookedSprite != null)
        {
            spriteRenderer.sprite = cookedSprite;
        }
    }

    public void FinishAccepted()
    {
        if (draggable != null)
        {
            draggable.SetInteractable(false);
        }

        if (hideAfterCooked)
        {
            gameObject.SetActive(false);
        }
    }

    public void SetInteractable(bool value)
    {
        CacheReferences();
        if (draggable != null)
        {
            draggable.SetInteractable(value);
        }
    }

    private void CacheReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (draggable == null)
        {
            draggable = GetComponent<WorldDraggable2D>();
            if (draggable == null)
            {
                draggable = gameObject.AddComponent<WorldDraggable2D>();
            }
        }
    }
}
