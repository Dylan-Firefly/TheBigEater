using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class VillageIntroCam : MonoBehaviour
{
    private const string DefaultPlayedKey = "VillageIntroCam.Played";

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform housePoint;
    [SerializeField] private GameObject houseEnterButton;
    [SerializeField] private float duration = 1.2f;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera mainStreetCamera;
    [SerializeField] private CinemachineCamera marketCamera;
    [SerializeField] private int introPriority = 30;
    [SerializeField] private int gameplayPriority = 20;
    [SerializeField] private int inactivePriority;

    [SerializeField] private bool rememberPlayed = true;
    [SerializeField] private string playedKey = DefaultPlayedKey;

    private void Awake()
    {
        ResolveCameras();
        PrimeInitialCameraState();
    }

    private IEnumerator Start()
    {
        if (ShouldSkipIntro())
        {
            SkipToEnd();
            yield break;
        }

        if (!CanPlay())
        {
            yield break;
        }

        houseEnterButton.SetActive(false);
        SetCameraPriorities(introPriority, inactivePriority, inactivePriority);

        cameraTarget.position = startPoint.position;

        if (duration <= 0f)
        {
            cameraTarget.position = housePoint.position;
            FinishIntro();
            MarkPlayed();
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            cameraTarget.position = Vector3.Lerp(startPoint.position, housePoint.position, p);
            yield return null;
        }

        cameraTarget.position = housePoint.position;
        FinishIntro();
        MarkPlayed();
    }

    private bool CanPlay()
    {
        return cameraTarget != null &&
               startPoint != null &&
               housePoint != null &&
               houseEnterButton != null;
    }

    private bool HasPlayed()
    {
        return rememberPlayed && PlayerPrefs.GetInt(GetPlayedKey(), 0) == 1;
    }

    private void MarkPlayed()
    {
        if (!rememberPlayed)
        {
            return;
        }

        PlayerPrefs.SetInt(GetPlayedKey(), 1);
        PlayerPrefs.Save();
    }

    private void SkipToEnd()
    {
        if (cameraTarget != null && housePoint != null)
        {
            cameraTarget.position = housePoint.position;
        }

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (marketCamera != null && marketCamera.Priority > inactivePriority)
        {
            if (introCamera != null)
            {
                introCamera.Priority = inactivePriority;
            }

            if (houseEnterButton != null)
            {
                houseEnterButton.SetActive(true);
            }

            return;
        }

        SetCameraPriorities(inactivePriority, gameplayPriority, inactivePriority);

        if (houseEnterButton != null)
        {
            houseEnterButton.SetActive(true);
        }
    }

    private void PrimeInitialCameraState()
    {
        if (!CanPlay())
        {
            return;
        }

        if (ShouldSkipIntro())
        {
            cameraTarget.position = housePoint.position;
            SetCameraPriorities(inactivePriority, gameplayPriority, inactivePriority);
            houseEnterButton.SetActive(true);
            return;
        }

        cameraTarget.position = startPoint.position;
        SetCameraPriorities(introPriority, inactivePriority, inactivePriority);
        houseEnterButton.SetActive(false);
    }

    private string GetPlayedKey()
    {
        return string.IsNullOrWhiteSpace(playedKey) ? DefaultPlayedKey : playedKey;
    }

    private bool ShouldSkipIntro()
    {
        return HasPlayed() || SceneSpawnSystem.HasPendingSpawnPoint || SceneSpawnSystem.LastLoadUsedSpawn;
    }

    [ContextMenu("Reset Played State")]
    private void ResetPlayedState()
    {
        PlayerPrefs.DeleteKey(GetPlayedKey());
    }

    private void ResolveCameras()
    {
        if (introCamera == null)
        {
            introCamera = GetComponentInChildren<CinemachineCamera>(true);
        }

        if (mainStreetCamera == null)
        {
            mainStreetCamera = FindCamera("MainStreetCinemachineCamera");
        }

        if (marketCamera == null)
        {
            marketCamera = FindCamera("MarketCinemachineCamera");
        }
    }

    private void SetCameraPriorities(int intro, int mainStreet, int market)
    {
        if (introCamera != null)
        {
            introCamera.Priority = intro;
        }

        if (mainStreetCamera != null)
        {
            mainStreetCamera.Priority = mainStreet;
        }

        if (marketCamera != null)
        {
            marketCamera.Priority = market;
        }
    }

    private static CinemachineCamera FindCamera(string cameraName)
    {
        GameObject cameraObject = GameObject.Find(cameraName);
        return cameraObject != null ? cameraObject.GetComponent<CinemachineCamera>() : null;
    }
}
