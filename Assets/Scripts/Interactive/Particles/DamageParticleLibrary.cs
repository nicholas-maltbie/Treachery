// Copyright (C) 2023 Nicholas Maltbie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

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

        public ParticleCacheSet(DamageParticleLibrary library, int maxCount = 100)
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
        private ParticleSystem[] cache;
        private int current = 0;

        public ParticleCache(ParticleSystem prefab, int maxCount)
        {
            particlePrefab = prefab;
            cache = new ParticleSystem[maxCount];
        }

        private ParticleSystem CurrentParticleSystem()
        {
            while (cache[current] == null)
            {
                ParticleSystem instantiated = GameObject.Instantiate(particlePrefab);
                instantiated.hideFlags = HideFlags.HideAndDontSave;
                cache[current] = instantiated;
            }

            return cache[current];
        }

        public ParticleSystem NextParticleSystem()
        {
            ParticleSystem system = CurrentParticleSystem();

            if (system.isPlaying)
            {
                current = (current + 1) % cache.Length;
            }

            system = CurrentParticleSystem();
            system.Stop();
            return system;
        }
    }
}
