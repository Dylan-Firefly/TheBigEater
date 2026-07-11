using Fungus;
using System.Collections;
using TheBigEater.Gameplay.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class npcDialog : MonoBehaviour
{
    public string pen = "Click Pen";

    [Header("Fungus")]
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string fallbackFlowchartName = "mainMapFlowchart";

    [Header("Completion")]
    [SerializeField] private bool disableInteractionAfterComplete = true;
    [SerializeField] private bool rememberCompletionAcrossScenes = true;
    [SerializeField] private string completionId;

    [Header("Player Input")]
    [SerializeField] private bool lockPlayerInputDuringDialogue = true;
    [SerializeField] private PlayerController playerController;

    [Header("Reward Hint")]
    [FormerlySerializedAs("showItemHintOnComplete")]
    [SerializeField] private bool grantsItemOnComplete;
    [SerializeField] private GetItemHintController itemHint;
    [FormerlySerializedAs("itemHintId")]
    [SerializeField] private string rewardItemId = "recipe";
    [SerializeField] private Sprite rewardHintSpriteOverride;
    [SerializeField, Min(0f)] private float itemHintDelaySeconds;
    [FormerlySerializedAs("showItemHintOnlyOnce")]
    [SerializeField] private bool showRewardOnlyOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueComplete = new UnityEvent();

    private bool canChat;
    private bool isDialogueRunning;
    private bool dialogueCompleted;
    private bool itemHintShown;
    private bool ownsPlayerInputLock;
    private Coroutine itemHintRoutine;

    private bool InteractionLocked => disableInteractionAfterComplete && dialogueCompleted;

    private void Awake()
    {
        RestoreCompletionState();
    }

    private void OnDisable()
    {
        ReleasePlayerInputLock();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canChat = !InteractionLocked;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            canChat = false;
        }
    }

    private void Update()
    {
        if (WasInteractPressed() && canChat && !isDialogueRunning && !InteractionLocked)
        {
            TryStartDialogue();
        }
    }

    private void TryStartDialogue()
    {
        if (InteractionLocked)
        {
            canChat = false;
            return;
        }

        Flowchart targetFlowchart = ResolveFlowchart();
        if (targetFlowchart == null || string.IsNullOrWhiteSpace(pen) || !targetFlowchart.HasBlock(pen))
        {
            Debug.LogWarning($"[npcDialog] Flowchart block '{pen}' was not found.", this);
            canChat = false;
            return;
        }

        Block block = targetFlowchart.FindBlock(pen);
        isDialogueRunning = true;
        AcquirePlayerInputLock();
        bool started = targetFlowchart.ExecuteBlock(block, 0, HandleDialogueComplete);
        if (!started)
        {
            isDialogueRunning = false;
            ReleasePlayerInputLock();
            Debug.LogWarning($"[npcDialog] Fungus block '{pen}' could not start.", this);
            return;
        }

        canChat = false;
    }

    private void HandleDialogueComplete()
    {
        isDialogueRunning = false;
        ReleasePlayerInputLock();
        dialogueCompleted = true;
        canChat = false;
        RememberDialogueCompletion();
        ShowRewardHint();
        onDialogueComplete?.Invoke();
    }

    private void RestoreCompletionState()
    {
        if (!rememberCompletionAcrossScenes)
        {
            return;
        }

        dialogueCompleted = GameManager.EnsureInstance().IsDialogueCompleted(GetCompletionId());
        if (dialogueCompleted)
        {
            canChat = false;
            itemHintShown = true;
        }
    }

    private void RememberDialogueCompletion()
    {
        if (rememberCompletionAcrossScenes)
        {
            GameManager.EnsureInstance().MarkDialogueCompleted(GetCompletionId());
        }
    }

    private string GetCompletionId()
    {
        return string.IsNullOrWhiteSpace(completionId) ? pen : completionId;
    }

    private void AcquirePlayerInputLock()
    {
        if (!lockPlayerInputDuringDialogue || ownsPlayerInputLock)
        {
            return;
        }

        PlayerController targetPlayer = ResolvePlayerController();
        if (targetPlayer == null)
        {
            return;
        }

        targetPlayer.AcquireInputLock();
        ownsPlayerInputLock = true;
    }

    private void ReleasePlayerInputLock()
    {
        if (!ownsPlayerInputLock)
        {
            return;
        }

        if (playerController != null)
        {
            playerController.ReleaseInputLock();
        }

        ownsPlayerInputLock = false;
    }

    private PlayerController ResolvePlayerController()
    {
        if (playerController == null)
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        return playerController;
    }

    private void ShowRewardHint()
    {
        if (!grantsItemOnComplete)
        {
            return;
        }

        if (showRewardOnlyOnce && itemHintShown)
        {
            return;
        }

        itemHintShown = true;

        if (itemHintRoutine != null)
        {
            StopCoroutine(itemHintRoutine);
        }

        if (itemHintDelaySeconds > 0f)
        {
            itemHintRoutine = StartCoroutine(ShowRewardHintAfterDelay());
            return;
        }

        TryShowRewardHint();
    }

    private IEnumerator ShowRewardHintAfterDelay()
    {
        yield return new WaitForSecondsRealtime(itemHintDelaySeconds);
        itemHintRoutine = null;
        TryShowRewardHint();
    }

    private void TryShowRewardHint()
    {
        GetItemHintController targetHint = ResolveItemHint();
        if (targetHint == null)
        {
            Debug.LogWarning($"[npcDialog] No GetItemHintController found for reward hint on {name}.", this);
            return;
        }

        if (rewardHintSpriteOverride != null)
        {
            targetHint.ShowSprite(rewardHintSpriteOverride);
            return;
        }

        targetHint.TryShowItem(rewardItemId);
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
        if (flowchart != null && (string.IsNullOrWhiteSpace(pen) || flowchart.HasBlock(pen)))
        {
            return flowchart;
        }

        Flowchart[] flowcharts = Object.FindObjectsByType<Flowchart>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Flowchart candidate in flowcharts)
        {
            if (candidate != null && candidate.HasBlock(pen))
            {
                flowchart = candidate;
                return flowchart;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackFlowchartName))
        {
            GameObject namedFlowchart = GameObject.Find(fallbackFlowchartName);
            if (namedFlowchart != null && namedFlowchart.TryGetComponent(out flowchart))
            {
                return flowchart;
            }
        }

        return flowcharts.Length > 0 ? flowcharts[0] : null;
    }

    private static bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
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
}
