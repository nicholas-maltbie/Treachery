
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
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Player;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Dodge action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(KCCMovementEngine))]
    [RequireComponent(typeof(IActionActor<PlayerAction>))]
    [RequireComponent(typeof(IMovementActor))]
    public class DodgeActionBehaviour : AbstractActionBehaviour<PlayerAction>
    {
        [SerializeField]
        private float dodgeDuration = 1.0f;

        [SerializeField]
        private float dodgeDistance = 3.5f;

        public DodgeAction dodgeAction { get; private set; }

        public override ActorConditionalAction<PlayerAction> Action => dodgeAction;

        public override void Awake()
        {
            base.Awake();
            IActionActor<PlayerAction> actionActor = GetComponent<IActionActor<PlayerAction>>();
            IMovementActor movementActor = GetComponent<IMovementActor>();
            dodgeAction = new DodgeAction(
                bufferedInput,
                actionActor,
                dodgeDuration,
                GetComponent<KCCMovementEngine>())
            {
                dodgeDist = dodgeDistance,
            };

            dodgeAction.OnPerform += (_, _) =>
            {
                dodgeAction.DodgeDirection = movementActor.GetDesiredMovement().normalized;
                actionActor.RaiseEvent(DodgeStart.Instance);
            };
            dodgeAction.OnComplete += (_, _) => actionActor.RaiseEvent(DodgeStop.Instance);
        }
    }
}
