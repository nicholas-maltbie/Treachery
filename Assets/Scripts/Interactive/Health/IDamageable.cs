
namespace nickmaltbie.Treachery.Interactive.Health
{
    public interface IDamageable
    {
        void ApplyDamage(float damage, IDamageSource source);
        void HealHealth(float amount, IDamageSource source);
        float GetRemainingHealth();
        float GetMaxHealth();
        float GetHealthPercentage();
        bool IsAlive();
    }
}