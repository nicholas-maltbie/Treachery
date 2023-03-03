
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
        private void AdjustStamina(float amount)
        {
            RemainingStamina = Mathf.Clamp(RemainingStamina + amount, 0, MaximumStamina);
        }

        /// <inheritdoc/>
        public void RestoreStamina(float amount)
        {
            AdjustStamina(amount);
        }

        /// <inheritdoc/>
        public void ExhaustStamina(float amount)
        {
            if (amount > 0)
            {
                AdjustStamina(-amount);
                lastStaminaSpendTime = Time.time;
            }
        }

        /// <inheritdoc/>
        public bool SpendStamina(float amount)
        {
            if (RemainingStamina >= amount)
            {
                ExhaustStamina(amount);
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
                    float factor = (Time.time - lastStaminaSpendTime - cooldownBeforeRestore) / cooldownSmoothTime;
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
            return SpendStamina(action.Cost);
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