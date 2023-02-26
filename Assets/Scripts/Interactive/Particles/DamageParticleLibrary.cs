
using System;
using System.Collections.Generic;
using nickmaltbie.Treachery.Interactive.Health;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Particles
{
    [CreateAssetMenu(fileName = "DamageParticleLibrary", menuName = "ScriptableObjects/DamageParticleLibrary", order = 1)]
    public class DamageParticleLibrary : ScriptableObject
    {
        public ParticleSystem piercingParticleSystem;
        public ParticleSystem bludgeoningParticleSystem;
        public ParticleSystem bleedingParticleSystem;

        public ParticleSystem GetParticleSystemPrefab(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Piercing:
                    return piercingParticleSystem;
                case DamageType.Bludgeoning:
                    return bludgeoningParticleSystem;
                case DamageType.Slashing:
                default:
                    return bleedingParticleSystem;
            }   
        }
    }

    public class ParticleCacheSet
    {
        private Dictionary<string, ParticleCache> particleCaches = new Dictionary<string, ParticleCache>();
        private DamageParticleLibrary library;

        public ParticleCacheSet(DamageParticleLibrary library, int maxCount = 10)
        {
            this.library = library;
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                ParticleSystem system = library.GetParticleSystemPrefab(damageType);
                string name = system.name;

                if (particleCaches.ContainsKey(name))
                {
                    continue;
                }

                particleCaches[name] = new ParticleCache(system, maxCount);
            }
        }

        public ParticleCache GetParticleCache(DamageType damageType)
        {
            ParticleSystem system = library.GetParticleSystemPrefab(damageType);
            string name = system.name;
            return particleCaches[name];
        }

        public ParticleSystem GetNextParticleCache(DamageType damageType)
        {
            return GetParticleCache(damageType).NextParticleSystem();
        }
    }

    public class ParticleCache
    {
        private ParticleSystem particlePrefab;
        private int maxCount = 10;
        private List<ParticleSystem> cache = new List<ParticleSystem>();
        private int current = 0;

        public ParticleCache(ParticleSystem prefab, int maxCount)
        {
            this.particlePrefab = prefab;
            this.maxCount = maxCount;
        }

        public ParticleSystem CurrentParticleSystem()
        {
            while (cache.Count <= current)
            {
                ParticleSystem instantiated = GameObject.Instantiate(particlePrefab);
                instantiated.hideFlags = HideFlags.HideAndDontSave;
                cache.Add(instantiated);
            }

            return cache[current];
        }

        public ParticleSystem NextParticleSystem()
        {
            ParticleSystem system = CurrentParticleSystem();
            if (system.isPlaying)
            {
                current = (current + 1) % cache.Count;
            }

            system = CurrentParticleSystem();
            system.Stop();
            return system;
        }
    }
}