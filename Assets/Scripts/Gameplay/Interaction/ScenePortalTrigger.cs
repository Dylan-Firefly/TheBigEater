using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public sealed class ScenePortalTrigger : MonoBehaviour
{
    private enum PortalMode
    {
        Teleport,
        LoadScene
    }

    [Header("Portal")]
    [SerializeField] private PortalMode portalMode = PortalMode.Teleport;
    [SerializeField] private Transform targetPoint;
    [SerializeField] private string targetPointName;
    [SerializeField] private GameSceneId sceneToLoad = GameSceneId.Indoor;
    [SerializeField] private string targetSpawnPointName;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private bool useFirstChildAsPrompt = true;
    [SerializeField] private bool hidePromptOnAwake = true;

    [Header("Input")]
    [SerializeField] private KeyCode legacyInteractKey = KeyCode.F;
    [SerializeField] private bool requirePlayerTag = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Interaction Range")]
    [SerializeField] private bool useProximityFallback = true;
    [SerializeField] private float proximityDistance = 2.5f;
    [SerializeField] private Transform playerFallback;
    [SerializeField] private string playerFallbackName = "Player";

    [Header("Camera Switch")]
    [SerializeField] private bool switchCameraOnActivate;
    [SerializeField] private CinemachineCamera activeCamera;
    [SerializeField] private string activeCameraName;
    [SerializeField] private CinemachineCamera inactiveCamera;
    [SerializeField] private string inactiveCameraName;
    [SerializeField] private int activeCameraPriority = 20;
    [SerializeField] private int inactiveCameraPriority;

    private Transform playerInRange;
    private Collider2D interactionCollider;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider2D>();
        ResolvePrompt();

        if (hidePromptOnAwake)
        {
            SetPromptVisible(false);
        }
    }

    private void Update()
    {
        RefreshProximityFallback();

        if (playerInRange != null && WasInteractPressed())
        {
            ActivatePortal(playerInRange);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInRange = ResolvePlayerTransform(other);
        SetPromptVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Transform exitingPlayer = ResolvePlayerTransform(other);
        if (playerInRange == null || exitingPlayer != playerInRange)
        {
            return;
        }

        playerInRange = null;
        SetPromptVisible(false);
    }

    private void ActivatePortal(Transform player)
    {
        AudioManager.PlayGenericButton();

        if (portalMode == PortalMode.LoadScene)
        {
            LoadScene();
            return;
        }

        Transform point = ResolveTargetPoint();
        if (point == null)
        {
            Debug.LogWarning($"[ScenePortalTrigger] Missing target point on {name}.", this);
            return;
        }

        Vector3 targetPosition = point.position;
        targetPosition.z = player.position.z;
        Vector3 startPosition = player.position;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.position = new Vector2(targetPosition.x, targetPosition.y);
            player.position = targetPosition;
            Physics2D.SyncTransforms();
        }
        else
        {
            player.position = targetPosition;
        }

        ApplyCameraSwitch(player, targetPosition - startPosition);
        playerInRange = null;
        SetPromptVisible(false);
    }

    private void LoadScene()
    {
        SceneSpawnSystem.SetNextSpawnPoint(targetSpawnPointName);

        if (sceneToLoad == GameSceneId.Indoor)
        {
            GameManager.EnsureInstance().EnterIndoorFlow();
            return;
        }

        SceneLoader loader = GameManager.EnsureInstance().GetComponent<SceneLoader>();
        if (loader != null)
        {
            loader.Load(sceneToLoad);
        }
    }

    private Transform ResolveTargetPoint()
    {
        if (targetPoint != null)
        {
            return targetPoint;
        }

        if (string.IsNullOrWhiteSpace(targetPointName))
        {
            return null;
        }

        GameObject targetObject = GameObject.Find(targetPointName);
        if (targetObject != null)
        {
            targetPoint = targetObject.transform;
        }

        return targetPoint;
    }

    private void ResolvePrompt()
    {
        if (promptRoot != null || !useFirstChildAsPrompt || transform.childCount == 0)
        {
            return;
        }

        promptRoot = transform.GetChild(0).gameObject;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null && promptRoot.activeSelf != visible)
        {
            promptRoot.SetActive(visible);
        }
    }

    private bool IsPlayer(Collider2D other)
    {
        return ResolvePlayerTransform(other) != null;
    }

    private void RefreshProximityFallback()
    {
        if (!useProximityFallback)
        {
            return;
        }

        Transform player = ResolveFallbackPlayer();
        if (player == null)
        {
            return;
        }

        bool isClose = IsCloseEnough(player);
        if (isClose)
        {
            playerInRange = player;
            SetPromptVisible(true);
            return;
        }

        if (playerInRange == player)
        {
            playerInRange = null;
            SetPromptVisible(false);
        }
    }

    private Transform ResolveFallbackPlayer()
    {
        if (playerInRange != null)
        {
            return playerInRange;
        }

        if (playerFallback != null)
        {
            return playerFallback;
        }

        GameObject playerObject = null;
        if (requirePlayerTag && !string.IsNullOrWhiteSpace(playerTag))
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
        }

        if (playerObject == null && !string.IsNullOrWhiteSpace(playerFallbackName))
        {
            playerObject = GameObject.Find(playerFallbackName);
        }

        if (playerObject != null)
        {
            playerFallback = playerObject.transform;
        }

        return playerFallback;
    }

    private bool IsCloseEnough(Transform player)
    {
        if (proximityDistance <= 0f)
        {
            return false;
        }

        Vector2 playerPosition = player.position;
        Vector2 referencePosition = transform.position;
        if (interactionCollider != null)
        {
            referencePosition = interactionCollider.ClosestPoint(playerPosition);
        }

        return Vector2.Distance(playerPosition, referencePosition) <= proximityDistance;
    }

    private Transform ResolvePlayerTransform(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        if (!requirePlayerTag)
        {
            return other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        }

        if (other.CompareTag(playerTag))
        {
            return other.transform;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
        {
            return other.attachedRigidbody.transform;
        }

        Transform parent = other.transform.parent;
        while (parent != null)
        {
            if (parent.CompareTag(playerTag))
            {
                return parent;
            }

            parent = parent.parent;
        }

        return null;
    }

    private void ApplyCameraSwitch(Transform warpedTarget, Vector3 warpDelta)
    {
        if (!switchCameraOnActivate)
        {
            return;
        }

        CinemachineCamera active = ResolveCamera(ref activeCamera, activeCameraName);
        ResolveCamera(ref inactiveCamera, inactiveCameraName);
        if (active == null)
        {
            return;
        }

        CinemachineCamera[] cameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (CinemachineCamera camera in cameras)
        {
            camera.Priority = camera == active ? activeCameraPriority : inactiveCameraPriority;
        }

        active.Follow = warpedTarget;
        active.Priority = activeCameraPriority;
        active.MoveToTopOfPrioritySubqueue();
        active.OnTargetObjectWarped(warpedTarget, warpDelta);
        active.PreviousStateIsValid = false;

        if (active.TryGetComponent(out CinemachineConfiner2D confiner))
        {
            confiner.InvalidateBoundingShapeCache();
            confiner.InvalidateLensCache();
            confiner.BakeBoundingShape(active, 0.05f);
        }

        CutActiveCinemachineBlend();
    }

    private static void CutActiveCinemachineBlend()
    {
        for (int i = 0; i < CinemachineBrain.ActiveBrainCount; i++)
        {
            CinemachineBrain brain = CinemachineBrain.GetActiveBrain(i);
            if (brain == null)
            {
                continue;
            }

            brain.ActiveBlend = null;
            brain.ResetState();
        }
    }

    private static CinemachineCamera ResolveCamera(ref CinemachineCamera camera, string cameraName)
    {
        if (camera != null)
        {
            return camera;
        }

        if (string.IsNullOrWhiteSpace(cameraName))
        {
            return null;
        }

        GameObject cameraObject = GameObject.Find(cameraName);
        if (cameraObject != null)
        {
            camera = cameraObject.GetComponent<CinemachineCamera>();
        }

        return camera;
    }

    private bool WasInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(legacyInteractKey))
        {
            return true;
        }
#endif

        return false;
    }
}
