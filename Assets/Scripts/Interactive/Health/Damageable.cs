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
using nickmaltbie.Treachery.Interactive.Stamina;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class Damageable : NetworkBehaviour, IDamageable
    {
        public static readonly SHA256 hash = SHA256.Create();
        private Dictionary<string, IHitbox> hitboxLookup = new Dictionary<string, IHitbox>();

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

        public float DamageMultiplier { get; set; } = 1.0f;
        public float StaminaSplit { get; set; } = 0.0f;
        public IStaminaMeter Stamina => GetComponent<IStaminaMeter>();
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
            // If the player has a stamina meter and the stamina split is not zero
            // and damage is being dealt.
            bool damage = change < 0;
            bool spendStamina = Stamina != null && StaminaSplit >= 0;

            UnityEngine.Debug.Log($"{gameObject.name} -- Stamina:{Stamina} != null && StaminaSplit:{StaminaSplit} >= 0");
            if (spendStamina && damage)
            {
                float staminaCost = Mathf.Abs(change) * StaminaSplit;
                change *= Mathf.Clamp(1 - StaminaSplit, 0, 1);

                Stamina.SpendStamina(staminaCost);
            }

            bool wasAlive = IsAlive();
            currentHealth.Value = GetAdjustedHealth(change);
            if (wasAlive && !IsAlive())
            {
                ReportDeathServerRpc();
            }
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

        [ServerRpc]
        public void ReportDeathServerRpc()
        {
            OnDeathClientRpc();
        }

        [ClientRpc]
        public void OnDeathClientRpc()
        {
            OnDeath?.Invoke(this, EventArgs.Empty);
        }

        [ClientRpc]
        public void OnDamageClientRpc(NetworkDamageEvent networkDamageEvent)
        {
            OnDamageEvent?.Invoke(
                this,
                new OnDamagedEvent
                {
                    damageEvent = networkDamageEvent,
                });

            float adjust = networkDamageEvent.amount;
            if (networkDamageEvent.eventType == EventType.Damage)
            {
                adjust *= -DamageMultiplier;
            }

            if (IsOwner)
            {
                AdjustHealth(adjust);
            }
        }

        [ClientRpc]
        public void OnResetHealthClientRpc()
        {
            OnResetHealth?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyDamage(DamageEvent damageEvent)
        {
            if (damageEvent.type == EventType.Damage && (!IsAlive() || Invulnerable))
            {
                return;
            }

            OnDamageClientRpc(damageEvent);
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
        public float staminaSplit = 0.0f;
        public float damageMultiplier = 1.0f;

        public static float GetDamageMultiplier(Type state)
        {
            if (Attribute.GetCustomAttribute(state, typeof(DamageMultiplierAttribute)) is DamageMultiplierAttribute mul)
            {
                return mul.damageMultiplier;
            }

            return 1.0f;
        }

        public static float GetStaminaSplit(Type state)
        {
            if (Attribute.GetCustomAttribute(state, typeof(DamageMultiplierAttribute)) is DamageMultiplierAttribute mul)
            {
                return mul.staminaSplit;
            }

            return 0.0f;
        }

        public static void UpdateDamageMultiplier(Type state, Damageable damageable)
        {
            damageable.DamageMultiplier = GetDamageMultiplier(state);
            damageable.StaminaSplit = GetStaminaSplit(state);
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
