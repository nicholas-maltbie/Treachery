
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
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Dodge action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(KCCMovementEngine))]
    [RequireComponent(typeof(IMovementActor))]
    public class DodgeActionBehaviour : AbstractActionBehaviour<FixedMovementAction>
    {
        [SerializeField]
        private float dodgeDuration = 1.0f;

        [SerializeField]
        private float dodgeDistance = 3.5f;

        [SerializeField]
        private float staminaCost = 20.0f;

        private IMovementActor _movementActor;
        private IMovementActor MovementActor => _movementActor ??= GetComponent<IMovementActor>();

        public override FixedMovementAction SetupAction()
        {
            var action = new FixedMovementAction(
                inputActionReference,
                Actor,
                Stamina,
                GetComponent<KCCMovementEngine>(),
                PlayerAction.Dodge,
                dodgeDuration,
                cooldown,
                staminaCost)
            {
                dodgeDist = dodgeDistance,
            };
            action.OnPerform += OnDodge;
            action.OnComplete += OnComplete;
            return action;
        }

        private void OnDodge(object source, EventArgs args)
        {
            Action.MoveDirection = MovementActor.GetDesiredMovement().normalized;
            Actor.RaiseEvent(DodgeStart.Instance);
        }

        private void OnComplete(object source, bool interrupted)
        {
            Actor.RaiseEvent(DodgeStop.Instance);
        }

        public override void CleanupAction(FixedMovementAction action)
        {
            action.OnPerform -= OnDodge;
            action.OnComplete -= OnComplete;
        }
    }
}
