
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

        public float MaxHealth => maxHealth.Value;
        public float CurrentHealth => currentHealth.Value;

        private void AdjustHealth(float change)
        {
            currentHealth.Value = Mathf.Clamp(CurrentHealth + change, 0, MaxHealth);
        }

        public void ApplyDamage(float damage, IDamageSource source)
        {
            // If player is dead, can't apply damage now can we.
            if (!IsAlive())
            {
                return;
            }

            float previous = CurrentHealth;
            AdjustHealth(-damage);
            InvokeListenersOnDamage(damage, previous, CurrentHealth, source);

            NetworkObject objectSource = source.Source?.GetComponent<NetworkObject>();
            if (objectSource != null)
            {
                OnDamageClientRpc(damage, previous, CurrentHealth, objectSource);
            }
            else
            {
                OnEmptyDamageClientRpc(damage, previous, CurrentHealth);
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

        public void HealHealth(float amount, IDamageSource source)
        {
            float previous = CurrentHealth;
            AdjustHealth(amount);
            InvokeListenersOnHeal(amount, previous, CurrentHealth, source);

            NetworkObject objectSource = source.Source?.GetComponent<NetworkObject>();
            if (objectSource != null)
            {
                OnHealedClientRpc(amount, previous, CurrentHealth, objectSource);
            }
            else
            {
                OnEmptyHealClientRpc(amount, previous, CurrentHealth);
            }
        }

        public bool IsAlive()
        {
            return CurrentHealth > 0;
        }

        [ClientRpc]
        public void OnEmptyDamageClientRpc(float damage, float previous, float current)
        {
            InvokeListenersOnDamage(damage, previous, current, EmptyDamageSource.Instance);
        }

        [ClientRpc]
        public void OnDamageClientRpc(float damage, float previous, float current, NetworkObjectReference networkDamageSource)
        {
            InvokeListenersOnDamage(damage, previous, current, DamageSource.Resolve(networkDamageSource));
        }

        [ClientRpc]
        public void OnEmptyHealClientRpc(float amount, float previous, float current)
        {
            InvokeListenersOnHeal(amount, previous, current, EmptyDamageSource.Instance);
        }

        [ClientRpc]
        public void OnHealedClientRpc(float amount, float previous, float current, NetworkObjectReference networkDamageSource)
        {
            InvokeListenersOnHeal(amount, previous, current, DamageSource.Resolve(networkDamageSource));
        }

        public void InvokeListenersOnHeal(float amount, float previous, float current, IDamageSource source)
        {
            foreach (IDamageListener listener in gameObject.GetComponents<IDamageListener>())
            {
                listener.OnHeal(this, source, previous, current, amount);
            }
        }

        public void InvokeListenersOnDamage(float damage, float previous, float current, IDamageSource source)
        {
            foreach (IDamageListener listener in gameObject.GetComponents<IDamageListener>())
            {
                listener.OnDamage(this, source, previous, current, damage);
            }
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // nothing to serialize here
        }
    }
}
