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

using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Breakeable
{
    public enum BreakableObjectState
    {
        Unitialized,
        Fixed,
        Broken,
        CleanedUp
    }

    [RequireComponent(typeof(Damageable))]
    public class BreakableObject : NetworkSMBehaviour
    {
        public BreakableObjectState defaultState = BreakableObjectState.Fixed;

        private NetworkVariable<BreakableObjectState> borkenState;

        public GameObject fixedPrefab;
        public GameObject brokenPrefab;
        public float despawnTime;

        private GameObject fixedSpawn;
        private GameObject brokenSpawn;
        private float brokenElapsed;

        public override void Start()
        {
            borkenState = new NetworkVariable<BreakableObjectState>(value: defaultState);
            base.Start();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                borkenState.Value = defaultState;
                Damageable damageable = GetComponent<Damageable>();
                damageable.HealHealth(damageable.GetMaxHealth(), EmptyDamageSource.Instance);
            }

            UpdatePrefabState();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            borkenState.Value = defaultState;
            UpdatePrefabState();
        }

        public override void Update()
        {
            base.Update();

            if (IsSpawned && IsServer)
            {
                UpdateObjectState();
            }

            UpdatePrefabState();
        }

        public void UpdateObjectState()
        {
            switch (borkenState.Value)
            {
                case BreakableObjectState.Broken:
                    brokenElapsed += Time.deltaTime;
                    if (brokenElapsed >= despawnTime)
                    {
                        borkenState.Value = BreakableObjectState.CleanedUp;
                    }

                    break;
                case BreakableObjectState.Fixed:
                    if (!GetComponent<Damageable>().IsAlive())
                    {
                        brokenElapsed = 0.0f;
                        borkenState.Value = BreakableObjectState.Broken;
                    }

                    break;
                case BreakableObjectState.CleanedUp:
                    if (GetComponent<Damageable>().IsAlive())
                    {
                        borkenState.Value = BreakableObjectState.Fixed;
                    }

                    break;
            }
        }

        public void UpdatePrefabState()
        {
            if (borkenState.Value == BreakableObjectState.Fixed)
            {
                fixedSpawn ??= GameObject.Instantiate(fixedPrefab, transform.position, transform.rotation, transform);
            }
            else if (fixedSpawn != null)
            {
                GameObject.Destroy(fixedSpawn);
                fixedSpawn = null;
            }

            if (borkenState.Value == BreakableObjectState.Broken)
            {
                brokenSpawn ??= GameObject.Instantiate(brokenPrefab, transform.position, transform.rotation, transform);
            }
            else if (brokenSpawn != null)
            {
                GameObject.Destroy(brokenSpawn);
                brokenSpawn = null;
            }
        }
    }
}
