

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

using System;
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Character.Action;
using nickmaltbie.OpenKCC.Character.Config;
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using UnityEngine;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Dodge action that can be performed by a player.
    /// </summary>
    [Serializable]
    public class DodgeAction : ConditionalAction
    {
        /// <summary>
        /// Action reference for dodge.
        /// </summary>
        [SerializeField]
        public BufferedInput dodgeInput;

        /// <summary>
        /// Time in which the player is dodging.
        /// </summary>
        public float dodgeDuration;

        /// <summary>
        /// Speed at which the player moves while dodging.
        /// </summary>
        public float dodgeSpeed;

        /// <summary>
        /// Function to get the direction the player is moving in.
        /// </summary>
        protected Func<Vector3> GetMoveDir;

        /// <summary>
        /// Grounded state of the player action.
        /// </summary>
        protected KCCGroundedState groundedState;

        /// <summary>
        /// Direction player is moving while dodging.
        /// </summary>
        protected Vector3 dodgeDirection;

        /// <summary>
        /// Time elapsed during the current dodge action.
        /// </summary>
        protected float dodgeElapsed = Mathf.Infinity;

        /// <summary>
        /// Is the dodge action currently active.
        /// </summary>
        public bool DodgeActive => dodgeElapsed <= dodgeDuration;

        /// <summary>
        /// Setup this dodge action.
        /// </summary>
        public void Setup(Func<Vector3> moveDirFn)
        {
            base.condition = CanDodge;
            this.GetMoveDir = moveDirFn;
            dodgeInput.InputAction?.Enable();
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();
            dodgeInput?.Update();
            dodgeElapsed += Time.deltaTime;
        }

        public bool DodgeIfPossible(KCCGroundedState groundedState)
        {
            this.groundedState = groundedState;
            if (dodgeInput.Pressed && CanPerform)
            {
                Dodge();
                return true;
            }

            return false;
        }

        public void Dodge()
        {
            dodgeDirection = GetMoveDir().normalized * dodgeSpeed;
            dodgeElapsed = 0.0f;
        }

        /// <summary>
        /// Can the player attack based on current state.
        /// </summary>
        /// <returns>True if the player can jump, false otherwise.</returns>
        public bool CanDodge()
        {
            bool canMove = PlayerInputUtils.playerMovementState != PlayerInputState.Deny;
            bool grounded = groundedState.StandingOnGround && !groundedState.Sliding;
            return canMove && grounded;
        }
    }
}
