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
