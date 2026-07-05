using System;
using UnityEngine;
using UnityEngine.UI;

public class PhonePostController : MonoBehaviour
{
    [Header("Post Rules")]
    [SerializeField] private bool postUnlockedOnStart = true;
    [SerializeField] private bool allowRepeatPosts;

    [Header("Profile Prompt")]
    [SerializeField] private GameObject postRedHint;
    [SerializeField] private Button openDraftButton;

    [Header("Draft Panel")]
    [SerializeField] private GameObject postPanel;
    [SerializeField] private GameObject newPostPage;
    [SerializeField] private Button publishButton;

    [Header("Published Post")]
    [SerializeField] private GameObject publishedPostObject;
    [SerializeField] private Button publishedPostButton;
    [SerializeField] private bool hidePublishedPostUntilPosted = true;
    [SerializeField] private bool disablePublishedPostButtonUntilPosted = true;

    private bool isPostUnlocked;
    private bool isDraftOpen;
    private bool hasPosted;

    public bool IsPostUnlocked => isPostUnlocked;
    public bool IsDraftOpen => isDraftOpen;
    public bool HasPosted => hasPosted;
    public bool CanOpenDraft => isPostUnlocked && !isDraftOpen && (allowRepeatPosts || !hasPosted);
    public bool CanPublish => isDraftOpen && isPostUnlocked && (allowRepeatPosts || !hasPosted);
    public bool CanOpenPublishedPost => hasPosted;
    public Button OpenDraftButton => openDraftButton;
    public Button PublishButton => publishButton;
    public Button PublishedPostButton => publishedPostButton;

    public event Action DraftOpened;
    public event Action DraftClosed;
    public event Action PostPublished;

    public void ResetPostState()
    {
        hasPosted = false;
        isDraftOpen = false;
        isPostUnlocked = postUnlockedOnStart;
        RefreshUi();
    }

    public void SetPostUnlocked(bool unlocked)
    {
        isPostUnlocked = unlocked;

        if (!isPostUnlocked)
        {
            CloseDraft();
            return;
        }

        RefreshUi();
    }

    public bool TryOpenDraft()
    {
        if (!CanOpenDraft)
        {
            return false;
        }

        isDraftOpen = true;
        RefreshUi();
        DraftOpened?.Invoke();
        return true;
    }

    public void CloseDraft()
    {
        if (!isDraftOpen)
        {
            RefreshUi();
            return;
        }

        isDraftOpen = false;
        RefreshUi();
        DraftClosed?.Invoke();
    }

    public bool TryPublish()
    {
        if (!CanPublish)
        {
            return false;
        }

        hasPosted = true;
        isDraftOpen = false;

        if (!allowRepeatPosts)
        {
            isPostUnlocked = false;
        }

        RefreshUi();
        PostPublished?.Invoke();
        return true;
    }

    public void ValidateReferences(UnityEngine.Object owner)
    {
        WarnMissing(owner, postRedHint, nameof(postRedHint));
        WarnMissing(owner, openDraftButton, nameof(openDraftButton));
        WarnMissing(owner, postPanel, nameof(postPanel));
        WarnMissing(owner, newPostPage, nameof(newPostPage));
        WarnMissing(owner, publishButton, nameof(publishButton));
        WarnMissing(owner, publishedPostButton, nameof(publishedPostButton));
    }

    private void RefreshUi()
    {
        SetActive(postRedHint, CanOpenDraft);
        SetActive(postPanel, isDraftOpen);
        SetActive(newPostPage, isDraftOpen);
        SetActive(GetPublishedPostObject(), !hidePublishedPostUntilPosted || hasPosted);

        if (openDraftButton != null)
        {
            openDraftButton.interactable = CanOpenDraft;
        }

        if (publishButton != null)
        {
            publishButton.interactable = CanPublish;
        }

        if (publishedPostButton != null && disablePublishedPostButtonUntilPosted)
        {
            publishedPostButton.interactable = CanOpenPublishedPost;
        }
    }

    private GameObject GetPublishedPostObject()
    {
        if (publishedPostObject != null)
        {
            return publishedPostObject;
        }

        return publishedPostButton != null ? publishedPostButton.gameObject : null;
    }

    private static void WarnMissing(UnityEngine.Object owner, UnityEngine.Object target, string fieldName)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(PhonePostController)}] Missing reference: {fieldName}.", owner);
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
