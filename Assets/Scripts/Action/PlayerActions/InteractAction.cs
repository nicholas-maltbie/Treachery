
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
using System.Collections.Generic;
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.Treachery.Interactive;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    public class InteractAction : ActorConditionalAction<PlayerAction>
    {
        public float interactRange = 2.0f;
        public float interactRadius = 0.1f;
        private Transform playerPos;
        private Vector3 viewOffset;
        private ICameraControls viewHeading;
        private IInteractive previousInteractive;
        private GameObject player;

        public InteractAction(
            InputActionReference actionReference,
            IActionActor<PlayerAction> actor,
            IStaminaMeter stamina,
            GameObject player,
            Transform playerPos,
            Vector3 viewOffset,
            ICameraControls viewHeading,
            float cooldown = 0.0f,
            float staminaCost = 0.0f)
            : base(actionReference, actor, stamina, PlayerAction.Interact, cooldown, staminaCost, false)
        {
            this.player = player;
            this.viewHeading = viewHeading;
            this.playerPos = playerPos;
            this.viewOffset = viewOffset;
        }

        public Quaternion PlayerHeading()
        {
            if (viewHeading is IManagedCamera cameraController)
            {
                return Quaternion.Euler(cameraController.Pitch, cameraController.Yaw, 0);
            }

            return viewHeading.PlayerHeading;
        }

        public override void Update()
        {
            Physics.SphereCast(
                playerPos.position + viewOffset,
                interactRadius,
                PlayerHeading() * Vector3.forward,
                out RaycastHit hit,
                interactRange,
                ~IHitbox.HitboxAndPlayerLayerMask,
                QueryTriggerInteraction.Ignore);
            IInteractive nextInteractive = hit.collider?.GetComponent<IInteractive>();
            
            if (nextInteractive != previousInteractive)
            {
                previousInteractive?.SetFocusState(player, false);
                nextInteractive?.SetFocusState(player, true);
            }

            previousInteractive = nextInteractive;
            base.Update();
        }

        protected override void Perform()
        {
            previousInteractive.OnInteract(player);
        }

        protected override bool Condition()
        {
            return base.Condition() && previousInteractive != null && previousInteractive.CanInteract(player);
        }
    }
}
