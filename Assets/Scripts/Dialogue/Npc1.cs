using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Npc1 : MonoBehaviour
{
    public GameObject Dialog;
    public string dialogBlock = "Click Pen";
    public bool isChat = false;

    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string fallbackFlowchartName = "mainMapFlowchart";

    [Header("Reward Hint")]
    [SerializeField] private bool showItemHintOnComplete = true;
    [SerializeField] private GetItemHintController itemHint;
    [SerializeField] private string itemHintId = "egg";
    [SerializeField, Min(0f)] private float itemHintDelaySeconds;

    private readonly List<GameObject> dialogSteps = new List<GameObject>();
    private int currentDialogStep = -1;
    private int lastHandledPointerFrame = -1;
    private bool playerBlockExecuted;
    private bool dialogOpenedByPlayer;
    private bool itemHintShownForCurrentDialog;

    private void Awake()
    {
        EnsureEventSystem();
        EnsurePhysics2DRaycaster();
        CacheDialogSteps();
        HideDialog();
    }

    private void Update()
    {
        if (isChat && WasInteractPressed())
        {
            OpenDialog();
            isChat = false;
        }

        if (Dialog != null && Dialog.activeSelf && WasPointerPressedThisFrame() && IsPointerOverCurrentDialogStep())
        {
            HandleDialogClick(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isChat = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isChat = false;
        }
    }

    public void Onclick()
    {
        Onclick1();
    }

    public void Onclick1()
    {
        HandleDialogClick(true);
    }

    public void Onclick2()
    {
        HandleDialogClick(false);
    }

    private void HandleDialogClick(bool executePlayerBlock)
    {
        if (lastHandledPointerFrame == Time.frameCount)
        {
            return;
        }

        lastHandledPointerFrame = Time.frameCount;

        if (executePlayerBlock)
        {
            ExecutePlayerBlockOnce();
        }

        AdvanceDialog();
    }

    private void OpenDialog()
    {
        if (Dialog == null)
        {
            Debug.LogWarning($"[Npc1] Missing Dialog on {name}.");
            return;
        }

        CacheDialogSteps();
        Dialog.SetActive(true);
        playerBlockExecuted = false;
        dialogOpenedByPlayer = true;
        itemHintShownForCurrentDialog = false;
        ShowDialogStep(0);
    }

    private void AdvanceDialog()
    {
        if (Dialog == null)
        {
            return;
        }

        CacheDialogSteps();

        if (!Dialog.activeSelf)
        {
            OpenDialog();
            return;
        }

        if (currentDialogStep < 0)
        {
            currentDialogStep = FindActiveDialogStep();
        }

        int nextStep = currentDialogStep + 1;
        if (nextStep < dialogSteps.Count)
        {
            ShowDialogStep(nextStep);
            return;
        }

        CompleteDialog();
    }

    private void HideDialog()
    {
        if (Dialog == null)
        {
            return;
        }

        Dialog.SetActive(false);
        currentDialogStep = -1;
    }

    private void CompleteDialog()
    {
        bool shouldShowHint = dialogOpenedByPlayer && !itemHintShownForCurrentDialog;
        HideDialog();
        dialogOpenedByPlayer = false;

        if (shouldShowHint)
        {
            ShowRewardHint();
        }
    }

    private void ShowDialogStep(int index)
    {
        if (dialogSteps.Count == 0)
        {
            currentDialogStep = -1;
            return;
        }

        currentDialogStep = Mathf.Clamp(index, 0, dialogSteps.Count - 1);

        for (int i = 0; i < dialogSteps.Count; i++)
        {
            if (dialogSteps[i] != null)
            {
                bool shouldShow = i == currentDialogStep;
                dialogSteps[i].SetActive(shouldShow);

                Npc2 clickableBubble = dialogSteps[i].GetComponent<Npc2>();
                if (clickableBubble != null)
                {
                    clickableBubble.SetVisible(shouldShow);
                }
            }
        }
    }

    private int FindActiveDialogStep()
    {
        for (int i = 0; i < dialogSteps.Count; i++)
        {
            if (dialogSteps[i] != null && dialogSteps[i].activeSelf)
            {
                return i;
            }
        }

        return dialogSteps.Count > 0 ? 0 : -1;
    }

    private bool IsPointerOverCurrentDialogStep()
    {
        CacheDialogSteps();

        if (currentDialogStep < 0)
        {
            currentDialogStep = FindActiveDialogStep();
        }

        if (currentDialogStep < 0 || currentDialogStep >= dialogSteps.Count)
        {
            return false;
        }

        GameObject currentStep = dialogSteps[currentDialogStep];
        if (currentStep == null)
        {
            return false;
        }

        Collider2D collider = currentStep.GetComponent<Collider2D>();
        Camera mainCamera = Camera.main;
        if (collider == null || mainCamera == null)
        {
            return false;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(GetPointerScreenPosition());
        return collider.OverlapPoint(worldPosition);
    }

    private void CacheDialogSteps()
    {
        dialogSteps.Clear();

        if (Dialog == null)
        {
            return;
        }

        Transform dialogTransform = Dialog.transform;
        bool rootLooksLikeStep = Dialog.GetComponent<SpriteRenderer>() != null ||
                                 Dialog.GetComponent<Collider2D>() != null;

        if (dialogTransform.childCount == 0 || rootLooksLikeStep)
        {
            dialogSteps.Add(Dialog);
            EnsureDialogStepCollider(Dialog);
            return;
        }

        for (int i = 0; i < dialogTransform.childCount; i++)
        {
            Transform child = dialogTransform.GetChild(i);
            if (child != null)
            {
                dialogSteps.Add(child.gameObject);
                EnsureDialogStepCollider(child.gameObject);
            }
        }
    }

    private static void EnsureDialogStepCollider(GameObject dialogStep)
    {
        if (dialogStep == null || dialogStep.GetComponent<Collider2D>() != null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = dialogStep.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        BoxCollider2D collider = dialogStep.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        if (spriteRenderer.drawMode == SpriteDrawMode.Simple)
        {
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            collider.offset = spriteBounds.center;
            collider.size = spriteBounds.size;
        }
        else
        {
            collider.offset = Vector2.zero;
            collider.size = spriteRenderer.size;
        }
    }

    private static void EnsurePhysics2DRaycaster()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || mainCamera.GetComponent<Physics2DRaycaster>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existingEventSystem != null)
        {
            existingEventSystem.gameObject.SetActive(true);
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void ExecutePlayerBlockOnce()
    {
        if (playerBlockExecuted || string.IsNullOrWhiteSpace(dialogBlock))
        {
            return;
        }

        Flowchart targetFlowchart = ResolveFlowchart();
        if (targetFlowchart == null)
        {
            Debug.LogWarning($"[Npc1] No Flowchart found for block '{dialogBlock}'.");
            return;
        }

        Block block = targetFlowchart.FindBlock(dialogBlock);
        if (block == null)
        {
            Debug.LogWarning($"[Npc1] Flowchart '{targetFlowchart.name}' has no block '{dialogBlock}'.");
            return;
        }

        PrepareSayDialogsForExecution();
        playerBlockExecuted = targetFlowchart.ExecuteBlock(block, 0, HandlePlayerBlockComplete);
    }

    private void HandlePlayerBlockComplete()
    {
        StartCoroutine(CleanupFinishedSayDialogs());
    }

    private IEnumerator CleanupFinishedSayDialogs()
    {
        yield return null;

        SayDialog[] sayDialogs = Object.FindObjectsByType<SayDialog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SayDialog sayDialog in sayDialogs)
        {
            if (sayDialog == null)
            {
                continue;
            }

            Fungus.Writer writer = sayDialog.GetComponent<Fungus.Writer>();
            if (writer != null && (writer.IsWriting || writer.IsWaitingForInput))
            {
                continue;
            }

            sayDialog.Clear();
            sayDialog.SetCharacter(null);
            sayDialog.FadeWhenDone = true;
            RestoreSayDialogVisibility(sayDialog);
            sayDialog.SetActive(false);
        }
    }

    private static void PrepareSayDialogsForExecution()
    {
        SayDialog[] sayDialogs = Object.FindObjectsByType<SayDialog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SayDialog sayDialog in sayDialogs)
        {
            if (sayDialog == null)
            {
                continue;
            }

            Transform dialogTransform = sayDialog.transform;
            bool hiddenByScale = dialogTransform.localScale.sqrMagnitude <= 0.0001f;
            bool inactive = !sayDialog.gameObject.activeInHierarchy;
            if (hiddenByScale || inactive)
            {
                RestoreSayDialogVisibility(sayDialog);
            }
        }
    }

    private static void RestoreSayDialogVisibility(SayDialog sayDialog)
    {
        if (sayDialog == null)
        {
            return;
        }

        Transform dialogTransform = sayDialog.transform;
        if (dialogTransform.localScale.sqrMagnitude <= 0.0001f)
        {
            dialogTransform.localScale = Vector3.one;
        }

        CanvasGroup canvasGroup = sayDialog.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void ShowRewardHint()
    {
        if (!showItemHintOnComplete)
        {
            return;
        }

        itemHintShownForCurrentDialog = true;

        if (itemHintDelaySeconds > 0f)
        {
            StartCoroutine(ShowRewardHintAfterDelay());
            return;
        }

        TryShowRewardHint();
    }

    private IEnumerator ShowRewardHintAfterDelay()
    {
        yield return new WaitForSecondsRealtime(itemHintDelaySeconds);
        TryShowRewardHint();
    }

    private void TryShowRewardHint()
    {
        GetItemHintController targetHint = ResolveItemHint();
        if (targetHint == null)
        {
            Debug.LogWarning($"[Npc1] No GetItemHintController found for reward hint on {name}.", this);
            return;
        }

        targetHint.TryShowItem(itemHintId);
    }

    private GetItemHintController ResolveItemHint()
    {
        if (itemHint != null)
        {
            return itemHint;
        }

        itemHint = Object.FindFirstObjectByType<GetItemHintController>(FindObjectsInactive.Include);
        return itemHint;
    }

    private Flowchart ResolveFlowchart()
    {
        if (flowchart != null && (string.IsNullOrWhiteSpace(dialogBlock) || flowchart.HasBlock(dialogBlock)))
        {
            return flowchart;
        }

        Flowchart[] flowcharts = Object.FindObjectsByType<Flowchart>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (!string.IsNullOrWhiteSpace(dialogBlock))
        {
            foreach (Flowchart candidate in flowcharts)
            {
                if (candidate != null && candidate.HasBlock(dialogBlock))
                {
                    flowchart = candidate;
                    return flowchart;
                }
            }
        }

        GameObject namedFlowchart = !string.IsNullOrWhiteSpace(fallbackFlowchartName)
            ? GameObject.Find(fallbackFlowchartName)
            : null;
        if (namedFlowchart != null && namedFlowchart.TryGetComponent(out flowchart))
        {
            return flowchart;
        }

        GameObject legacyFlowchart = GameObject.Find("Flowchart");
        if (legacyFlowchart != null && legacyFlowchart.TryGetComponent(out flowchart))
        {
            return flowchart;
        }

        flowchart = flowcharts.Length > 0 ? flowcharts[0] : null;
        return flowchart;
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F))
        {
            return true;
        }
#endif

        return false;
    }

    private static bool WasPointerPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        return false;
    }

    private static Vector3 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 pointerPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            return new Vector3(pointerPosition.x, pointerPosition.y, 0f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector3.zero;
#endif
    }
}
