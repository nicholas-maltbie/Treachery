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

using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public class GeneratedItemSpawner : NetworkBehaviour
    {
        [SerializeField]
        public int startupEquipment = IEquipment.EmptyEquipmentId;

        public GameObject CurrentPreviewState { get; set; }
        public GameObject CurrentPreview { get; set; }

        private bool spawned = false;

        public void Update()
        {
            if (IsServer && !spawned)
            {
                GeneratedWorldItem item = GameObject.Instantiate(EquipmentLibrary.Singleton.WorldItemPrefab, transform.position, transform.rotation);
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                netObj.Spawn();
                item.SetEquipment(startupEquipment);
                spawned = true;
                NetworkManager.Singleton.OnServerStarted += ResetState;
            }
        }

        public void ResetState()
        {
            spawned = false;
            NetworkManager.Singleton.OnServerStarted -= ResetState;
        }

        public GameObject PreviewPrefab()
        {
            if (startupEquipment != IEquipment.EmptyEquipmentId)
            {
                return EquipmentLibrary.Singleton?.GetEquipment(startupEquipment).HeldPrefab;
            }

            return null;
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
                    CurrentPreview.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                }
            }

            CurrentPreviewState = desiredState;
        }
    }
}
