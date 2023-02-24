using System;
using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Character.Config;
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Dodge action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(IJumping))]
    [RequireComponent(typeof(KCCMovementEngine))]
    public class JumpActionBehaviour : AbstractActionBehaviour<JumpActorAction>
    {
        /// <summary>
        /// Velocity of player jump.
        /// </summary>
        [Tooltip("Velocity of player jump.")]
        [SerializeField]
        public float jumpVelocity = 6.5f;

        private IJumping _jumping;
        private IJumping Jumping => _jumping ??= GetComponent<IJumping>();

        public override JumpActorAction SetupAction()
        {
            var jumpAction = new JumpActorAction(
                BufferedInput,
                Actor,
                GetComponent<KCCMovementEngine>())
            {
                jumpVelocity = jumpVelocity,
                maxJumpAngle = 85.0f,
                jumpAngleWeightFactor = 0.0f,
            };
            jumpAction.OnPerform += OnJump;
            return jumpAction;
        }

        private void OnJump(object source, EventArgs args)
        {
            Jumping.ApplyJump(jumpVelocity * Action.JumpDirection());
        }

        public override void CleanupAction(JumpActorAction action)
        {
            action.OnPerform -= OnJump;
        }
    }
}