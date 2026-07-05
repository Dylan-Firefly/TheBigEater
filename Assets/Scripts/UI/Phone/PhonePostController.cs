using System;
using UnityEngine;
using UnityEngine.UI;

public class PhonePostController : MonoBehaviour
{
    [Header("Post Rules")]
    [SerializeField] private bool postUnlockedOnStart = true;
    [SerializeField] private bool allowRepeatPosts;
    [SerializeField] private bool hideRedHintOnStart = true;

    [Header("References")]
    [SerializeField] private GameObject postRedHint;
    [SerializeField] private Button publishButton;

    private bool isPostUnlocked;
    private bool hasPosted;

    public bool IsPostUnlocked => isPostUnlocked;
    public bool HasPosted => hasPosted;
    public Button PublishButton => publishButton;

    public event Action PostPublished;

    [ContextMenu("Auto Bind")]
    public void AutoBind()
    {
        AutoBind(transform);
    }

    public void AutoBind(Transform root)
    {
        Transform searchRoot = root != null ? root : transform;
        postRedHint = postRedHint != null ? postRedHint : PhoneUiLookup.FindGameObject(searchRoot, "PostRedHint");
        publishButton = publishButton != null ? publishButton : PhoneUiLookup.FindButtonOn(searchRoot, "PostBtn");
    }

    public void ResetPostState()
    {
        hasPosted = false;
        isPostUnlocked = postUnlockedOnStart;
        RefreshUi();
    }

    public void SetPostUnlocked(bool unlocked)
    {
        isPostUnlocked = unlocked;
        RefreshUi();
    }

    public bool TryPublish()
    {
        if (!isPostUnlocked)
        {
            Debug.Log("[PhonePost] Post is locked. Call SetPostUnlocked(true) after a successful stream.");
            return false;
        }

        if (hasPosted && !allowRepeatPosts)
        {
            return false;
        }

        hasPosted = true;
        RefreshUi();
        PostPublished?.Invoke();
        return true;
    }

    private void RefreshUi()
    {
        SetActive(postRedHint, !hideRedHintOnStart && isPostUnlocked && !hasPosted);

        if (publishButton != null)
        {
            publishButton.interactable = isPostUnlocked && (allowRepeatPosts || !hasPosted);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
