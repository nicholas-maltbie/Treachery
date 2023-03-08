
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
using nickmaltbie.OpenKCC.Utils;
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Roll action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(KCCMovementEngine))]
    [RequireComponent(typeof(ICameraControls))]
    [RequireComponent(typeof(IMovementActor))]
    public class RollActionBehaviour : AbstractActionBehaviour<RollMovementAction>
    {
        [SerializeField]
        private float rollDuration = 1.0f;

        [SerializeField]
        private float rollDistance = 5.5f;

        [SerializeField]
        public float staminaCost = 33.3f;

        private IMovementActor _movementActor;
        private IMovementActor MovementActor => _movementActor ??= GetComponent<IMovementActor>();

        private ICameraControls _cameraControls;
        private ICameraControls CameraControls => _cameraControls ??= GetComponent<ICameraControls>();

        private KCCMovementEngine _movementEngine;
        private KCCMovementEngine MovementEngine => _movementEngine ??= GetComponent<KCCMovementEngine>();

        public Vector3 RollDirection { get; private set; }
        public float RollRotation { get; private set; }

        public override RollMovementAction SetupAction()
        {
            var action = new RollMovementAction(
                inputActionReference,
                Actor,
                Stamina,
                MovementEngine,
                GetComponent<DodgeActionBehaviour>().Action,
                rollDuration,
                cooldown,
                staminaCost)
            {
                dodgeDist = rollDistance,
            };
            action.OnPerform += OnRoll;
            action.OnComplete += OnComplete;
            return action;
        }

        public override void Update()
        {
            base.Update();
            Vector3 movement = MovementActor.GetDesiredMovement();
            if (movement.magnitude > KCCUtils.Epsilon)
            {
                Vector3 rotatedMovementForward = CameraControls.PlayerHeading * MovementActor.InputMovement;
                var angle = Quaternion.LookRotation(rotatedMovementForward, MovementEngine.Up);
                RollRotation = angle.eulerAngles.y;
                RollDirection = movement.normalized;
            }
        }

        private void OnRoll(object source, EventArgs args)
        {
            Action.MoveDirection = RollDirection;
            Actor.RaiseEvent(RollStart.Instance);
        }

        private void OnComplete(object source, bool interrupted)
        {
            Actor.RaiseEvent(RollStop.Instance);
        }

        public override void CleanupAction(RollMovementAction action)
        {
            action.OnPerform -= OnRoll;
            action.OnComplete -= OnComplete;
        }
    }
}
