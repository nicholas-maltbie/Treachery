
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;

namespace nickmaltbie.Treachery.Player
{
    public interface IDamageActor
    {
        [ServerRpc]
        void AttackServerRpc(NetworkDamageEvent attack);

        [ServerRpc]
        void MultiAttackServerRpc(NetworkDamageEvent[] attack);
    }
}