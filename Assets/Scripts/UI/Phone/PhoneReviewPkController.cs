using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneReviewPkController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider pkSlider;
    [SerializeField] private TextMeshProUGUI goodPercentText;
    [SerializeField] private TextMeshProUGUI badPercentText;

    [Header("Result")]
    [SerializeField] private int totalVotes = 100;
    [SerializeField] private float animationSeconds = 2.2f;
    [SerializeField] private float resultDelaySeconds = 0.45f;
    [SerializeField] private string percentFormat = "{0}%";
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine pkRoutine;

    public int LastGoodCount { get; private set; }
    public int LastBadCount { get; private set; }
    public float LastGoodRatio { get; private set; } = 0.5f;
    public bool LastResultWasGood => LastGoodCount > LastBadCount;
    public bool IsRunning { get; private set; }

    public event Action<int, int, float> PkStarted;
    public event Action<int, int, float, bool> PkCompleted;

    [ContextMenu("Auto Bind")]
    public void AutoBind()
    {
        AutoBind(transform);
    }

    public void AutoBind(Transform root)
    {
        Transform searchRoot = root != null ? root : transform;
        if (pkSlider == null)
        {
            GameObject pkObject = PhoneUiLookup.FindGameObject(searchRoot, "PK");
            pkSlider = pkObject != null ? pkObject.GetComponent<Slider>() : null;
        }
    }

    public void ResetToNeutral()
    {
        StopPk();
        LastGoodCount = Mathf.Max(0, totalVotes / 2);
        LastBadCount = Mathf.Max(0, totalVotes - LastGoodCount);
        LastGoodRatio = 0.5f;
        SetValue(0.5f);
        UpdateVoteLabels(0.5f);
    }

    public void StartRandomPk(float minimumDuration = 0f)
    {
        StopPk();
        LastGoodCount = UnityEngine.Random.Range(0, Mathf.Max(1, totalVotes) + 1);
        LastBadCount = Mathf.Max(0, totalVotes - LastGoodCount);
        LastGoodRatio = totalVotes <= 0 ? 0.5f : Mathf.Clamp01((float)LastGoodCount / totalVotes);
        IsRunning = true;
        PkStarted?.Invoke(LastGoodCount, LastBadCount, LastGoodRatio);
        pkRoutine = StartCoroutine(PkRoutine(minimumDuration));
    }

    public void StopPk()
    {
        if (pkRoutine != null)
        {
            StopCoroutine(pkRoutine);
            pkRoutine = null;
        }

        IsRunning = false;
    }

    private IEnumerator PkRoutine(float minimumDuration)
    {
        float duration = Mathf.Max(0.01f, animationSeconds);
        float totalDuration = Mathf.Max(duration, minimumDuration);
        float elapsed = 0f;

        SetValue(0.5f);
        UpdateVoteLabels(0.5f);

        while (elapsed < totalDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float value = Mathf.Lerp(0.5f, LastGoodRatio, Smooth(t));
            SetValue(value);
            UpdateVoteLabels(value);
            yield return null;
        }

        SetValue(LastGoodRatio);
        UpdateVoteLabels(LastGoodRatio);

        if (resultDelaySeconds > 0f)
        {
            yield return WaitSeconds(resultDelaySeconds);
        }

        pkRoutine = null;
        IsRunning = false;
        PkCompleted?.Invoke(LastGoodCount, LastBadCount, LastGoodRatio, LastResultWasGood);
    }

    private void SetValue(float value)
    {
        if (pkSlider != null)
        {
            pkSlider.value = Mathf.Clamp01(value);
        }
    }

    private void UpdateVoteLabels(float goodRatio)
    {
        int goodPercent = Mathf.RoundToInt(Mathf.Clamp01(goodRatio) * 100f);
        int badPercent = 100 - goodPercent;

        if (goodPercentText != null)
        {
            goodPercentText.text = string.Format(percentFormat, goodPercent);
        }

        if (badPercentText != null)
        {
            badPercentText.text = string.Format(percentFormat, badPercent);
        }
    }

    private IEnumerator WaitSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
