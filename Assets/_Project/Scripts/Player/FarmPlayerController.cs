using UnityEngine;
using UnityEngine.InputSystem;
using FarmPrototype.Farming;

namespace FarmPrototype.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public sealed class FarmPlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float rotationSmoothTime = 0.08f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float groundProbeDistance = 0.5f;
        [SerializeField, Range(0.005f, 0.08f)] private float groundedSkinWidth = 0.02f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int AirborneHash = Animator.StringToHash("Airborne");

        private CharacterController characterController;
        private Animator animator;
        private FarmPlayerOwnership ownership;
        private float verticalVelocity;
        private float rotationVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            characterController.skinWidth = Mathf.Clamp(groundedSkinWidth, 0.005f, characterController.radius * 0.5f);
            animator = GetComponent<Animator>();
            ownership = GetComponent<FarmPlayerOwnership>();
            if (ownership == null) ownership = gameObject.AddComponent<FarmPlayerOwnership>();
            animator.applyRootMotion = false;
        }

        private void Update()
        {
            if (ownership != null && !ownership.IsLocallyControlled)
            {
                animator.SetFloat(SpeedHash, 0f);
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                animator.SetFloat(SpeedHash, 0f);
                return;
            }

            if (FarmHudController.IsModalOpen)
            {
                animator.SetFloat(SpeedHash, 0f);
                return;
            }

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            var cameraTransform = Camera.main != null ? Camera.main.transform : transform;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var movement = (forward * input.y) + (right * input.x);
            var isMoving = movement.sqrMagnitude > 0.001f;
            var isSprinting = keyboard.leftShiftKey.isPressed;

            if (isMoving)
            {
                var targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
                var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            var speed = isSprinting ? sprintSpeed : walkSpeed;
            characterController.Move(movement * (speed * Time.deltaTime));
            animator.SetFloat(SpeedHash, isMoving ? (isSprinting ? 1f : 0.5f) : 0f, 0.08f, Time.deltaTime);

            var grounded = IsGrounded();
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (grounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetBool(AirborneHash, true);
                animator.SetTrigger(JumpHash);
            }

            verticalVelocity += gravity * Time.deltaTime;
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
            animator.SetBool(AirborneHash, !IsGrounded());
        }

        private bool IsGrounded()
        {
            if (characterController.isGrounded)
            {
                return true;
            }

            var origin = transform.position + (Vector3.up * 0.3f);
            foreach (var hit in Physics.RaycastAll(origin, Vector3.down, groundProbeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
