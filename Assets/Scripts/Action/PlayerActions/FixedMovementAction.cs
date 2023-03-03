
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
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    /// <summary>
    /// FixedMovement action that can be performed by a player.
    /// </summary>
    public class FixedMovementAction : TimedConditionalAction<PlayerAction>
    {
        public static float DodgeSpeedFactor(float x)
        {
            float val = (x * 0.5f) * 2;
            return Mathf.Exp(-(val * val));
        }

        /// <summary>
        /// Speed at which the player moves while dodging.
        /// </summary>
        public float dodgeDist;

        /// <summary>
        /// Direction the dodge is moving in.
        /// </summary>
        private Vector3 _dodgeDirection;

        /// <summary>
        /// Direction player is moving while dodging.
        /// </summary>
        public Vector3 MoveDirection
        {
            get => _dodgeDirection.normalized * DodgeSpeedFactor(elapsed / duration) * dodgeDist / duration;
            set => _dodgeDirection = value;
        }

        /// <summary>
        /// MovementEngine for the player.
        /// </summary>
        private KCCMovementEngine movementEngine;

        public FixedMovementAction(
            InputActionReference actionReference,
            IActionActor<PlayerAction> actor,
            IStaminaMeter stamina,
            KCCMovementEngine movementEngine,
            PlayerAction action,
            float duration,
            float cooldown = 0.0f,
            float staminaCost = 0.0f,
            bool performWhileHeld = false)
            : base(actionReference, actor, stamina, action, duration, cooldown, staminaCost, performWhileHeld)
        {
            this.movementEngine = movementEngine;
        }

        protected override void Perform()
        {
            base.Perform();
        }

        /// <summary>
        /// Can the player attack based on current state.
        /// </summary>
        /// <returns>True if the player can jump, false otherwise.</returns>
        protected override bool Condition()
        {
            OpenKCC.Character.Config.KCCGroundedState kccGrounded = movementEngine.GroundedState;
            bool grounded = kccGrounded.StandingOnGround && !kccGrounded.Sliding;
            return base.Condition() && grounded;
        }
    }
}
