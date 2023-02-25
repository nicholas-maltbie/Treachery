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

using System.Linq;
using nickmaltbie.Treachery.Interactive.Health;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Particles
{
    public class BleedOnHit : MonoBehaviour
    {
        public ParticleSystem bloodParticles;
        public int maxParticles = 10;
        private ParticleSystem[] particleCache;
        private int currentIndex;

        public ParticleSystem CurrentParticles => particleCache[currentIndex];
        public ParticleSystem NextParticles
        {
            get
            {
                currentIndex = (currentIndex + 1) % particleCache.Length;
                return CurrentParticles;
            }
        }

        public void Awake()
        {
            particleCache = Enumerable.Range(0, maxParticles).Select(_ =>
            {
                GameObject spawned = GameObject.Instantiate(bloodParticles.gameObject, transform.position, transform.rotation, transform);
                spawned.hideFlags = HideFlags.HideAndDontSave;
                ParticleSystem particles = spawned.GetComponent<ParticleSystem>();
                particles.Stop();
                return particles;
            }).ToArray();

            GetComponent<IDamageable>().OnDamageEvent += OnDamage;
        }

        public void OnDamage(object source, OnDamagedEvent onDamagedEvent)
        {
            ParticleSystem particles = NextParticles;
            particles.transform.position = transform.localToWorldMatrix * onDamagedEvent.damageEvent.relativeHitPos;
            particles.transform.rotation = Quaternion.FromToRotation(Vector3.up, onDamagedEvent.damageEvent.hitNormal);
            particles.Stop();
            particles.Play();
        }
    }
}
