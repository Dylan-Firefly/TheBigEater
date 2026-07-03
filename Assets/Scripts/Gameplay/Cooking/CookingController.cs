using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cookPreviewPoint;
    [SerializeField] private List<IngredientItem> ingredients = new List<IngredientItem>();
    [SerializeField] private List<CookingRecipe> recipes = new List<CookingRecipe>();

    [Header("Timing")]
    [SerializeField] private float cookedPreviewSeconds = 0.2f;
    [SerializeField] private bool resetIngredientsOnBegin = true;

    private readonly Dictionary<string, int> acceptedIngredientCounts = new Dictionary<string, int>();
    private bool isCookingActive;
    private bool recipeCompleted;

    public bool IsCookingActive => isCookingActive;
    public event Action<CookingRecipe, GameObject> RecipeCompleted;
    public event Action<IngredientItem> IngredientAccepted;
    public event Action<IngredientItem> IngredientRejected;

    private void Awake()
    {
        HideRecipeOutputs();
    }

    public void BeginCooking()
    {
        acceptedIngredientCounts.Clear();
        recipeCompleted = false;
        isCookingActive = true;

        if (resetIngredientsOnBegin)
        {
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i] != null)
                {
                    ingredients[i].ResetForCooking();
                }
            }
        }

        HideRecipeOutputs();
    }

    public void StopCooking()
    {
        isCookingActive = false;
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (ingredients[i] != null)
            {
                ingredients[i].SetInteractable(false);
            }
        }
    }

    public bool TryAddIngredient(IngredientItem ingredient)
    {
        if (!isCookingActive || recipeCompleted || ingredient == null)
        {
            return false;
        }

        string ingredientId = ingredient.IngredientId;
        if (string.IsNullOrEmpty(ingredientId) || !CanAnyRecipeAccept(ingredientId))
        {
            IngredientRejected?.Invoke(ingredient);
            return false;
        }

        IngredientAccepted?.Invoke(ingredient);
        StartCoroutine(AcceptIngredientRoutine(ingredient));
        return true;
    }

    private IEnumerator AcceptIngredientRoutine(IngredientItem ingredient)
    {
        ingredient.SetInteractable(false);

        if (cookPreviewPoint != null && ingredient.Draggable != null)
        {
            ingredient.Draggable.MoveTo(cookPreviewPoint.position);
        }

        ingredient.ShowCookedState();

        if (cookedPreviewSeconds > 0f)
        {
            yield return new WaitForSeconds(cookedPreviewSeconds);
        }

        acceptedIngredientCounts.TryGetValue(ingredient.IngredientId, out int count);
        acceptedIngredientCounts[ingredient.IngredientId] = count + 1;
        ingredient.FinishAccepted();
        CheckRecipeCompletion();
    }

    private bool CanAnyRecipeAccept(string ingredientId)
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            CookingRecipe recipe = recipes[i];
            if (recipe != null && recipe.CanAcceptWith(acceptedIngredientCounts, ingredientId))
            {
                return true;
            }
        }

        return false;
    }

    private void CheckRecipeCompletion()
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            CookingRecipe recipe = recipes[i];
            if (recipe != null && recipe.IsSatisfiedBy(acceptedIngredientCounts))
            {
                CompleteRecipe(recipe);
                return;
            }
        }
    }

    private void CompleteRecipe(CookingRecipe recipe)
    {
        recipeCompleted = true;
        isCookingActive = false;

        GameObject output = GetOrCreateRecipeOutput(recipe);
        if (output != null)
        {
            output.SetActive(true);
            Transform spawnPoint = recipe.OutputSpawnPoint;
            if (spawnPoint != null)
            {
                output.transform.position = spawnPoint.position;
            }

            EdibleFoodItem edible = output.GetComponent<EdibleFoodItem>();
            if (edible != null)
            {
                edible.SetInteractable(false);
            }
        }

        StopCooking();
        RecipeCompleted?.Invoke(recipe, output);
    }

    private GameObject GetOrCreateRecipeOutput(CookingRecipe recipe)
    {
        if (recipe.OutputObject != null)
        {
            return recipe.OutputObject;
        }

        if (recipe.OutputPrefab != null)
        {
            Transform spawnPoint = recipe.OutputSpawnPoint;
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            return Instantiate(recipe.OutputPrefab, position, Quaternion.identity);
        }

        return null;
    }

    private void HideRecipeOutputs()
    {
        for (int i = 0; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].OutputObject != null)
            {
                recipes[i].OutputObject.SetActive(false);
            }
        }
    }
}
