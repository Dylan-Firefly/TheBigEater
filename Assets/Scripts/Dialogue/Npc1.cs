using Fungus;
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

    private readonly List<GameObject> dialogSteps = new List<GameObject>();
    private int currentDialogStep = -1;
    private int lastHandledPointerFrame = -1;
    private bool playerBlockExecuted;

    private void Awake()
    {
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

        HideDialog();
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
                dialogSteps[i].SetActive(i == currentDialogStep);
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

        playerBlockExecuted = targetFlowchart.ExecuteBlock(block);
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
