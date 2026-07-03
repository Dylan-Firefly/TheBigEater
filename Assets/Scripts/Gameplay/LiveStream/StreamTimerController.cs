using System;
using UnityEngine;
using UnityEngine.UI;

public class StreamTimerController : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float defaultDurationSeconds = 20f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool resetSliderOnStop = true;

    private float durationSeconds;
    private float elapsedSeconds;
    private bool isRunning;
    private bool hasFinished;

    public float NormalizedProgress => durationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / durationSeconds);
    public float RemainingSeconds => Mathf.Max(0f, durationSeconds - elapsedSeconds);
    public bool IsRunning => isRunning;

    public event Action TimerFinished;
    public event Action<float> ProgressChanged;

    private void Awake()
    {
        if (slider == null)
        {
            slider = GetComponentInChildren<Slider>(true);
        }

        SetProgress(0f);
    }

    private void Update()
    {
        if (!isRunning || hasFinished)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsedSeconds += deltaTime;
        SetProgress(NormalizedProgress);

        if (elapsedSeconds >= durationSeconds)
        {
            hasFinished = true;
            isRunning = false;
            SetProgress(1f);
            TimerFinished?.Invoke();
        }
    }

    public void StartTimer()
    {
        StartTimer(defaultDurationSeconds);
    }

    public void StartTimer(float seconds)
    {
        durationSeconds = Mathf.Max(0.01f, seconds);
        elapsedSeconds = 0f;
        hasFinished = false;
        isRunning = true;
        SetProgress(0f);
    }

    public void Pause()
    {
        isRunning = false;
    }

    public void Resume()
    {
        if (!hasFinished)
        {
            isRunning = true;
        }
    }

    public void StopTimer()
    {
        isRunning = false;
        hasFinished = false;
        if (resetSliderOnStop)
        {
            elapsedSeconds = 0f;
            SetProgress(0f);
        }
    }

    private void SetProgress(float value)
    {
        if (slider != null)
        {
            slider.value = value;
        }

        ProgressChanged?.Invoke(value);
    }
}
