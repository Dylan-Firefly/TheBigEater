using System;
using UnityEngine;

[DisallowMultipleComponent]
public class FungusDialogueBridge : MonoBehaviour
{
    [Header("Fungus")]
    [SerializeField] private Fungus.Flowchart flowchart;
    [SerializeField] private string startBlockName = "Start";

    public event Action Completed;

    private void Awake()
    {
        if (flowchart == null)
        {
            flowchart = GetComponentInChildren<Fungus.Flowchart>(true);
        }
    }

    public void Play()
    {
        if (flowchart == null)
        {
            Debug.LogWarning("[FungusDialogueBridge] Missing Flowchart. Dialogue completes immediately.");
            Completed?.Invoke();
            return;
        }

        Fungus.Block block = flowchart.FindBlock(startBlockName);
        if (block == null)
        {
            Debug.LogWarning($"[FungusDialogueBridge] Block '{startBlockName}' not found. Dialogue completes immediately.");
            Completed?.Invoke();
            return;
        }

        if (!flowchart.ExecuteBlock(block, 0, HandleDialogueFinished))
        {
            Completed?.Invoke();
        }
    }

    public void CompleteFromFungusEvent()
    {
        Completed?.Invoke();
    }

    private void HandleDialogueFinished()
    {
        Completed?.Invoke();
    }
}
