
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class Damageable : MonoBehaviour, IDamageable, IDamageSource
    {
        public float maxHealth;
        public float currentHealth;
        public GameObject Source => gameObject;

        private void AdjustHealth(float change)
        {
            currentHealth = Mathf.Clamp(currentHealth + change, 0, maxHealth);
        }

        public void ApplyDamage(float damage, IDamageSource source)
        {
            AdjustHealth(-damage);
        }

        public float GetHealthPercentage()
        {
            if (maxHealth == 0)
            {
                return 1;
            }

            return currentHealth / maxHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }

        public float GetRemainingHealth()
        {
            return currentHealth;
        }

        public void HealHealth(float amount, IDamageSource source)
        {
            AdjustHealth(amount);
        }

        public bool IsAlive()
        {
            return currentHealth > 0;
        }
    }
}