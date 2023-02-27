
using System;
using nickmaltbie.Treachery.Interactive.Health;

namespace nickmaltbie.Treachery.Player
{
    public class DamagePassthroughAttribute : Attribute
    {
        public static void UpdateDamageableState(Type currentState, Damageable damageable)
        {
            if (Attribute.GetCustomAttribute(currentState, typeof(DamagePassthroughAttribute)) is DamagePassthroughAttribute)
            {
                damageable.Passthrough = true;
            }
            else
            {
                damageable.Passthrough = false;
            }
        }
    }

    public class InvulnerableAttribute : Attribute
    {
        public static void UpdateDamageableState(Type currentState, Damageable damageable)
        {
            if (Attribute.GetCustomAttribute(currentState, typeof(InvulnerableAttribute)) is InvulnerableAttribute)
            {
                damageable.Invulnerable = true;
            }
            else
            {
                damageable.Invulnerable = false;
            }
        }
    }
}