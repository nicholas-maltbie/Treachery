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
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive
{
    public class Arrow : NetworkBehaviour
    {
        public IDamageable source = null;
        public Transform holdPosition;
        public Transform front;
        public float despawnTime = 100.0f;
        public float despawnPinTime = 10.0f;
        public float arrowDamage = 10.0f;
        public bool useGravity = true;
        public float hitboxRadius = 0.05f;
        private float elapsed = 0.0f;

        public bool Pinned { get; private set; } = false;
        public bool Fired { get; private set; } = false;
        public Vector3 Speed { get; private set; } = Vector3.zero;

        public void Loose(Vector3 direction, float speed)
        {
            Fired = true;
            Speed = direction * speed;
            LooseClientRpc(Speed);
        }

        [ClientRpc]
        public void LooseClientRpc(Vector3 speed)
        {
            Fired = true;
            Speed = speed;
        }

        public void Update()
        {
            if (Pinned)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= despawnPinTime)
                {
                    GameObject.Destroy(gameObject);
                }
            }
        }

        public void UpdatePosition()
        {
            transform.position += Speed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.LookRotation(Speed.normalized, -Physics.gravity.normalized);
            if (useGravity)
            {
                Speed += Physics.gravity * Time.fixedDeltaTime;
            }
        }

        public void PredictMovement()
        {
            if (Fired)
            {
                UpdatePosition();
            }
        }

        public void FixedUpdate()
        {
            if (!IsServer)
            {
                PredictMovement();
                return;
            }

            if (!Fired)
            {
                return;
            }

            elapsed += Time.fixedDeltaTime;
            if (elapsed >= despawnTime)
            {
                GetComponent<NetworkObject>().Despawn();
                return;
            }

            Vector3 startArrowHeadPos = front.position;
            UpdatePosition();

            // Draw a ray from start position to end position.
            Vector3 dir = front.position - startArrowHeadPos;
            foreach (RaycastHit hit in Physics.SphereCastAll(
                startArrowHeadPos,
                hitboxRadius,
                dir.normalized,
                dir.magnitude,
                IHitbox.HitLayerMaskComputation,
                QueryTriggerInteraction.Collide))
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
                    // If we hit something, despawn and create pinned arrow.
                    NetworkObject netObj = hit.collider.GetComponent<NetworkObject>();

                    if (netObj != null)
                    {
                        Vector3 relativePos = netObj.transform.worldToLocalMatrix * hit.point;
                        SpawnPincushionArrowParentedClientRpc(netObj, relativePos, transform.rotation * Vector3.back);
                    }
                    else
                    {
                        SpawnPincushionArrowClientRpc(hit.point, transform.rotation * Vector3.back);
                    }

                    Fired = false;
                    GetComponent<NetworkObject>().Despawn();
                    return;
                }

                Component hitObj = (checkHitbox as Component) ?? (checkHitbox.Source as Component);
                Vector3 relativeHitPos = hitObj.transform.worldToLocalMatrix * hit.point;

                var arrowDamageEvent = new DamageEvent(
                    type: Health.EventType.Damage,
                    damageType: DamageType.Piercing,
                    target: checkHitbox.Source,
                    source: EmptyDamageSource.Instance,
                    amount: arrowDamage,
                    relativeHitPos: relativeHitPos,
                    hitNormal: transform.rotation * Vector3.back,
                    hitbox: checkHitbox);
                checkHitbox.Source.ApplyDamage(arrowDamageEvent);
                SpawnPincushionArrowOnDamageClientRpc(arrowDamageEvent);
                Fired = false;
                GetComponent<NetworkObject>().Despawn();
                return;
            }
        }

        [ClientRpc]
        public void SpawnPincushionArrowClientRpc(Vector3 pos, Vector3 normal)
        {
            Arrow pinnedArrow = GameObject.Instantiate(this, pos + front.localPosition - normal * 0.1f, Quaternion.LookRotation(-normal));
            pinnedArrow.hideFlags = HideFlags.HideAndDontSave;
            pinnedArrow.Pinned = true;
        }

        [ClientRpc]
        public void SpawnPincushionArrowParentedClientRpc(NetworkObjectReference parentReference, Vector3 relativePos, Vector3 normal)
        {
            if (parentReference.TryGet(out NetworkObject parentNetworkObj))
            {
                var arrowRot = Quaternion.LookRotation(-normal);
                Vector3 arrowPos = parentNetworkObj.transform.localToWorldMatrix * relativePos;
                Arrow pinnedArrow = GameObject.Instantiate(
                    this,
                    arrowPos + front.localPosition - normal * 0.1f,
                    arrowRot,
                    parentNetworkObj.transform);
                pinnedArrow.hideFlags = HideFlags.HideAndDontSave;
                pinnedArrow.Pinned = true;
            }
        }

        [ClientRpc]
        public void SpawnPincushionArrowOnDamageClientRpc(NetworkDamageEvent damageEvent)
        {
            DamageEvent localEvent = damageEvent;
            Transform pincushion = (damageEvent.hitbox as Component ?? damageEvent.target as Component).transform;
            Vector3 arrowPos = pincushion.localToWorldMatrix * damageEvent.relativeHitPos;
            var arrowRot = Quaternion.LookRotation(-damageEvent.hitNormal);
            Arrow pinnedArrow = GameObject.Instantiate(this, arrowPos + front.localPosition - damageEvent.hitNormal * 0.1f, arrowRot, pincushion);
            pinnedArrow.Pinned = true;
            pinnedArrow.hideFlags = HideFlags.HideAndDontSave;
            pinnedArrow.transform.SetParent(localEvent.SourceTransform);
        }
    }
}
