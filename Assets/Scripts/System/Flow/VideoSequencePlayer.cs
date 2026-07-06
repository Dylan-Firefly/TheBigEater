using System;
using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class VideoSequencePlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Fallback")]
    [SerializeField] private bool completeImmediatelyWhenNoClip = true;

    public event Action Completed;

    private bool isPlaying;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
        }

        SetRootVisible(false);
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += HandleVideoFinished;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
        }
    }

    public void Play()
    {
        SetRootVisible(true);

        if (videoPlayer == null || videoPlayer.clip == null)
        {
            if (completeImmediatelyWhenNoClip)
            {
                Complete();
            }

            return;
        }

        isPlaying = true;
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    public void Stop()
    {
        isPlaying = false;
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        SetRootVisible(false);
    }

    private void HandleVideoFinished(VideoPlayer player)
    {
        if (isPlaying)
        {
            Complete();
        }
    }

    private void Complete()
    {
        isPlaying = false;
        SetRootVisible(false);
        Completed?.Invoke();
    }

    private void SetRootVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
        }
    }
}
