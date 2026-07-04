using System;
using System.Collections;
using UnityEngine;

public enum LiveStreamStage
{
    Idle,
    Cooking,
    DishPreview,
    Eating,
    Success,
    Failed
}

public enum LiveStreamFailReason
{
    CookingTimeout,
    EatingTimeout,
    Cancelled
}

public class LiveStreamController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CookingController cookingController;
    [SerializeField] private EatingController eatingController;
    [SerializeField] private StreamTimerController timerController;
    [SerializeField] private GameObject cookingRoot;
    [SerializeField] private GameObject eatingRoot;

    [Header("Timing")]
    [SerializeField] private float cookingDurationSeconds = 25f;
    [SerializeField] private float eatingDurationSeconds = 12f;
    [SerializeField] private float dishPreviewSeconds = 1f;
    [SerializeField] private bool pauseTimerDuringDishPreview = true;
    [SerializeField] private bool startOnAwake;

    private Coroutine stageRoutine;

    public LiveStreamStage Stage { get; private set; } = LiveStreamStage.Idle;
    public event Action<LiveStreamStage> StageChanged;
    public event Action LiveSucceeded;
    public event Action<LiveStreamFailReason> LiveFailed;

    private void Awake()
    {
        if (cookingController == null)
        {
            cookingController = GetComponentInChildren<CookingController>(true);
        }

        if (eatingController == null)
        {
            eatingController = GetComponentInChildren<EatingController>(true);
        }

        if (timerController == null)
        {
            timerController = GetComponentInChildren<StreamTimerController>(true);
        }
    }

    private void OnEnable()
    {
        if (cookingController != null)
        {
            cookingController.RecipeCompleted += HandleRecipeCompleted;
        }

        if (eatingController != null)
        {
            eatingController.FoodEaten += HandleFoodEaten;
        }

        if (timerController != null)
        {
            timerController.TimerFinished += HandleTimerFinished;
        }
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartLive();
        }
        else
        {
            SetStage(LiveStreamStage.Idle);
        }
    }

    private void OnDisable()
    {
        if (cookingController != null)
        {
            cookingController.RecipeCompleted -= HandleRecipeCompleted;
        }

        if (eatingController != null)
        {
            eatingController.FoodEaten -= HandleFoodEaten;
        }

        if (timerController != null)
        {
            timerController.TimerFinished -= HandleTimerFinished;
        }
    }

    [ContextMenu("Start Live")]
    public void StartLive()
    {
        StopStageRoutine();
        EnterCooking();
    }

    public void FailLive(LiveStreamFailReason reason)
    {
        if (Stage == LiveStreamStage.Success || Stage == LiveStreamStage.Failed)
        {
            return;
        }

        StopStageRoutine();
        timerController?.StopTimer();
        cookingController?.StopCooking();
        eatingController?.StopEating();
        SetStage(LiveStreamStage.Failed);
        Debug.Log($"Live stream failed: {reason}");
        LiveFailed?.Invoke(reason);
    }

    private void EnterCooking()
    {
        SetStage(LiveStreamStage.Cooking);
        SetRootActive(cookingRoot, true);
        SetRootActive(eatingRoot, false);
        eatingController?.StopEating();
        cookingController?.BeginCooking();
        timerController?.StartTimer(cookingDurationSeconds);
        Debug.Log("Live stream cooking stage started.");
    }

    private void HandleRecipeCompleted(CookingRecipe recipe, GameObject outputObject)
    {
        if (Stage != LiveStreamStage.Cooking)
        {
            return;
        }

        StopStageRoutine();
        stageRoutine = StartCoroutine(DishPreviewRoutine(outputObject));
    }

    private IEnumerator DishPreviewRoutine(GameObject outputObject)
    {
        SetStage(LiveStreamStage.DishPreview);

        if (pauseTimerDuringDishPreview)
        {
            timerController?.Pause();
        }

        if (dishPreviewSeconds > 0f)
        {
            yield return new WaitForSeconds(dishPreviewSeconds);
        }

        if (Stage == LiveStreamStage.DishPreview)
        {
            EnterEating(outputObject);
        }
    }

    private void EnterEating(GameObject outputObject)
    {
        SetStage(LiveStreamStage.Eating);
        SetRootActive(eatingRoot, true);
        cookingController?.StopCooking();
        eatingController?.BeginEating(outputObject);
        SetRootActive(cookingRoot, false);
        timerController?.StartTimer(eatingDurationSeconds);
        Debug.Log("Live stream eating stage started.");
    }

    private void HandleFoodEaten(EdibleFoodItem food)
    {
        if (Stage != LiveStreamStage.Eating)
        {
            return;
        }

        StopStageRoutine();
        timerController?.StopTimer();
        SetStage(LiveStreamStage.Success);
        Debug.Log("Live stream success.");
        LiveSucceeded?.Invoke();
    }

    private void HandleTimerFinished()
    {
        if (Stage == LiveStreamStage.Cooking)
        {
            FailLive(LiveStreamFailReason.CookingTimeout);
        }
        else if (Stage == LiveStreamStage.Eating)
        {
            FailLive(LiveStreamFailReason.EatingTimeout);
        }
        else if (Stage == LiveStreamStage.DishPreview && !pauseTimerDuringDishPreview)
        {
            FailLive(LiveStreamFailReason.CookingTimeout);
        }
    }

    private void SetStage(LiveStreamStage stage)
    {
        Stage = stage;
        StageChanged?.Invoke(stage);
    }

    private void StopStageRoutine()
    {
        if (stageRoutine != null)
        {
            StopCoroutine(stageRoutine);
            stageRoutine = null;
        }
    }

    private static void SetRootActive(GameObject root, bool active)
    {
        if (root != null)
        {
            root.SetActive(active);
        }
    }
}
