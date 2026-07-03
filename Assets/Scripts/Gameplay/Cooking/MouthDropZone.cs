using UnityEngine;

public class MouthDropZone : DropZone2D
{
    [SerializeField] private EatingController eatingController;

    protected override bool HandleDrop(WorldDraggable2D draggable)
    {
        if (eatingController == null)
        {
            eatingController = GetComponentInParent<EatingController>();
        }

        EdibleFoodItem food = draggable.GetComponent<EdibleFoodItem>();
        return eatingController != null
            && food != null
            && eatingController.TryEat(food);
    }
}
