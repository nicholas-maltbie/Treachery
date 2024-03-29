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

using nickmaltbie.Treachery.Interactive.Hitbox;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public enum DamageType
    {
        None,
        Bludgeoning,
        Slashing,
        Piercing
    }

    public enum EventType
    {
        Damage,
        Heal
    }

    public struct OnDamagedEvent
    {
        public DamageEvent damageEvent;
    }

    public struct DamageEvent
    {
        public EventType type;
        public IDamageable target;
        public IDamageSource damageSource;
        public float amount;
        public Vector3 relativeHitPos;
        public Vector3 hitNormal;
        public IHitbox hitbox;
        public DamageType damageType;

        public Transform SourceTransform => (hitbox as Component ?? target as Component).transform;

        public DamageEvent(
            EventType type,
            DamageType damageType,
            IDamageable target,
            IDamageSource source,
            float amount,
            Vector3 relativeHitPos,
            Vector3 hitNormal,
            IHitbox hitbox)
        {
            this.type = type;
            this.target = target;
            damageSource = source;
            this.amount = amount;
            this.relativeHitPos = relativeHitPos;
            this.hitNormal = hitNormal;
            this.hitbox = hitbox;
            this.damageType = damageType;
        }
    }
}
