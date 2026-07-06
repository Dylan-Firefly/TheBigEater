using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BtnClick : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;
    [SerializeField] private bool bindButtonOnAwake = true;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private int animationLayerIndex;
    [SerializeField] private string clickStateName = "Click";
    [SerializeField] private string clickTriggerName = "Click";
    [SerializeField] private bool replayStateFromStart = true;

    [Header("Effects")]
    [SerializeField] private FloatingFxEmitter[] fxEmitters;

    [Header("Counter")]
    [SerializeField] private bool incrementTextOnClick;
    [SerializeField] private TMP_Text counterText;
    [SerializeField] private string counterPrefix = "x";
    [SerializeField] private int counterStartValue = 666;
    [SerializeField] private bool resetCounterOnEnable = true;

    private int clickTriggerHash;
    private int clickStateHash;
    private int clickStateFullPathHash;
    private int currentCounterValue;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        clickTriggerHash = Animator.StringToHash(clickTriggerName);
        clickStateHash = Animator.StringToHash(clickStateName);
        clickStateFullPathHash = Animator.StringToHash("Base Layer." + clickStateName);

        if (bindButtonOnAwake && button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
    }

    private void OnEnable()
    {
        if (resetCounterOnEnable)
        {
            currentCounterValue = counterStartValue;
            RefreshCounterText();
        }
    }

    private void OnDestroy()
    {
        if (bindButtonOnAwake && button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void HandleClick()
    {
        AudioManager.PlayPop();
        IncrementCounter();
        PlayClickAnimation();
        EmitEffects();
    }

    private void IncrementCounter()
    {
        if (!incrementTextOnClick)
        {
            return;
        }

        currentCounterValue++;
        RefreshCounterText();
    }

    private void RefreshCounterText()
    {
        if (!incrementTextOnClick || counterText == null)
        {
            return;
        }

        counterText.text = counterPrefix + currentCounterValue;
    }

    private void PlayClickAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (replayStateFromStart && !IsBlank(clickStateName))
        {
            if (animator.HasState(animationLayerIndex, clickStateHash))
            {
                animator.Play(clickStateHash, animationLayerIndex, 0f);
                animator.Update(0f);
                return;
            }

            if (animator.HasState(animationLayerIndex, clickStateFullPathHash))
            {
                animator.Play(clickStateFullPathHash, animationLayerIndex, 0f);
                animator.Update(0f);
                return;
            }
        }

        if (!IsBlank(clickTriggerName))
        {
            animator.ResetTrigger(clickTriggerHash);
            animator.SetTrigger(clickTriggerHash);
        }
    }

    private void EmitEffects()
    {
        if (fxEmitters == null)
        {
            return;
        }

        foreach (FloatingFxEmitter emitter in fxEmitters)
        {
            if (emitter != null)
            {
                emitter.EmitBurst();
            }
        }
    }

    private static bool IsBlank(string value)
    {
        return string.IsNullOrEmpty(value) || value.Trim().Length == 0;
    }
}
