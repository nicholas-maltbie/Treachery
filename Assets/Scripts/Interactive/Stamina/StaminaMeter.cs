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

using nickmaltbie.Treachery.Utils;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Stamina
{
    /// <summary>
    /// Stamina meter for some actor that is synchronized over
    /// the network for performing actions.
    /// </summary>
    public class StaminaMeter : NetworkBehaviour, IStaminaMeter
    {
        /// <summary>
        /// Maximum stamina for the actor.
        /// </summary>
        private NetworkVariable<float> maxStamina = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Owner,
            readPerm: NetworkVariableReadPermission.Everyone);

        /// <summary>
        /// Current remaining stamina for the actor.
        /// </summary>
        private NetworkVariable<float> currentStamina = new NetworkVariable<float>(
            value: 100,
            writePerm: NetworkVariableWritePermission.Owner,
            readPerm: NetworkVariableReadPermission.Everyone);

        /// <summary>
        /// Rate at which stamina is restored in units per second.
        /// </summary>
        public float staminaRestoreRate = 10.0f;

        /// <summary>
        /// Smooth time after cooldown passes.
        /// </summary>
        public float cooldownSmoothTime = 5.0f;

        /// <summary>
        /// Cooldown in time that the user has to wait before stamina will start
        /// to restore in seconds.
        /// </summary>
        public float cooldownBeforeRestore = 1.0f;

        /// <summary>
        /// last time the player spent some stamina.
        /// </summary>
        private float lastStaminaSpendTime = Mathf.NegativeInfinity;

        /// <inheritdoc/>
        public float RemainingStamina
        {
            get => currentStamina.Value;
            private set => currentStamina.Value = value;
        }

        /// <inheritdoc/>
        public float MaximumStamina
        {
            get => maxStamina.Value;
            private set => maxStamina.Value = value;
        }

        /// <inheritdoc/>
        public float PercentRemainingStamina => MaximumStamina > 0 ? RemainingStamina / MaximumStamina : 0;

        /// <summary>
        /// Adjust the current remaining stamina by some amount.
        /// </summary>
        /// <param name="amount">Increase or decrease stamina by some amount.</param>
        private float AdjustStamina(float amount)
        {
            float previous = RemainingStamina;
            RemainingStamina = Mathf.Clamp(RemainingStamina + amount, 0, MaximumStamina);
            return RemainingStamina - previous;
        }

        /// <inheritdoc/>
        public void RestoreStamina(float amount)
        {
            AdjustStamina(amount);
        }

        /// <inheritdoc/>
        public float ExhaustStamina(float amount, float cooldownTime = 0.0f)
        {
            if (amount > 0)
            {
                float change = AdjustStamina(-amount);
                lastStaminaSpendTime = Time.time + cooldownTime;
                return change;
            }

            return 0;
        }

        /// <inheritdoc/>
        public bool SpendStamina(float amount, float cooldownTime = 0.0f)
        {
            if (RemainingStamina >= amount)
            {
                ExhaustStamina(amount, cooldownTime);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (Time.time >= lastStaminaSpendTime + cooldownBeforeRestore)
            {
                if (cooldownSmoothTime > 0)
                {
                    float factor = (Time.time - lastStaminaSpendTime) / cooldownSmoothTime;
                    float smoothed = MathUtils.SmoothValue(factor);
                    RestoreStamina(staminaRestoreRate * smoothed * Time.fixedDeltaTime);
                }
                else
                {
                    RestoreStamina(staminaRestoreRate * Time.fixedDeltaTime);
                }
            }
        }

        public bool SpendStamina(IStaminaAction action)
        {
            return SpendStamina(action.Cost, action.CooldownTime);
        }

        public bool HasEnoughStamina(float amount)
        {
            return RemainingStamina >= amount;
        }

        public bool HasEnoughStamina(IStaminaAction action)
        {
            return RemainingStamina >= action.Cost;
        }
    }
}
