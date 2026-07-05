using UnityEngine;
using UnityEngine.UI;

public static class PhoneUiLookup
{
    public static GameObject FindGameObject(Transform root, string objectName)
    {
        Transform child = FindChildRecursive(root, objectName);
        return child != null ? child.gameObject : null;
    }

    public static Button FindButtonOn(Transform root, string objectName)
    {
        GameObject target = FindGameObject(root, objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    public static Button FindButtonUnder(Transform root, string objectName)
    {
        GameObject target = FindGameObject(root, objectName);
        return target != null ? target.GetComponentInChildren<Button>(true) : null;
    }

    public static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    public static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
