
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class DamageSource : IDamageSource
    {
        public GameObject Source { get; private set; }

        public DamageSource(GameObject source)
        {
            Source = source;
        }

        public static IDamageSource Resolve(NetworkObjectReference networkObjectReference)
        {
            if (networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                return new DamageSource(networkObject.gameObject);
            }

            return EmptyDamageSource.Instance;
        }
    }
}
