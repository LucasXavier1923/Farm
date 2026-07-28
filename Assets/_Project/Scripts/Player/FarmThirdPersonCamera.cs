using UnityEngine;
using UnityEngine.InputSystem;
using FarmPrototype.Farming;

namespace FarmPrototype.Player
{
    public sealed class FarmThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private string targetName = "Player";
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float distance = 44f;
        [SerializeField] private float minDistance = 12f;
        [SerializeField] private float maxDistance = 70f;
        [SerializeField] private float keyboardRotationSpeed = 90f;
        [SerializeField] private float collisionRadius = 0.35f;
        [SerializeField] private float collisionOffset = 0.1f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private Transform target;
        private FarmPlayerOwnership targetOwnership;
        private float yaw;
        private float pitch = 55f;

        public float ConfiguredSensitivity => FarmSettings.CameraSensitivity;
        public float ConfiguredZoomStep => FarmSettings.ZoomStep;
        public bool InvertVertical => FarmSettings.InvertVertical;
        public float CurrentDistance => distance;

        private void Start()
        {
            FarmSettings.EnsureLoaded();
            FindTarget();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTarget();
                return;
            }
            if (targetOwnership != null && !targetOwnership.IsLocallyControlled)
            {
                target = null;
                targetOwnership = null;
                FindTarget();
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (mouse != null && !FarmHudController.IsModalOpen)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * FarmSettings.CameraSensitivity;
                    pitch += delta.y * FarmSettings.CameraSensitivity * (FarmSettings.InvertVertical ? 1f : -1f);
                }

                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    distance = Mathf.Clamp(distance - (Mathf.Sign(scroll) * FarmSettings.ZoomStep), minDistance, maxDistance);
                }
            }

            if (keyboard != null && !FarmHudController.IsModalOpen)
            {
                if (keyboard.qKey.isPressed)
                {
                    yaw -= keyboardRotationSpeed * Time.deltaTime;
                }

                if (keyboard.eKey.isPressed)
                {
                    yaw += keyboardRotationSpeed * Time.deltaTime;
                }
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + targetOffset;
            var cameraDistance = GetUnobstructedDistance(focus, rotation);
            transform.SetPositionAndRotation(focus - (rotation * Vector3.forward * cameraDistance), rotation);
        }

        private float GetUnobstructedDistance(Vector3 focus, Quaternion rotation)
        {
            var direction = -(rotation * Vector3.forward);
            var hits = Physics.SphereCastAll(
                focus,
                collisionRadius,
                direction,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            var unobstructedDistance = distance;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(target))
                {
                    continue;
                }

                unobstructedDistance = Mathf.Min(unobstructedDistance, Mathf.Max(0.05f, hit.distance - collisionOffset));
            }

            return unobstructedDistance;
        }

        private void FindTarget()
        {
            var targetObject = GameObject.Find(targetName);
            target = targetObject != null ? targetObject.transform : null;
            if (target != null)
            {
                targetOwnership = target.GetComponent<FarmPlayerOwnership>();
                if (targetOwnership != null && !targetOwnership.IsLocallyControlled)
                {
                    target = null;
                    targetOwnership = null;
                    return;
                }
                yaw = target.eulerAngles.y;
            }
        }

        /// <summary>Future player-spawn adapters call this after assigning local ownership.</summary>
        public void SetLocalTarget(Transform localTarget)
        {
            target = localTarget;
            targetOwnership = target != null ? target.GetComponent<FarmPlayerOwnership>() : null;
            if (targetOwnership != null && !targetOwnership.IsLocallyControlled)
            {
                target = null;
                targetOwnership = null;
                return;
            }
            if (target != null) yaw = target.eulerAngles.y;
        }
    }
}
