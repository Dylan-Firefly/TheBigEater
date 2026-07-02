using UnityEngine;
using UnityEngine.UI;

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

    private int clickTriggerHash;
    private int clickStateHash;
    private int clickStateFullPathHash;

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

    private void OnDestroy()
    {
        if (bindButtonOnAwake && button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void HandleClick()
    {
        PlayClickAnimation();
        EmitEffects();
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
