using System.Collections;
using Fungus;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class IndoorEntryDialogueController : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private GameObject dialogRoot;
    [SerializeField] private string dialogRootName = "playerBigSayDialog";
    [SerializeField, Min(0f)] private float showDelaySeconds = 0.1f;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Fungus")]
    [SerializeField] private bool executeFungusBlock = true;
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string fallbackFlowchartName = "mainMapFlowchart";
    [SerializeField] private string blockName = "mama";

    [Header("Objects Hidden During Dialogue")]
    [SerializeField] private GameObject[] hideDuringDialogueObjects;

    [Header("One Shot")]
    [SerializeField] private bool rememberWithPlayerPrefs = true;
    [SerializeField] private string shownKey = "TheBigEater.Indoor.EntryDialogueShown";

    private bool[] hiddenObjectPreviousStates;
    private bool hasHiddenObjects;

    private void Awake()
    {
        ResolveDialogRoot();

        if (hideOnAwake)
        {
            SetDialogVisible(false);
        }
    }

    private void Start()
    {
        if (HasAlreadyShown())
        {
            return;
        }

        MarkShown();
        StartCoroutine(ShowRoutine());
    }

    [ContextMenu("Reset One Shot")]
    public void ResetOneShot()
    {
        if (!string.IsNullOrWhiteSpace(shownKey))
        {
            PlayerPrefs.DeleteKey(shownKey);
        }
    }

    private IEnumerator ShowRoutine()
    {
        if (showDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(showDelaySeconds);
        }

        ResolveDialogRoot();
        HideObjectsForDialogue();
        SetDialogVisible(true);

        if (executeFungusBlock)
        {
            ExecuteBlock();
            yield break;
        }

        RestoreObjectsHiddenForDialogue();
    }

    private bool HasAlreadyShown()
    {
        return rememberWithPlayerPrefs &&
               !string.IsNullOrWhiteSpace(shownKey) &&
               PlayerPrefs.GetInt(shownKey, 0) == 1;
    }

    private void MarkShown()
    {
        if (!rememberWithPlayerPrefs || string.IsNullOrWhiteSpace(shownKey))
        {
            return;
        }

        PlayerPrefs.SetInt(shownKey, 1);
        PlayerPrefs.Save();
    }

    private void ResolveDialogRoot()
    {
        if (dialogRoot != null || string.IsNullOrWhiteSpace(dialogRootName))
        {
            return;
        }

        dialogRoot = FindInactiveObjectByName(dialogRootName);
    }

    private void SetDialogVisible(bool visible)
    {
        if (dialogRoot != null && dialogRoot.activeSelf != visible)
        {
            dialogRoot.SetActive(visible);
        }
    }

    private void ExecuteBlock()
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            RestoreObjectsHiddenForDialogue();
            return;
        }

        Flowchart targetFlowchart = ResolveFlowchart();
        if (targetFlowchart == null)
        {
            Debug.LogWarning($"[IndoorEntryDialogueController] No Flowchart found for block '{blockName}'.", this);
            RestoreObjectsHiddenForDialogue();
            return;
        }

        Block block = targetFlowchart.FindBlock(blockName);
        if (block == null)
        {
            Debug.LogWarning($"[IndoorEntryDialogueController] Flowchart '{targetFlowchart.name}' has no block '{blockName}'.", this);
            RestoreObjectsHiddenForDialogue();
            return;
        }

        if (!targetFlowchart.ExecuteBlock(block, 0, HandleDialogueComplete))
        {
            RestoreObjectsHiddenForDialogue();
        }
    }

    private void HandleDialogueComplete()
    {
        RestoreObjectsHiddenForDialogue();
    }

    private void HideObjectsForDialogue()
    {
        if (hideDuringDialogueObjects == null || hideDuringDialogueObjects.Length == 0)
        {
            return;
        }

        hiddenObjectPreviousStates = new bool[hideDuringDialogueObjects.Length];
        for (int i = 0; i < hideDuringDialogueObjects.Length; i++)
        {
            GameObject target = hideDuringDialogueObjects[i];
            if (target == null)
            {
                continue;
            }

            hiddenObjectPreviousStates[i] = target.activeSelf;
            target.SetActive(false);
        }

        hasHiddenObjects = true;
    }

    private void RestoreObjectsHiddenForDialogue()
    {
        if (!hasHiddenObjects || hideDuringDialogueObjects == null || hiddenObjectPreviousStates == null)
        {
            return;
        }

        int count = Mathf.Min(hideDuringDialogueObjects.Length, hiddenObjectPreviousStates.Length);
        for (int i = 0; i < count; i++)
        {
            GameObject target = hideDuringDialogueObjects[i];
            if (target != null)
            {
                target.SetActive(hiddenObjectPreviousStates[i]);
            }
        }

        hasHiddenObjects = false;
    }

    private Flowchart ResolveFlowchart()
    {
        if (flowchart != null && (string.IsNullOrWhiteSpace(blockName) || flowchart.HasBlock(blockName)))
        {
            return flowchart;
        }

        if (!string.IsNullOrWhiteSpace(fallbackFlowchartName))
        {
            GameObject namedFlowchart = GameObject.Find(fallbackFlowchartName);
            if (namedFlowchart != null && namedFlowchart.TryGetComponent(out flowchart))
            {
                return flowchart;
            }
        }

        Flowchart[] flowcharts = Object.FindObjectsByType<Flowchart>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Flowchart candidate in flowcharts)
        {
            if (candidate != null && candidate.HasBlock(blockName))
            {
                flowchart = candidate;
                return flowchart;
            }
        }

        return null;
    }

    private static GameObject FindInactiveObjectByName(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in transforms)
        {
            if (candidate != null &&
                candidate.gameObject.scene.IsValid() &&
                candidate.name == objectName)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }
}
