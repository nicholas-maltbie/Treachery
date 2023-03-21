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
using System.Security.Cryptography;
using nickmaltbie.Treachery.Interactive.Hitbox;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class Damageable : NetworkBehaviour, IDamageable
    {
        public static readonly SHA256 hash = SHA256.Create();
        private Dictionary<string, IHitbox> hitboxLookup = new Dictionary<string, IHitbox>();
        public float DamageMultiplier { get; set; } = 1.0f;

        public NetworkVariable<float> maxHealth = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Owner,
            readPerm: NetworkVariableReadPermission.Everyone);
        public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Owner,
            readPerm: NetworkVariableReadPermission.Everyone);

        public event EventHandler<OnDamagedEvent> OnDamageEvent;
        public event EventHandler OnDeath;
        public event EventHandler OnResetHealth;

        public float MaxHealth => maxHealth.Value;
        public float CurrentHealth => currentHealth.Value;
        public bool Invulnerable { get; set; } = false;
        public bool Passthrough { get; set; } = false;

        private float GetAdjustedHealth(float change)
        {
            return Mathf.Clamp(CurrentHealth + change, 0, MaxHealth);
        }

        private void AdjustHealth(float change)
        {
            currentHealth.Value = GetAdjustedHealth(change);
        }

        public float GetHealthPercentage()
        {
            if (MaxHealth == 0)
            {
                return 1;
            }

            return CurrentHealth / MaxHealth;
        }

        public float GetMaxHealth()
        {
            return MaxHealth;
        }

        public float GetRemainingHealth()
        {
            return CurrentHealth;
        }

        public bool IsAlive()
        {
            return CurrentHealth > 0;
        }

        [ClientRpc]
        public void OnDeathClientRpc()
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
        }

        [ClientRpc]
        public void OnDamageClientRpc(NetworkDamageEvent networkDamageEvent, float adjust, float previousHealth, float currentHealth)
        {
            if (IsOwner && !IsOwnedByServer)
            {
                AdjustHealth(adjust);
            }

            OnDamageEvent?.Invoke(
                this,
                new OnDamagedEvent
                {
                    damageEvent = networkDamageEvent,
                    previousHealth = previousHealth,
                    currentHealth = currentHealth,
                });
        }

        [ClientRpc]
        public void OnResetHealthClientRpc()
        {
            OnResetHealth?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyDamage(DamageEvent damageEvent)
        {
            float adjust = damageEvent.amount;
            if (damageEvent.type == EventType.Damage)
            {
                if (!IsAlive() || Invulnerable)
                {
                    return;
                }

                adjust *= -DamageMultiplier;
            }

            float previousHealth = currentHealth.Value;
            if (IsOwnedByServer)
            {
                AdjustHealth(adjust);
            }

            OnDamageClientRpc(damageEvent, adjust, previousHealth, GetAdjustedHealth(adjust));
        }

        public void ResetToMaxHealth()
        {
            currentHealth.Value = maxHealth.Value;
        }

        public string AddHitbox(IHitbox hitbox, string name = "")
        {
            string id = null;
            lock (hitboxLookup)
            {
                id = hitboxLookup.Count.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    id = name;
                }

                hitboxLookup[id] = hitbox;
            }

            return id;
        }

        public IHitbox LookupHitbox(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            else if (hitboxLookup.TryGetValue(id, out IHitbox hitbox))
            {
                return hitbox;
            }
            else
            {
                return null;
            }
        }
    }

    public class DamageMultiplierAttribute : Attribute
    {
        public float multiplier = 1.0f;

        public static float GetDamageMultiplier(Type state)
        {
            if (Attribute.GetCustomAttribute(state, typeof(DamageMultiplierAttribute)) is DamageMultiplierAttribute mul)
            {
                return mul.multiplier;
            }

            return 1.0f;
        }

        public static void UpdateDamageMultiplier(Type state, Damageable damageable)
        {
            damageable.DamageMultiplier = GetDamageMultiplier(state);
        }

        public static void UpdateDamageMultiplier(Type state, GameObject player)
        {
            if (player.GetComponent<Damageable>() is Damageable damageable)
            {
                UpdateDamageMultiplier(state, damageable);
            }
        }
    }
}
