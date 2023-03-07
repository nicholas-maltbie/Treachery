
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public class GeneratedItemSpawner : NetworkBehaviour
    {
        [SerializeField]
        public GeneratedWorldItem worldItemPrefab;

        [SerializeField]
        public EquipmentLibrary library;

        [SerializeField]
        public int startupEquipment = IEquipment.EmptyEquipmentId;

        public GameObject CurrentPreviewState { get; set; }
        public GameObject CurrentPreview { get; set; }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"Attempting to setup Generated item spawner.");
            if (IsServer)
            {
                GeneratedWorldItem item = GameObject.Instantiate(worldItemPrefab, transform.position, transform.rotation);
                NetworkObject netObj = item.GetComponent<NetworkObject>();
                item.SetEquipment(startupEquipment);
                netObj.Spawn();
            }

            base.OnNetworkSpawn();
        }

        public GameObject PreviewPrefab()
        {
            if (startupEquipment != IEquipment.EmptyEquipmentId)
            {
                return library.GetEquipment(startupEquipment).HeldPrefab;
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
                    CurrentPreview.hideFlags = HideFlags.DontSave;
                }
            }

            CurrentPreviewState = desiredState;
        }
    }
}
