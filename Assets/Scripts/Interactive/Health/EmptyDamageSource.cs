
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Health
{
    public class EmptyDamageSource : IDamageSource
    {
        public EmptyDamageSource Instance = new EmptyDamageSource();
        
        private EmptyDamageSource()
        {

        }

        public GameObject Source => null;
    }
}