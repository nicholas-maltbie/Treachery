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

using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Action
{
    public struct NetworkAttackEvent : INetworkSerializable
    {
        public IDamageSource source => Source?.GetComponent<IDamageSource>();
        public IDamageable target => Target?.GetComponent<IDamageable>();
        public float damage;
        public Vector3 hitPos;
        private NetworkObjectReference targetReference;
        private NetworkObjectReference sourceReference;

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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref damage);
            serializer.SerializeValue(ref hitPos);
            serializer.SerializeValue(ref targetReference);
            serializer.SerializeValue(ref sourceReference);
        }

        public static void ProcessEvent(NetworkAttackEvent attack)
        {
            attack.target.ApplyDamage(attack.damage, attack.source);
        }

        public static NetworkAttackEvent FromAttackEvent(AttackEvent attack, GameObject source)
        {
            return new NetworkAttackEvent
            {
                damage = attack.damage,
                hitPos = attack.hitPos,
                targetReference = (attack.target as MonoBehaviour).gameObject.GetComponent<NetworkObject>(),
                sourceReference = source.GetComponent<NetworkObject>(),
            };
        }
    }
}
