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
        PublishedPost,
        PkRunning,
        FinalResult
    }

    [Header("Setup")]
    [SerializeField] private bool autoBindOnAwake = true;
    [SerializeField] private bool initializeOnAwake = true;

    [Header("Flow")]
    [SerializeField] private bool phonePanelVisibleOnStart;
    [SerializeField] private bool hidePhoneButtonWhileOpen = true;
    [SerializeField] private bool keepPostPageBehindFinalResult = true;

    [Header("Modules")]
    [SerializeField] private PhonePageNavigator pageNavigator;
    [SerializeField] private PhonePostController postController;
    [SerializeField] private PhoneCommentRevealController commentRevealController;
    [SerializeField] private PhoneReviewPkController reviewPkController;

    [Header("Buttons")]
    [SerializeField] private Button phoneButton;
    [SerializeField] private Button postAppButton;
    [SerializeField] private Button streamAppButton;
    [SerializeField] private Button startPkButton;
    [SerializeField] private List<Button> returnButtons = new List<Button>();

    private bool isPhoneBlocked;
    private bool buttonsBound;

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

    private void Awake()
    {
        if (autoBindOnAwake)
        {
            AutoBind();
        }

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

    [ContextMenu("Auto Bind")]
    public void AutoBind()
    {
        pageNavigator = pageNavigator != null ? pageNavigator : PhoneUiLookup.EnsureComponent<PhonePageNavigator>(gameObject);
        postController = postController != null ? postController : PhoneUiLookup.EnsureComponent<PhonePostController>(gameObject);
        commentRevealController = commentRevealController != null ? commentRevealController : PhoneUiLookup.EnsureComponent<PhoneCommentRevealController>(gameObject);
        reviewPkController = reviewPkController != null ? reviewPkController : PhoneUiLookup.EnsureComponent<PhoneReviewPkController>(gameObject);

        pageNavigator.AutoBind(transform);
        postController.AutoBind(transform);
        commentRevealController.AutoBind(transform);
        reviewPkController.AutoBind(transform);

        phoneButton = phoneButton != null ? phoneButton : PhoneUiLookup.FindButtonOn(transform, "PhoneBtn");
        postAppButton = postAppButton != null ? postAppButton : PhoneUiLookup.FindButtonUnder(transform, "PostApp");
        streamAppButton = streamAppButton != null ? streamAppButton : PhoneUiLookup.FindButtonUnder(transform, "StreamApp");
        startPkButton = startPkButton != null ? startPkButton : PhoneUiLookup.FindButtonOn(transform, "ResultBtn");
        AutoBindReturnButtons();
    }

    [ContextMenu("Reset Demo State")]
    public void ResetDemoState()
    {
        StopResultFlow();
        postController.ResetPostState();
        reviewPkController.ResetToNeutral();
        commentRevealController.HideAll();
        SetButtonInteractable(startPkButton, true);

        if (phonePanelVisibleOnStart)
        {
            OpenPhone();
        }
        else
        {
            pageNavigator.ShowClosed();
            SetActive(phoneButton != null ? phoneButton.gameObject : null, true);
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
        postController.SetPostUnlocked(unlocked);
    }

    public void OpenPhone()
    {
        if (isPhoneBlocked)
        {
            Debug.Log("[PhoneDemoFlow] Phone is blocked during live gameplay.");
            return;
        }

        pageNavigator.SetPhonePanelVisible(true);
        SetActive(phoneButton != null ? phoneButton.gameObject : null, !hidePhoneButtonWhileOpen);

        if (State == PhoneDemoState.Closed)
        {
            ShowAppGrid();
        }

        PhoneOpened?.Invoke();
    }

    public void ClosePhone()
    {
        StopResultFlow();
        pageNavigator.ShowClosed();
        commentRevealController.HideAll();
        SetButtonInteractable(startPkButton, true);
        SetActive(phoneButton != null ? phoneButton.gameObject : null, true);
        State = PhoneDemoState.Closed;
        PhoneClosed?.Invoke();
    }

    public void ShowAppGrid()
    {
        StopResultFlow();
        pageNavigator.ShowAppGrid();
        commentRevealController.HideAll();
        SetButtonInteractable(startPkButton, true);
        State = PhoneDemoState.AppGrid;
    }

    public void OpenPostApp()
    {
        StopResultFlow();
        pageNavigator.ShowProfile();
        commentRevealController.HideAll();
        SetButtonInteractable(startPkButton, true);
        State = PhoneDemoState.Profile;
    }

    public void PublishPost()
    {
        bool publishedNewPost = postController.TryPublish();
        if (!publishedNewPost && !postController.HasPosted)
        {
            return;
        }

        OpenPublishedPost(true);

        if (publishedNewPost)
        {
            PostPublished?.Invoke();
        }
    }

    public void OpenPublishedPost(bool showResultGuide)
    {
        StopResultFlow();
        pageNavigator.ShowPublishedPost(showResultGuide);
        commentRevealController.HideAll();
        SetButtonInteractable(startPkButton, true);
        State = PhoneDemoState.PublishedPost;
    }

    public void StartPkResult()
    {
        if (reviewPkController.IsRunning)
        {
            return;
        }

        pageNavigator.ShowPkPage();
        commentRevealController.StartReveal();
        SetButtonInteractable(startPkButton, false);

        State = PhoneDemoState.PkRunning;
        reviewPkController.StartRandomPk(commentRevealController.TotalRevealDuration);
    }

    public void Back()
    {
        if (State == PhoneDemoState.FinalResult)
        {
            OpenPostApp();
            return;
        }

        if (State == PhoneDemoState.PkRunning)
        {
            OpenPublishedPost(false);
            return;
        }

        if (State == PhoneDemoState.PublishedPost)
        {
            OpenPostApp();
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

    private void HandlePkStarted(int goodCount, int badCount, float goodRatio)
    {
        PkStarted?.Invoke(goodCount, badCount, goodRatio);
    }

    private void HandlePkCompleted(int goodCount, int badCount, float goodRatio, bool isGoodResult)
    {
        pageNavigator.ShowFinalResult(isGoodResult, keepPostPageBehindFinalResult);
        SetButtonInteractable(startPkButton, true);
        State = PhoneDemoState.FinalResult;
        PkCompleted?.Invoke(goodCount, badCount, goodRatio, isGoodResult);
    }

    private void StopResultFlow()
    {
        reviewPkController.StopPk();
        commentRevealController.StopReveal();
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

        if (postController != null && postController.PublishButton != null)
        {
            postController.PublishButton.onClick.AddListener(PublishPost);
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

        if (postController != null && postController.PublishButton != null)
        {
            postController.PublishButton.onClick.RemoveListener(PublishPost);
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

        buttonsBound = false;
    }

    private void BindModuleEvents()
    {
        if (reviewPkController != null)
        {
            reviewPkController.PkStarted -= HandlePkStarted;
            reviewPkController.PkStarted += HandlePkStarted;
            reviewPkController.PkCompleted -= HandlePkCompleted;
            reviewPkController.PkCompleted += HandlePkCompleted;
        }
    }

    private void UnbindModuleEvents()
    {
        if (reviewPkController != null)
        {
            reviewPkController.PkStarted -= HandlePkStarted;
            reviewPkController.PkCompleted -= HandlePkCompleted;
        }
    }

    private void AutoBindReturnButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == "Return" && !returnButtons.Contains(button))
            {
                returnButtons.Add(button);
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
