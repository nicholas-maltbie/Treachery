
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.OpenKCC.Utils;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Player
{
    [RequireComponent(typeof(ICameraControls))]
    [RequireComponent(typeof(IMovementActor))]
    public class RotateTowardsMovement : NetworkBehaviour
    {
        /// <summary>
        /// Rotation rate in degrees per second.
        /// </summary>
        public float rotationRate = 30.0f;

        /// <summary>
        /// Avatar base to rotate.
        /// </summary>
        public GameObject avatarBase;
        public bool Locked { get; set; }

        private float initialTargetRotationHeading;
        private float overrideTargetRotationHeading;
        private float totalTimeOverrideTarget;
        private float remainingTimeOverrideTarget;

        private IMovementActor actor;
        private ICameraControls cameraControls;

        private NetworkVariable<float> currentHeading = new NetworkVariable<float>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        /// <summary>
        /// Animation movement for the player
        /// </summary>
        private NetworkVariable<Vector2> animationMove = new NetworkVariable<Vector2>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);

        private Animator AttachedAnimator => avatarBase.GetComponent<Animator>();

        public void Awake()
        {
            actor = GetComponent<IMovementActor>();
            cameraControls = GetComponent<ICameraControls>();
        }

        public void SetOverrideTargetHeading(float desiredHeading, float rotationRate)
        {
            float deltaHeading = Mathf.Abs(currentHeading.Value - desiredHeading);
            SetOverrideTargetHeadingFixedTime(desiredHeading, deltaHeading / rotationRate);
        }

        public void SetOverrideTargetHeadingFixedTime(float desiredHeading, float time)
        {
            initialTargetRotationHeading = currentHeading.Value;
            overrideTargetRotationHeading = desiredHeading;
            remainingTimeOverrideTarget = time;
            totalTimeOverrideTarget = time;
        }

        public void Update()
        {
            AttachedAnimator.SetFloat("MoveX", animationMove.Value.x);
            AttachedAnimator.SetFloat("MoveY", animationMove.Value.y);
        }

        public void LateUpdate()
        {
            if (IsOwner)
            {
                if (remainingTimeOverrideTarget > 0)
                {
                    remainingTimeOverrideTarget -= Time.deltaTime;
                    remainingTimeOverrideTarget = Mathf.Max(0, remainingTimeOverrideTarget);

                    float fraction = remainingTimeOverrideTarget / totalTimeOverrideTarget;
                    Quaternion startingHeading = Quaternion.AngleAxis(initialTargetRotationHeading, Vector3.up);
                    Quaternion desiredHeading = Quaternion.AngleAxis(overrideTargetRotationHeading, Vector3.up);
                    Quaternion lerpedHeading = Quaternion.Lerp(startingHeading, desiredHeading, Mathf.Sqrt(1 - fraction));   
                    currentHeading.Value = lerpedHeading.eulerAngles.y;
                }
                else if (!Locked && actor.InputMovement.magnitude > KCCUtils.Epsilon)
                {
                    // Rotate the avatar to follow the direction of the
                    // player's movement if it's grater than some dead zone
                    // with a given turning rate.
                    // Rotate the current heading towards the desired heading.
                    var desiredHeading = Quaternion.LookRotation(cameraControls.PlayerHeading * actor.InputMovement);
                    var rotatedHeading = Quaternion.RotateTowards(avatarBase.transform.rotation, desiredHeading, rotationRate * Time.deltaTime);
                    currentHeading.Value = rotatedHeading.eulerAngles.y;
                }
            }
            // set the avatar rotation to be the new rotated heading.
            avatarBase.transform.rotation = Quaternion.AngleAxis(currentHeading.Value, Vector3.up);
        }

        public void UpdateAnimationState(Vector3 movementDir, bool smooth = true)
        {
            // Get the relative moveX and moveY to include
            // the delta in rotation between the avatar's current heading
            // and the desired world space input
            Vector3 playerHeading = AttachedAnimator.transform.forward;
            var relative = Quaternion.FromToRotation(playerHeading, movementDir);
            Vector3 relativeMovement = relative * Vector3.forward;
            AttachedAnimator.SetFloat("MoveX", relativeMovement.x);
            AttachedAnimator.SetFloat("MoveY", relativeMovement.z);
            animationMove.Value = new Vector2(relativeMovement.x, relativeMovement.z);
        }
    }
}