using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CookingRecipe
{
    [SerializeField] private string recipeId;
    [SerializeField] private List<string> requiredIngredientIds = new List<string>();
    [SerializeField] private GameObject outputObject;
    [SerializeField] private GameObject outputPrefab;
    [SerializeField] private Transform outputSpawnPoint;

    public string RecipeId => recipeId;
    public IReadOnlyList<string> RequiredIngredientIds => requiredIngredientIds;
    public GameObject OutputObject => outputObject;
    public GameObject OutputPrefab => outputPrefab;
    public Transform OutputSpawnPoint => outputSpawnPoint;

    public bool HasIngredient(string ingredientId)
    {
        return !string.IsNullOrEmpty(ingredientId) && requiredIngredientIds.Contains(ingredientId);
    }

    public int CountRequired(string ingredientId)
    {
        int count = 0;
        for (int i = 0; i < requiredIngredientIds.Count; i++)
        {
            if (requiredIngredientIds[i] == ingredientId)
            {
                count++;
            }
        }

        return count;
    }

    public bool IsSatisfiedBy(Dictionary<string, int> ingredientCounts)
    {
        if (requiredIngredientIds.Count == 0)
        {
            return false;
        }

        Dictionary<string, int> requiredCounts = BuildRequiredCounts();
        foreach (KeyValuePair<string, int> pair in requiredCounts)
        {
            if (!ingredientCounts.TryGetValue(pair.Key, out int currentCount) || currentCount < pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanAcceptWith(Dictionary<string, int> ingredientCounts, string nextIngredientId)
    {
        if (!HasIngredient(nextIngredientId))
        {
            return false;
        }

        Dictionary<string, int> requiredCounts = BuildRequiredCounts();
        ingredientCounts.TryGetValue(nextIngredientId, out int currentCount);
        return requiredCounts.TryGetValue(nextIngredientId, out int requiredCount)
            && currentCount + 1 <= requiredCount;
    }

    private Dictionary<string, int> BuildRequiredCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < requiredIngredientIds.Count; i++)
        {
            string id = requiredIngredientIds[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            counts.TryGetValue(id, out int count);
            counts[id] = count + 1;
        }

        return counts;
    }
}
