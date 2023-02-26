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

using System.Collections.Generic;
using nickmaltbie.Treachery.Interactive.Health;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Hitbox
{
    public interface IHitbox
    {
        public static int HitboxLayer => LayerMask.NameToLayer("Hitbox");
        public static int PlayerLayer => LayerMask.NameToLayer("Player");
        public static int HitLayerMaskComputation => HitboxLayerMask | (~PlayerLayerMask);
        public static int HitboxLayerMask => 1 << HitboxLayer;
        public static int PlayerLayerMask => 1 << PlayerLayer;

        /// <summary>
        /// Damageable object associated with this hitbox.
        /// </summary>
        /// <value></value>
        IDamageable Source { get; }

        /// <summary>
        /// Collider associated with this hitbox.
        /// </summary>
        Collider Collider { get; }

        /// <summary>
        /// Is this a critical hit area (like a headshot).
        /// </summary>
        bool IsCritical { get; }

        /// <summary>
        /// Is the hitbox disabled and objects/hits should passthrough
        /// instead of hitting this.
        /// </summary>
        bool Disabled { get; }

        /// <summary>
        /// Hitbox id for differentiating between other hitboxes on a character/object.
        /// </summary>
        string HitboxId { get; }

        public static DamageEvent DamageEventFromhit(RaycastHit hit, IHitbox hitbox, float damage, Vector3 normal)
        {
            Component hitObj = (hitbox as Component) ?? (hitbox.Source as Component);
            Vector3 relativeHitPos = hitObj.transform.worldToLocalMatrix * hit.point;
            return new DamageEvent(
                type: Health.EventType.Damage,
                damageType: DamageType.Piercing,
                target: hitbox.Source,
                source: EmptyDamageSource.Instance,
                amount: damage,
                relativeHitPos: relativeHitPos,
                hitNormal: normal,
                hitbox: hitbox);
        }

        public static IHitbox GetFirstValidHit(IEnumerable<RaycastHit> hitSequence, IDamageable source, out RaycastHit firstHit)
        {
            foreach (RaycastHit hit in hitSequence)
            {
                // Get the hitbox associated with the hit
                IHitbox checkHitbox = hit.collider?.GetComponent<IHitbox>();

                // Don't let the player hit him/her self.
                if (checkHitbox != null && checkHitbox.Source == source)
                {
                    continue;
                }
                else if (checkHitbox == null && !hit.collider.isTrigger)
                {
                    // check if we hit a wall or something.
                    firstHit = hit;
                    return null;
                }
                else if (!checkHitbox.Disabled)
                {
                    // we had a valid hit, return this hitbox.
                    firstHit = hit;
                    return checkHitbox;
                }
            }

            firstHit = default;
            return null;
        }
    }
}
