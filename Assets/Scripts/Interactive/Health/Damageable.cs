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
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class Damageable : NetworkBehaviour, IDamageable
    {
        public NetworkVariable<float> maxHealth = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Server,
            readPerm: NetworkVariableReadPermission.Everyone);
        public NetworkVariable<float> currentHealth = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Server,
            readPerm: NetworkVariableReadPermission.Everyone);

        public event EventHandler<OnDamagedEvent> OnDamageEvent;
        public event EventHandler OnResetHealth;

        public float MaxHealth => maxHealth.Value;
        public float CurrentHealth => currentHealth.Value;

        private void AdjustHealth(float change)
        {
            currentHealth.Value = Mathf.Clamp(CurrentHealth + change, 0, MaxHealth);
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
        public void OnDamageClientRpc(NetworkDamageEvent networkDamageEvent, float previousHealth, float currentHealth)
        {
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
            if (damageEvent.type == DamageType.Damage)
            {
                if (!IsAlive())
                {
                    return;
                }

                adjust *= -1;
            }

            float previousHealth = currentHealth.Value;
            AdjustHealth(adjust);
            OnDamageClientRpc(damageEvent, previousHealth, currentHealth.Value);
        }

        public void ResetToMaxHealth()
        {
            currentHealth.Value = maxHealth.Value;
        }
    }
}
