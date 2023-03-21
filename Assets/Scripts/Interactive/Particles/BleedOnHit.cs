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

using nickmaltbie.Treachery.Interactive.Health;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Particles
{
    public class BleedOnHit : MonoBehaviour
    {
        public static ParticleCacheSet BleedParticleCache = null;

        public DamageParticleLibrary library;

        public void Awake()
        {
            GetComponent<IDamageable>().OnDamageEvent += OnDamage;
            BleedParticleCache ??= new ParticleCacheSet(library);
        }

        public void OnDamage(object source, OnDamagedEvent onDamagedEvent)
        {
            Transform sourceTransform = onDamagedEvent.damageEvent.SourceTransform;
            ParticleSystem particles = BleedParticleCache.GetNextParticleCache(onDamagedEvent.damageEvent.damageType);
            particles.transform.SetParent(sourceTransform);
            particles.transform.position = sourceTransform.localToWorldMatrix * onDamagedEvent.damageEvent.relativeHitPos;
            particles.transform.rotation = Quaternion.FromToRotation(Vector3.forward, onDamagedEvent.damageEvent.hitNormal);
            particles.transform.localScale = Vector3.one;
            if (sourceTransform != null)
            {
                particles.transform.localScale = sourceTransform.lossyScale;
            }

            particles.Stop();
            particles.Play();
        }
    }
}
