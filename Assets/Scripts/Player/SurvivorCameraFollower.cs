using nickmaltbie.OpenKCC.CameraControls;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Player
{
    /// <summary>
    /// Script to move main camera to follow the local player
    /// </summary>
    [RequireComponent(typeof(SurvivorCameraController))]
    public class SurvivorCameraFollower : NetworkBehaviour
    {
        /// <summary>
        /// Position and rotation to control camera position and movement
        /// </summary>
        private SurvivorCameraController cameraController;

        /// <summary>
        /// AudioListener for moving listening position
        /// </summary>
        private AudioListener audioListener;

        public void Start()
        {
            cameraController = GetComponent<SurvivorCameraController>();
            audioListener = GameObject.FindObjectOfType<AudioListener>();
        }

        public void LateUpdate()
        {
            if (IsOwner)
            {
                CameraFollow.MoveCamera(cameraController.config.cameraTransform, audioListener);
            }
        }
    }
}