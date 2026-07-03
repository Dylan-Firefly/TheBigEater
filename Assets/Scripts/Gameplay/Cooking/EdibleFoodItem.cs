using UnityEngine;

public class EdibleFoodItem : MonoBehaviour
{
    [SerializeField] private string foodId;

    private WorldDraggable2D draggable;

    public string FoodId => foodId;
    public WorldDraggable2D Draggable => draggable;

    private void Awake()
    {
        CacheReferences();
    }

    public void SetFoodId(string value)
    {
        foodId = value;
    }

    public void PrepareForEating(Vector3 worldPosition)
    {
        CacheReferences();
        gameObject.SetActive(true);
        transform.position = worldPosition;

        if (draggable != null)
        {
            draggable.CaptureHomeTransform();
            draggable.SetInteractable(true);
        }
    }

    public void FinishEaten()
    {
        if (draggable != null)
        {
            draggable.SetInteractable(false);
        }

        gameObject.SetActive(false);
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
