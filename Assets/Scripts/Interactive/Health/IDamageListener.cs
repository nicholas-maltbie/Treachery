

namespace nickmaltbie.Treachery.Interactive.Health
{
    public interface IDamageListener
    {
        void OnDamage(IDamageable target, IDamageSource source, float previous, float current, float damage);
        void OnHeal(IDamageable target, IDamageSource source, float previous, float current, float amount);
    }
}
