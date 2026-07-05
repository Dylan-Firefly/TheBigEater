using UnityEngine;

namespace TheBigEater.Gameplay.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private bool flipSpriteByHorizontalInput = true;

        private global::InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new global::InputSystem_Actions();
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            inputActions?.Player.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Player.Disable();
        }

        private void OnDestroy()
        {
            inputActions?.Dispose();
        }

        private void Update()
        {
            Vector2 moveInput = ReadMoveInput();
            Move(moveInput);
            UpdateAnimation(moveInput);
            UpdateFacing(moveInput);
        }

        private Vector2 ReadMoveInput()
        {
            Vector2 moveInput = inputActions == null ? Vector2.zero : inputActions.Player.Move.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            return moveInput;
        }

        private void Move(Vector2 moveInput)
        {
            transform.position += new Vector3(moveInput.x, moveInput.y, 0f) * (moveSpeed * Time.deltaTime);
        }

        private void UpdateAnimation(Vector2 moveInput)
        {
            if (animator != null && !string.IsNullOrEmpty(speedParameter))
            {
                animator.SetFloat(speedParameter, moveInput.magnitude);
            }
        }

        private void UpdateFacing(Vector2 moveInput)
        {
            if (flipSpriteByHorizontalInput && spriteRenderer != null && Mathf.Abs(moveInput.x) > 0.01f)
            {
                spriteRenderer.flipX = moveInput.x < 0f;
            }
        }
    }
}
