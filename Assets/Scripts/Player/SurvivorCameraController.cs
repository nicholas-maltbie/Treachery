
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.OpenKCC.CameraControls.Config;
using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Utils;
using nickmaltbie.TestUtilsUnity;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Player
{
    public class SurvivorCameraController : NetworkBehaviour, ICameraControls, IManagedCamera
    {
        /// <summary>
        /// Unity service associated with this game object for testing.
        /// </summary>
        public IUnityService unityService = UnityService.Instance;

        /// <summary>
        /// Camera config associated wit this camera controller.
        /// </summary>
        [SerializeField]
        public CameraConfig config = new CameraConfig();

        /// <summary>
        /// Where is the player looking right now.
        /// </summary>
        private NetworkVariable<Vector2> playerView = new NetworkVariable<Vector2>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        /// <inheritdoc/>
        public float PreviousOpacity { get; set; }

        /// <inheritdoc/>
        public float Pitch
        {
            get => playerView.Value.x;
            set => playerView.Value = new Vector2(value, Yaw);
        }

        /// <inheritdoc/>
        public float Yaw
        {
            get => playerView.Value.y;
            set => playerView.Value = new Vector2(Pitch, value);
        }

        /// <inheritdoc/>
        public Quaternion PlayerHeading => Quaternion.Euler(0, Yaw, 0);

        /// <inheritdoc/>
        public Transform CameraBase => config.cameraTransform;

        public void Start()
        {
            config.Setup(gameObject);
        }

        public void Update()
        {
            if (IsOwner)
            {
                UpdateCameraController(config, gameObject, this, unityService.deltaTime);
            }
        }

        
        /// <summary>
        /// Update a camera controller based on current configuration
        /// and inputs.
        /// </summary>
        /// <param name="config">Configuration for the camear controller.</param>
        /// <param name="go">GameObject associated with the camera controller.</param>
        /// <param name="cameraControls">Camera controls object.</param>
        /// <param name="deltaTime">delta time for updating the camera controller.</param>
        public void UpdateCameraController(
            CameraConfig config,
            GameObject go,
            IManagedCamera cameraControls,
            float deltaTime)
        {
            Vector2 look = config.LookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            look *= PlayerInputUtils.mouseSensitivity;
            float yawChange = look.x;
            float pitchChange = look.y;

            float zoomAxis = config.ZoomAction?.ReadValue<Vector2>().y ?? 0;

            // bound pitch between -180 and 180
            float zoomChange = 0;
            cameraControls.Pitch = (cameraControls.Pitch % 360 + 180) % 360 - 180;

            // Only allow rotation if player is allowed to move
            if (PlayerInputUtils.playerMovementState == PlayerInputState.Allow)
            {
                yawChange = config.rotationRate * deltaTime * yawChange;
                cameraControls.Yaw += yawChange;
                cameraControls.Pitch += config.rotationRate * deltaTime * -1 * pitchChange;
                zoomChange = config.zoomSpeed * deltaTime * -1 * zoomAxis;
            }

            // Clamp rotation of camera between minimum and maximum specified pitch
            cameraControls.Pitch = Mathf.Clamp(cameraControls.Pitch, config.minPitch, config.maxPitch);

            // Change camera zoom by desired level
            // Bound the current distance between minimum and maximum
            config.currentDistance = Mathf.Clamp(config.currentDistance + zoomChange, config.minCameraDistance, config.maxCameraDistance);

            if (config.cameraTransform != null)
            {
                // Set the player's rotation to be that of the camera's yaw
                // transform.rotation = Quaternion.Euler(0, yaw, 0);
                // Set pitch to be camera's rotation
                config.cameraTransform.rotation = Quaternion.Euler(cameraControls.Pitch, cameraControls.Yaw, 0);

                // Set the local position of the camera to be the current rotation projected
                //   backwards by the current distance of the camera from the player
                Vector3 cameraDirection = -config.cameraTransform.forward * config.currentDistance;
                Vector3 cameraSource = config.CameraSource(go.transform);

                // Draw a line from our camera source in the camera direction. If the line hits anything that isn't us
                // Limit the distance by how far away that object is
                // If we hit something
                if (PhysicsUtils.SphereCastFirstHitIgnore(config.IgnoreObjects, cameraSource, 0.01f, cameraDirection, cameraDirection.magnitude,
                    config.cameraRaycastMask, QueryTriggerInteraction.Ignore, out RaycastHit hit))
                {
                    // limit the movement by that hit
                    cameraDirection = cameraDirection.normalized * hit.distance;
                }

                config.CameraDistance = cameraDirection.magnitude;
                config.cameraTransform.position = cameraSource + cameraDirection;

                if (config.thirdPersonCharacterBase != null)
                {
                    UpdateThirdPersonCameraBase(cameraDirection, config, go, cameraControls, deltaTime);
                }
            }

            cameraControls.Yaw %= 360;
        }

        /// <summary>
        /// Update the third person camera base for a camera controller.
        /// </summary>
        /// <param name="cameraDirection">Direction the camera is relative to the player.</param>
        /// <param name="config">Config associated with the player.</param>
        /// <param name="go">Game object for the character position.</param>
        /// <param name="cameraControls">Camera controls associated with the player.</param>
        /// <param name="deltaTime">delta time for updating player opacity.</param>
        public void UpdateThirdPersonCameraBase(
            Vector3 cameraDirection,
            CameraConfig config,
            GameObject go,
            IManagedCamera cameraControls,
            float deltaTime)
        {
            bool hittingSelf = PhysicsUtils.SphereCastAllow(go, config.CameraSource(go.transform) + cameraDirection, 0.01f, -cameraDirection.normalized,
                cameraDirection.magnitude, ~0, QueryTriggerInteraction.Ignore, out RaycastHit selfHit);
            float actualDistance = hittingSelf ? selfHit.distance : cameraDirection.magnitude;
            if (actualDistance < config.shadowOnlyDistance)
            {
                MaterialUtils.RecursiveSetShadowCastingMode(config.thirdPersonCharacterBase, UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
            }
            else
            {
                MaterialUtils.RecursiveSetShadowCastingMode(config.thirdPersonCharacterBase, UnityEngine.Rendering.ShadowCastingMode.On);
            }

            if (actualDistance > config.shadowOnlyDistance && actualDistance < config.ditherDistance)
            {
                float newOpacity = (actualDistance - config.shadowOnlyDistance) / (config.ditherDistance - config.minCameraDistance);
                float lerpPosition = config.transitionTime > 0 ? deltaTime * 1 / config.transitionTime : 1;
                cameraControls.PreviousOpacity = Mathf.Lerp(cameraControls.PreviousOpacity, newOpacity, lerpPosition);
                // Set opacity of character based on how close the camera is
                MaterialUtils.RecursiveSetFloatProperty(config.thirdPersonCharacterBase, "_Opacity", cameraControls.PreviousOpacity);
            }
            else
            {
                // Set opacity of character based on how close the camera is
                MaterialUtils.RecursiveSetFloatProperty(config.thirdPersonCharacterBase, "_Opacity", 1);
                cameraControls.PreviousOpacity = actualDistance > config.shadowOnlyDistance ? 1 : 0;
            }
        }
    }
}
