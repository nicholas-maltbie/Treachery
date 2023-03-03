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
using nickmaltbie.Treachery.Interactive.Stamina;
using nickmaltbie.Treachery.Player;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    public class BlockActorAction : ContinuousConditionalAction<PlayerAction>
    {
        /// <summary>
        /// MovementEngine for the player.
        /// </summary>
        private KCCMovementEngine movementEngine;

        public BlockActorAction(
            InputActionReference actionReference,
            IActionActor<PlayerAction> actor,
            IStaminaMeter stamina,
            KCCMovementEngine movementEngine)
            : base(actionReference, actor, stamina, PlayerAction.Block, 0.0f, 0.0f, BlockStart.Instance, BlockStop.Instance)
        {
            this.movementEngine = movementEngine;
        }

        /// <summary>
        /// Can the player jump based on their current state.
        /// </summary>
        /// <returns>True if the player can jump, false otherwise.</returns>
        protected override bool Condition()
        {
            KCCGroundedState kccGrounded = movementEngine.GroundedState;
            return base.Condition() && kccGrounded.StandingOnGround && !kccGrounded.Sliding;
        }
    }
}
