
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
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using UnityEngine;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Jump action that can be performed by a character controller.
    /// </summary>
    [Serializable]
    public class PunchAttackAction : ConditionalAction
    {
        [Header("Player Jump Settings")]

        /// <summary>
        /// Action reference for attacking.
        /// </summary>
        [Tooltip("Action reference for attacking.")]
        [SerializeField]
        public BufferedInput attackInput;

        /// <summary>
        /// Range at which the punch can hit targets.
        /// </summary>
        public float attackRange = 2.0f;

        public float attackRadius = 0.1f;

        public float damageDealt = 10.0f;

        public bool IsLocalPlayer = false;

        public Vector3 attackBaseOffset;
        private IDamageSource damageSource;
        private IDamageable player;
        private ICameraControls viewHeading;
        private Transform playerPosition;

        public EventHandler<AttackEvent> OnAttack;

        /// <summary>
        /// Setup this attack action.
        /// </summary>
        public void Setup(IDamageable player, Transform playerPosition, ICameraControls viewHeading, IDamageSource damageSource)
        {
            base.condition = CanAttack;
            this.player = player;
            this.playerPosition = playerPosition;
            this.viewHeading = viewHeading;
            this.damageSource = damageSource;
            attackInput.InputAction?.Enable();
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();
            attackInput?.Update();
        }

        /// <summary>
        /// Apply the jump action to the actor if attempting
        /// jump and the player can jump.
        /// </summary>
        /// <returns>True if the player jumped, false otherwise.</returns>
        public bool AttackIfPossible()
        {
            if (attackInput.Pressed && CanPerform)
            {
                Attack();
                return true;
            }

            return false;
        }

        public Quaternion PlayerHeading()
        {
            if (viewHeading is IManagedCamera cameraController)
            {
                return Quaternion.Euler(cameraController.Pitch, cameraController.Yaw, 0);
            }

            return viewHeading.PlayerHeading;
        }

        public void Attack()
        {
            // Draw a line from the player's view towards
            // whatever direction they are looking.
            // Get the first thing we can hit in that direction
            Vector3 source = playerPosition.position + attackBaseOffset;
            Vector3 dir = PlayerHeading() * Vector3.forward;

            IDamageable target = null;
            float damage = 0.0f;
            Vector3 hitPos = Vector3.zero;
            IHitbox hitbox = null;

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
                hitPos = hit.point;
                hitbox = checkHitbox;

                // Then exit, no piercing in this attack.
                break;
            }

            OnAttack?.Invoke(
                this,
                new AttackEvent
                {
                    target = target,
                    damage = damageDealt,
                    hitPos = hitPos,
                    hitbox = hitbox,
                });

            attackInput.Reset();
        }

        /// <summary>
        /// Can the player attack based on current state.
        /// </summary>
        /// <returns>True if the player can jump, false otherwise.</returns>
        public bool CanAttack()
        {
            return player.IsAlive() && PlayerInputUtils.playerMovementState != PlayerInputState.Deny;
        }
    }
}
