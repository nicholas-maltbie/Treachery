
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
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using UnityEngine;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    /// <summary>
    /// Punch action that can be performed by a player.
    /// </summary>
    [Serializable]
    public class PunchAttackAction : ActorConditionalAction<PlayerAction>
    {
        /// <summary>
        /// Range at which the punch can hit targets.
        /// </summary>
        public float attackRange = 2.0f;

        public float attackRadius = 0.1f;

        public float damageDealt = 10.0f;

        public bool IsLocalPlayer = false;

        public Vector3 attackBaseOffset;
        private IDamageable player;
        private ICameraControls viewHeading;
        private Transform playerPosition;

        public EventHandler<DamageEvent> OnAttack;

        /// <summary>
        /// Setup this attack action.
        /// </summary>
        public PunchAttackAction(
            BufferedInput bufferedInput,
            IActionActor<PlayerAction> actor,
            IDamageable player,
            Transform playerPosition,
            ICameraControls viewHeading)
            : base(bufferedInput, actor, PlayerAction.Punch)
        {
            this.player = player;
            this.playerPosition = playerPosition;
            this.viewHeading = viewHeading;
        }

        public Quaternion PlayerHeading()
        {
            if (viewHeading is IManagedCamera cameraController)
            {
                return Quaternion.Euler(cameraController.Pitch, cameraController.Yaw, 0);
            }

            return viewHeading.PlayerHeading;
        }

        protected override void Perform()
        {
            // Draw a line from the player's view towards
            // whatever direction they are looking.
            // Get the first thing we can hit in that direction
            Vector3 source = playerPosition.position + attackBaseOffset;
            Vector3 dir = PlayerHeading() * Vector3.forward;

            IDamageable target = null;
            float damage = 0.0f;
            Vector3 relativePos = Vector3.zero;
            Vector3 hitNormal = Vector3.zero;

            foreach (RaycastHit hit in Physics.SphereCastAll(
                source,
                attackRadius,
                dir,
                attackRange,
                IHitbox.HitboxLayerMask,
                QueryTriggerInteraction.Collide))
            {
                // Get the hitbox associated with the hit
                IHitbox checkHitbox = hit.collider?.GetComponent<IHitbox>();

                // Don't let the player hit him/her self.
                if (checkHitbox == null || checkHitbox.Source == player)
                {
                    continue;
                }

                // Otherwise deal some damage
                target = checkHitbox.Source;
                damage = damageDealt;
                Transform sourceTransform = (checkHitbox.Source as Component).transform;
                relativePos = sourceTransform.transform.worldToLocalMatrix * hit.point;
                hitNormal = -dir;

                // Then exit, no piercing in this attack.
                break;
            }

            OnAttack?.Invoke(
                this,
                new DamageEvent(
                    Interactive.Health.EventType.Damage,
                    DamageType.Bludgeoning,
                    target,
                    EmptyDamageSource.Instance,
                    damageDealt,
                    relativePos,
                    hitNormal,
                    null
                ));
        }
    }
}
