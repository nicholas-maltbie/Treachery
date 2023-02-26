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

using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public struct NetworkDamageEvent : INetworkSerializable
    {
        public DamageType damageType;
        public EventType eventType;
        public float amount;
        public Vector3 relativeHitPos;
        public Vector3 hitNormal;
        private NetworkObjectReference targetReference;
        private NetworkObjectReference sourceReference;
        private string hitboxId;
        private bool hasSource;

        private GameObject Target
        {
            get
            {
                if (targetReference.TryGet(out NetworkObject obj))
                {
                    return obj.gameObject;
                }

                return null;
            }
        }

        private GameObject Source
        {
            get
            {
                if (sourceReference.TryGet(out NetworkObject obj))
                {
                    return obj.gameObject;
                }

                return null;
            }
        }

        public IDamageSource source => Source?.GetComponent<IDamageSource>() ?? EmptyDamageSource.Instance;
        public IDamageable target => Target?.GetComponent<IDamageable>();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref eventType);
            serializer.SerializeValue(ref amount);
            serializer.SerializeValue(ref relativeHitPos);
            serializer.SerializeValue(ref hitNormal);
            serializer.SerializeValue(ref targetReference);
            serializer.SerializeValue(ref hasSource);
            serializer.SerializeValue(ref hitboxId);
            serializer.SerializeValue(ref damageType);

            if (hasSource)
            {
                serializer.SerializeValue(ref sourceReference);
            }
        }

        public static void ProcessEvent(NetworkDamageEvent attack)
        {
            attack.target.ApplyDamage(attack);
        }

        public static implicit operator DamageEvent(NetworkDamageEvent damageEvent)
        {
            NetworkObject source = null;
            bool hasTarget = damageEvent.targetReference.TryGet(out NetworkObject target);
            bool hasSource = damageEvent.hasSource && damageEvent.sourceReference.TryGet(out source);
            IDamageable taget = hasTarget ? target.GetComponent<IDamageable>() : null;
            return new DamageEvent(
                type: damageEvent.eventType,
                damageType: damageEvent.damageType,
                amount: damageEvent.amount,
                relativeHitPos: damageEvent.relativeHitPos,
                hitNormal: damageEvent.hitNormal,
                target: taget,
                source: hasSource ? source.gameObject.GetComponent<IDamageSource>() : EmptyDamageSource.Instance,
                hitbox: taget?.LookupHitbox(damageEvent.hitboxId)
            );
        }

        public static implicit operator NetworkDamageEvent(DamageEvent damageEvent)
        {
            NetworkObject sourceObj = (damageEvent.damageSource as Component)?.GetComponent<NetworkObject>();
            NetworkObject targetObject = (damageEvent.target as Component)?.gameObject.GetComponent<NetworkObject>();
            _ = damageEvent.hitbox as NetworkBehaviour;

            return new NetworkDamageEvent
            {
                eventType = damageEvent.type,
                damageType = damageEvent.damageType,
                amount = damageEvent.amount,
                relativeHitPos = damageEvent.relativeHitPos,
                hitNormal = damageEvent.hitNormal,
                hasSource = sourceObj != null,
                targetReference = targetObject != null ? new NetworkObjectReference(targetObject) : default,
                sourceReference = sourceObj != null ? new NetworkObjectReference(sourceObj) : default,
                hitboxId = damageEvent.hitbox?.HitboxId ?? "",
            };
        }
    }
}
