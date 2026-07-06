using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhoneDemoFlowController : MonoBehaviour
{
    public enum PhoneDemoState
    {
        Closed,
        AppGrid,
        Profile,
        DraftPost,
        PostDetail,
        PkRunning,
        FinalResult
    }

    [Header("Setup")]
    [SerializeField] private bool initializeOnAwake = true;

    [Header("Flow")]
    [SerializeField] private bool phonePanelVisibleOnStart;
    [SerializeField] private bool hidePhoneButtonWhileOpen = true;
    [SerializeField] private bool keepPostPageBehindFinalResult = true;

    [Header("Modules")]
    [SerializeField] private PhonePageNavigator pageNavigator;
    [SerializeField] private PhonePostController postController;
    [SerializeField] private PhoneCommonPageScrollTrigger commonPageScrollTrigger;
    [SerializeField] private PhoneCommentRevealController commentRevealController;
    [SerializeField] private PhoneReviewPkController reviewPkController;

    [Header("Buttons")]
    [SerializeField] private Button phoneButton;
    [SerializeField] private Button postAppButton;
    [SerializeField] private Button streamAppButton;
    [SerializeField] private Button startPkButton;
    [SerializeField] private List<Button> returnButtons = new List<Button>();
    [SerializeField] private List<Button> resultReturnButtons = new List<Button>();

    private bool isPhoneBlocked;
    private bool buttonsBound;
    private bool moduleEventsBound;

    public PhoneDemoState State { get; private set; } = PhoneDemoState.Closed;
    public bool IsPhoneBlocked => isPhoneBlocked;
    public bool IsPhoneOpen => State != PhoneDemoState.Closed;
    public bool HasPosted => postController != null && postController.HasPosted;
    public int LastGoodCount => reviewPkController != null ? reviewPkController.LastGoodCount : 0;
    public int LastBadCount => reviewPkController != null ? reviewPkController.LastBadCount : 0;
    public float LastGoodRatio => reviewPkController != null ? reviewPkController.LastGoodRatio : 0.5f;
    public bool LastResultWasGood => reviewPkController != null && reviewPkController.LastResultWasGood;

    public event Action PhoneOpened;
    public event Action PhoneClosed;
    public event Action PostPublished;
    public event Action<int, int, float> PkStarted;
    public event Action<int, int, float, bool> PkCompleted;
    public event Action<int, int, float, bool> FinalResultConfirmed;

    private void Awake()
    {
        ValidateReferences();
        BindButtons();
        BindModuleEvents();

        if (initializeOnAwake)
        {
            ResetDemoState();
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
        UnbindModuleEvents();
    }

    [ContextMenu("Reset Demo State")]
    public void ResetDemoState()
    {
        StopResultFlow();
        postController?.ResetPostState();
        reviewPkController?.ResetToNeutral();
        commentRevealController?.HideAll();
        commonPageScrollTrigger?.ResetTrigger();
        SetButtonInteractable(startPkButton, false);

        if (phonePanelVisibleOnStart)
        {
            OpenPhone();
        }
        else
        {
            pageNavigator?.ShowClosed();
            SetActive(phoneButton != null ? phoneButton.gameObject : null, true);
            SetReturnButtonsVisible(false);
            State = PhoneDemoState.Closed;
        }
    }

    public void SetPhoneBlocked(bool blocked)
    {
        isPhoneBlocked = blocked;
        if (blocked && IsPhoneOpen)
        {
            ClosePhone();
        }
    }

    public void SetPostUnlocked(bool unlocked)
    {
        postController?.SetPostUnlocked(unlocked);
    }

    public void OpenPhone()
    {
        if (isPhoneBlocked)
        {
            Debug.Log("[PhoneDemoFlow] Phone is blocked during live gameplay.");
            return;
        }

        AudioManager.PlayPhoneOpen();
        pageNavigator?.SetPhonePanelVisible(true);
        SetActive(phoneButton != null ? phoneButton.gameObject : null, !hidePhoneButtonWhileOpen);
        SetReturnButtonsVisible(true);

        if (State == PhoneDemoState.Closed)
        {
            ShowAppGrid();
        }

        PhoneOpened?.Invoke();
    }

    public void ClosePhone()
    {
        StopResultFlow();
        postController?.CloseDraft();
        commonPageScrollTrigger?.ResetTrigger();
        pageNavigator?.ShowClosed();
        commentRevealController?.HideAll();
        SetButtonInteractable(startPkButton, false);
        SetActive(phoneButton != null ? phoneButton.gameObject : null, true);
        SetReturnButtonsVisible(false);
        State = PhoneDemoState.Closed;
        PhoneClosed?.Invoke();
    }

    public void ShowAppGrid()
    {
        StopResultFlow();
        postController?.CloseDraft();
        commonPageScrollTrigger?.ResetTrigger();
        pageNavigator?.ShowAppGrid();
        commentRevealController?.HideAll();
        SetButtonInteractable(startPkButton, false);
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.AppGrid;
    }

    public void OpenPostApp()
    {
        OpenPostApp(true);
    }

    private void OpenPostApp(bool playSound)
    {
        if (playSound)
        {
            AudioManager.PlayPhoneButton();
        }

        StopResultFlow();
        postController?.CloseDraft();
        commonPageScrollTrigger?.ResetTrigger();
        pageNavigator?.ShowProfile();
        commentRevealController?.HideAll();
        SetButtonInteractable(startPkButton, false);
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.Profile;
    }

    public void OpenDraftPost()
    {
        if (postController == null || !postController.TryOpenDraft())
        {
            return;
        }

        AudioManager.PlayPhoneButton();
        pageNavigator?.ShowProfile();
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.DraftPost;
    }

    public void PublishPost()
    {
        if (postController == null || !postController.TryPublish())
        {
            return;
        }

        AudioManager.PlayPhoneButton();
        pageNavigator?.ShowProfile();
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.Profile;
        PostPublished?.Invoke();
    }

    public void OpenPostDetail()
    {
        OpenPostDetail(true);
    }

    private void OpenPostDetail(bool playSound)
    {
        if (postController != null && !postController.CanOpenPublishedPost)
        {
            Debug.Log("[PhoneDemoFlow] Cannot open post detail before a post is published.");
            return;
        }

        if (playSound)
        {
            AudioManager.PlayPhoneButton();
        }

        StopResultFlow();
        commentRevealController?.HideAll();
        pageNavigator?.ShowPostDetail(false);
        commonPageScrollTrigger?.ResetTrigger();
        SetButtonInteractable(startPkButton, false);
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.PostDetail;
    }

    public void StartPkResult()
    {
        if (reviewPkController != null && reviewPkController.IsRunning)
        {
            return;
        }

        if (commonPageScrollTrigger != null && !commonPageScrollTrigger.HasTriggered)
        {
            Debug.Log("[PhoneDemoFlow] Scroll the common page down before starting the comment result.");
            return;
        }

        AudioManager.PlayPhoneButton();
        pageNavigator?.ShowPkPage();
        commentRevealController?.StartReveal();
        SetButtonInteractable(startPkButton, false);
        SetReturnButtonsVisible(true);

        State = PhoneDemoState.PkRunning;
        float revealDuration = commentRevealController != null ? commentRevealController.TotalRevealDuration : 0f;
        reviewPkController?.StartRandomPk(revealDuration);
    }

    public void Back()
    {
        if (State == PhoneDemoState.FinalResult)
        {
            ConfirmFinalResult();
            return;
        }

        AudioManager.PlayBack();
        if (State == PhoneDemoState.PkRunning)
        {
            OpenPostDetail(false);
            return;
        }

        if (State == PhoneDemoState.PostDetail)
        {
            OpenPostApp(false);
            return;
        }

        if (State == PhoneDemoState.DraftPost)
        {
            postController?.CloseDraft();
            pageNavigator?.ShowProfile();
            State = PhoneDemoState.Profile;
            return;
        }

        if (State == PhoneDemoState.Profile)
        {
            ShowAppGrid();
            return;
        }

        if (State == PhoneDemoState.AppGrid)
        {
            ClosePhone();
        }
    }

    public void ConfirmFinalResult()
    {
        if (State != PhoneDemoState.FinalResult)
        {
            Back();
            return;
        }

        AudioManager.PlayPhoneButton();
        if (FinalResultConfirmed != null)
        {
            FinalResultConfirmed.Invoke(LastGoodCount, LastBadCount, LastGoodRatio, LastResultWasGood);
            return;
        }

        OpenPostApp(false);
    }

    private void HandleResultPromptUnlocked()
    {
        if (State != PhoneDemoState.PostDetail)
        {
            return;
        }

        pageNavigator?.SetShowingResultVisible(true);
        SetButtonInteractable(startPkButton, true);
    }

    private void HandlePkStarted(int goodCount, int badCount, float goodRatio)
    {
        PkStarted?.Invoke(goodCount, badCount, goodRatio);
    }

    private void HandlePkCompleted(int goodCount, int badCount, float goodRatio, bool isGoodResult)
    {
        pageNavigator?.ShowFinalResult(isGoodResult, keepPostPageBehindFinalResult);
        SetButtonInteractable(startPkButton, true);
        SetReturnButtonsVisible(true);
        State = PhoneDemoState.FinalResult;
        PkCompleted?.Invoke(goodCount, badCount, goodRatio, isGoodResult);
    }

    private void StopResultFlow()
    {
        reviewPkController?.StopPk();
        commentRevealController?.StopReveal();
    }

    private void BindButtons()
    {
        if (buttonsBound)
        {
            return;
        }

        if (phoneButton != null)
        {
            phoneButton.onClick.AddListener(OpenPhone);
        }

        if (postAppButton != null)
        {
            postAppButton.onClick.AddListener(OpenPostApp);
        }

        if (streamAppButton != null)
        {
            streamAppButton.onClick.AddListener(OpenPostApp);
        }

        if (postController != null)
        {
            if (postController.OpenDraftButton != null)
            {
                postController.OpenDraftButton.onClick.AddListener(OpenDraftPost);
            }

            if (postController.PublishButton != null)
            {
                postController.PublishButton.onClick.AddListener(PublishPost);
            }

            if (postController.PublishedPostButton != null)
            {
                postController.PublishedPostButton.onClick.AddListener(OpenPostDetail);
            }
        }

        if (startPkButton != null)
        {
            startPkButton.onClick.AddListener(StartPkResult);
        }

        foreach (Button returnButton in returnButtons)
        {
            if (returnButton != null)
            {
                returnButton.onClick.AddListener(Back);
            }
        }

        foreach (Button resultReturnButton in resultReturnButtons)
        {
            if (resultReturnButton != null)
            {
                resultReturnButton.onClick.AddListener(ConfirmFinalResult);
            }
        }

        buttonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!buttonsBound)
        {
            return;
        }

        if (phoneButton != null)
        {
            phoneButton.onClick.RemoveListener(OpenPhone);
        }

        if (postAppButton != null)
        {
            postAppButton.onClick.RemoveListener(OpenPostApp);
        }

        if (streamAppButton != null)
        {
            streamAppButton.onClick.RemoveListener(OpenPostApp);
        }

        if (postController != null)
        {
            if (postController.OpenDraftButton != null)
            {
                postController.OpenDraftButton.onClick.RemoveListener(OpenDraftPost);
            }

            if (postController.PublishButton != null)
            {
                postController.PublishButton.onClick.RemoveListener(PublishPost);
            }

            if (postController.PublishedPostButton != null)
            {
                postController.PublishedPostButton.onClick.RemoveListener(OpenPostDetail);
            }
        }

        if (startPkButton != null)
        {
            startPkButton.onClick.RemoveListener(StartPkResult);
        }

        foreach (Button returnButton in returnButtons)
        {
            if (returnButton != null)
            {
                returnButton.onClick.RemoveListener(Back);
            }
        }

        foreach (Button resultReturnButton in resultReturnButtons)
        {
            if (resultReturnButton != null)
            {
                resultReturnButton.onClick.RemoveListener(ConfirmFinalResult);
            }
        }

        buttonsBound = false;
    }

    private void BindModuleEvents()
    {
        if (moduleEventsBound)
        {
            return;
        }

        if (reviewPkController != null)
        {
            reviewPkController.PkStarted += HandlePkStarted;
            reviewPkController.PkCompleted += HandlePkCompleted;
        }

        if (commonPageScrollTrigger != null)
        {
            commonPageScrollTrigger.ResultPromptUnlocked += HandleResultPromptUnlocked;
        }

        moduleEventsBound = true;
    }

    private void UnbindModuleEvents()
    {
        if (!moduleEventsBound)
        {
            return;
        }

        if (reviewPkController != null)
        {
            reviewPkController.PkStarted -= HandlePkStarted;
            reviewPkController.PkCompleted -= HandlePkCompleted;
        }

        if (commonPageScrollTrigger != null)
        {
            commonPageScrollTrigger.ResultPromptUnlocked -= HandleResultPromptUnlocked;
        }

        moduleEventsBound = false;
    }

    private void ValidateReferences()
    {
        WarnMissing(pageNavigator, nameof(pageNavigator));
        WarnMissing(postController, nameof(postController));
        WarnMissing(commonPageScrollTrigger, nameof(commonPageScrollTrigger));
        WarnMissing(commentRevealController, nameof(commentRevealController));
        WarnMissing(reviewPkController, nameof(reviewPkController));
        WarnMissing(phoneButton, nameof(phoneButton));
        WarnMissing(postAppButton, nameof(postAppButton));
        WarnMissing(startPkButton, nameof(startPkButton));

        pageNavigator?.ValidateReferences(this);
        postController?.ValidateReferences(this);
        commonPageScrollTrigger?.ValidateReferences(this);
        reviewPkController?.ValidateReferences(this);
    }

    private void WarnMissing(UnityEngine.Object target, string fieldName)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(PhoneDemoFlowController)}] Missing reference: {fieldName}.", this);
        }
    }

    private void SetReturnButtonsVisible(bool visible)
    {
        foreach (Button returnButton in returnButtons)
        {
            if (returnButton != null)
            {
                SetActive(returnButton.gameObject, visible);
            }
        }

        foreach (Button resultReturnButton in resultReturnButtons)
        {
            if (resultReturnButton != null)
            {
                SetActive(resultReturnButton.gameObject, visible);
            }
        }
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
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
