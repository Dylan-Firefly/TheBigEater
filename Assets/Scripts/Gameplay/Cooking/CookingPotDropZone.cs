using UnityEngine;

public class CookingPotDropZone : DropZone2D
{
    [SerializeField] private CookingController cookingController;

    protected override bool HandleDrop(WorldDraggable2D draggable)
    {
        if (cookingController == null)
        {
            cookingController = GetComponentInParent<CookingController>();
        }

        IngredientItem ingredient = draggable.GetComponent<IngredientItem>();
        return cookingController != null
            && ingredient != null
            && cookingController.TryAddIngredient(ingredient);
    }
}
