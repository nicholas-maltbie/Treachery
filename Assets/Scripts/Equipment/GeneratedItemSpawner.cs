
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
