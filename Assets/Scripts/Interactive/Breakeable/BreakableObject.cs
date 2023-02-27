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
using nickmaltbie.Treachery.Interactive.Hitbox;
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
    public class BreakableObject : NetworkBehaviour
    {
        public BreakableObjectState defaultState = BreakableObjectState.Fixed;

        private NetworkVariable<BreakableObjectState> brokenState;

        public GameObject fixedPrefab;
        public GameObject brokenPrefab;
        public float despawnTime;

        private GameObject fixedSpawn;
        private GameObject brokenSpawn;
        private float brokenElapsed;

        public GameObject CurrentPreviewState { get; set; }
        public GameObject CurrentPreview { get; set; }

        public GameObject PreviewPrefab()
        {
            switch (defaultState)
            {
                case BreakableObjectState.Fixed:
                    return fixedPrefab;
                case BreakableObjectState.Broken:
                    return brokenPrefab;
                default:
                    return null;
            }
        }

        public void Awake()
        {
            if (CurrentPreview != null)
            {
                GameObject.Destroy(CurrentPreview);
                CurrentPreview = null;
            }

            brokenState = new NetworkVariable<BreakableObjectState>(value: defaultState);
            UpdatePrefabState();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                Damageable damageable = GetComponent<Damageable>();
                damageable.ResetToMaxHealth();
                brokenState.Value = defaultState;
            }

            base.OnNetworkSpawn();
            UpdatePrefabState();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer)
            {
                brokenState.Value = defaultState;
            }

            UpdatePrefabState();
        }

        public void Update()
        {
            if (IsSpawned && IsServer)
            {
                UpdateObjectState();
            }

            if (IsSpawned)
            {
                UpdatePrefabState();
            }
        }

        public void UpdateObjectState()
        {
            switch (brokenState.Value)
            {
                case BreakableObjectState.Broken:
                    brokenElapsed += Time.deltaTime;
                    if (brokenElapsed >= despawnTime)
                    {
                        brokenState.Value = BreakableObjectState.CleanedUp;
                    }

                    break;
                case BreakableObjectState.Fixed:
                    if (!GetComponent<Damageable>().IsAlive())
                    {
                        brokenElapsed = 0.0f;
                        brokenState.Value = BreakableObjectState.Broken;
                    }

                    break;
                case BreakableObjectState.CleanedUp:
                    if (GetComponent<Damageable>().IsAlive())
                    {
                        brokenState.Value = BreakableObjectState.Fixed;
                    }

                    break;
            }
        }

        public void UpdatePrefabState()
        {
            if (brokenState.Value == BreakableObjectState.Fixed)
            {
                fixedSpawn ??= GameObject.Instantiate(fixedPrefab, transform.position, transform.rotation, transform);
                foreach (GenericHitbox hitbox in GetComponentsInChildren<GenericHitbox>())
                {
                    hitbox.Disabled = false;
                }
            }
            else if (fixedSpawn != null)
            {
                GameObject.Destroy(fixedSpawn);
                fixedSpawn = null;
            }

            if (brokenState.Value == BreakableObjectState.Broken)
            {
                brokenSpawn ??= GameObject.Instantiate(brokenPrefab, transform.position, transform.rotation, transform);
                foreach (GenericHitbox hitbox in GetComponentsInChildren<GenericHitbox>())
                {
                    hitbox.Disabled = true;
                }
            }
            else if (brokenSpawn != null)
            {
                GameObject.Destroy(brokenSpawn);
                brokenSpawn = null;
            }
        }

        public void UpdatePreviewState()
        {
            if (Application.isPlaying)
            {
                return;
            }

            GameObject desiredState = PreviewPrefab();
            if (CurrentPreviewState != desiredState)
            {
                if (CurrentPreview != null)
                {
                    GameObject.DestroyImmediate(CurrentPreview);
                }

                if (desiredState != null)
                {
                    CurrentPreview = GameObject.Instantiate(desiredState, transform.position, transform.rotation, transform);
                    CurrentPreview.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            CurrentPreviewState = desiredState;
        }
    }
}
