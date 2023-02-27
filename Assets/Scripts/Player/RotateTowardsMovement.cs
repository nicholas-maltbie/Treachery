
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
        
        private IMovementActor actor;
        private ICameraControls cameraControls;

        private NetworkVariable<float> currentHeading = new NetworkVariable<float>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner
        );

        public void Awake()
        {
            actor = GetComponent<IMovementActor>();
            cameraControls = GetComponent<ICameraControls>();
        }

        public void LateUpdate()
        {
            // Rotate the avatar to follow the direction of the
            // player's movement if it's grater than some dead zone
            // with a given turning rate.
            if (actor.InputMovement.magnitude > KCCUtils.Epsilon)
            {
                Quaternion currentHeading = avatarBase.transform.rotation;
                var desiredHeading = Quaternion.LookRotation(cameraControls.PlayerHeading * actor.InputMovement);

                // Rotate the current heading towards the desired heading.
                var rotatedHeading = Quaternion.RotateTowards(currentHeading, desiredHeading, rotationRate * Time.deltaTime);

                // set the avatar rotation to be the new rotated heading.
                avatarBase.transform.rotation = rotatedHeading;
            }
        }
    }
}