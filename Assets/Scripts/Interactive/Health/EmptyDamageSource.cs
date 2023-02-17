
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class EmptyDamageSource : IDamageSource
    {
        public static EmptyDamageSource Instance = new EmptyDamageSource();
        
        private EmptyDamageSource()
        {

        }

        public GameObject Source => null;
    }
}