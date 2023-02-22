// Copyright (C) 2023 Nicholas Maltbie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Character.Config;
using nickmaltbie.OpenKCC.Input;
using UnityEngine;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Jump action that can be performed by a character controller.
    /// </summary>
    public class JumpActorAction : ActorConditionalAction<PlayerAction>
    {
        /// <summary>
        /// Velocity of player jump in units per second.
        /// </summary>
        public float jumpVelocity = 5.0f;

        /// <summary>
        /// Maximum angle at which the player can jump (in degrees).
        /// </summary>
        public float maxJumpAngle = 85f;

        /// <summary>
        /// Weight to which the player's jump is weighted towards the direction
        /// of the surface they are standing on.
        /// </summary>
        public float jumpAngleWeightFactor = 0.0f;

        /// <summary>
        /// MovementEngine for the player.
        /// </summary>
        private KCCMovementEngine movementEngine;

        public JumpActorAction(
            BufferedInput bufferedInput,
            IActionActor<PlayerAction> actor,
            KCCMovementEngine movementEngine)
            : base(bufferedInput, actor, PlayerAction.Jump)
        {
            this.movementEngine = movementEngine;
            JumpedWhileSliding = false;
        }

        /// <summary>
        /// Has the player jumped while they are sliding.
        /// </summary>
        public bool JumpedWhileSliding { get; private set; }

        /// <inheritdoc/>
        public override void Update()
        {
            KCCGroundedState kccGrounded = movementEngine.GroundedState;
            base.Update();
            if (kccGrounded.StandingOnGround && !kccGrounded.Sliding)
            {
                JumpedWhileSliding = false;
            }
        }

        /// <summary>
        /// Apply the jump to the player.
        /// </summary>
        protected override void Perform()
        {
            KCCGroundedState kccGrounded = movementEngine.GroundedState;
            if (kccGrounded.Sliding)
            {
                JumpedWhileSliding = true;
            }
        }

        public Vector3 JumpDirection()
        {
            KCCGroundedState kccGrounded = movementEngine.GroundedState;
            Vector3 normal = (kccGrounded.StandingOnGround ? kccGrounded.SurfaceNormal : movementEngine.Up);
            Vector3 weighted = normal * jumpAngleWeightFactor + movementEngine.Up * (1 - jumpAngleWeightFactor);
            return weighted.normalized;
        }

        /// <summary>
        /// Can the player jump based on their current state.
        /// </summary>
        /// <returns>True if the player can jump, false otherwise.</returns>
        protected override bool CanPerform()
        {
            KCCGroundedState kccGrounded = movementEngine.GroundedState;
            bool canJump = kccGrounded.StandingOnGround && kccGrounded.Angle <= maxJumpAngle;
            if (canJump && !kccGrounded.Sliding)
            {
                return base.CanPerform();
            }
            else if (canJump)
            {
                return !JumpedWhileSliding && base.CanPerform();
            }

            return false;
        }
    }
}
