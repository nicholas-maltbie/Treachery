
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
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Roll action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(KCCMovementEngine))]
    [RequireComponent(typeof(ICameraControls))]
    public class RollActionBehaviour : AbstractActionBehaviour<FixedMovementAction>
    {
        [SerializeField]
        private float rollDuration = 1.0f;

        [SerializeField]
        private float rollDistance = 5.5f;

        public override FixedMovementAction SetupAction()
        {
            var action = new FixedMovementAction(
                inputActionReference,
                Actor,
                rollDuration,
                GetComponent<KCCMovementEngine>(),
                cooldown)
            {
                dodgeDist = rollDistance,
            };
            action.OnPerform += OnRoll;
            action.OnComplete += OnComplete;
            return action;
        }

        private void OnRoll(object source, EventArgs args)
        {
            Action.MoveDirection = GetComponent<ICameraControls>().PlayerHeading * Vector3.forward;
            Actor.RaiseEvent(RollStart.Instance);
        }

        private void OnComplete(object source, bool interrupted)
        {
            Actor.RaiseEvent(RollStop.Instance);
        }

        public override void CleanupAction(FixedMovementAction action)
        {
            action.OnPerform -= OnRoll;
            action.OnComplete -= OnComplete;
        }
    }
}
