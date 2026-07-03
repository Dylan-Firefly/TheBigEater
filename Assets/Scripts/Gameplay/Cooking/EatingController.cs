using System;
using UnityEngine;

public class EatingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform foodSpawnPoint;
    [SerializeField] private Transform foodRuntimeParent;

    [Header("Rules")]
    [SerializeField] private string requiredFoodId;
    [SerializeField] private bool allowAnyFoodWhenRequiredIdEmpty = true;

    private EdibleFoodItem currentFood;
    private bool isEatingActive;

    public bool IsEatingActive => isEatingActive;
    public event Action<EdibleFoodItem> FoodEaten;

    public void BeginEating(GameObject foodObject)
    {
        currentFood = foodObject != null ? foodObject.GetComponent<EdibleFoodItem>() : null;
        if (currentFood == null && foodObject != null)
        {
            currentFood = foodObject.AddComponent<EdibleFoodItem>();
        }

        isEatingActive = true;

        if (currentFood != null)
        {
            Transform runtimeParent = foodRuntimeParent != null
                ? foodRuntimeParent
                : foodSpawnPoint != null ? foodSpawnPoint.parent : null;
            if (runtimeParent != null)
            {
                currentFood.transform.SetParent(runtimeParent, true);
            }

            Vector3 spawnPosition = foodSpawnPoint != null ? foodSpawnPoint.position : currentFood.transform.position;
            currentFood.PrepareForEating(spawnPosition);
        }
    }

    public void StopEating()
    {
        isEatingActive = false;
        if (currentFood != null)
        {
            currentFood.SetInteractable(false);
        }
    }

    public bool TryEat(EdibleFoodItem food)
    {
        if (!isEatingActive || food == null || !CanEat(food))
        {
            return false;
        }

        food.FinishEaten();
        isEatingActive = false;
        FoodEaten?.Invoke(food);
        return true;
    }

    private bool CanEat(EdibleFoodItem food)
    {
        if (string.IsNullOrEmpty(requiredFoodId))
        {
            return allowAnyFoodWhenRequiredIdEmpty;
        }

        return food.FoodId == requiredFoodId;
    }
}
