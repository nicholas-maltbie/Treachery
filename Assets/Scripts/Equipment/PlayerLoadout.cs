
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public struct NetworkEquipmentLoadout : INetworkSerializable, IEquatable<NetworkEquipmentLoadout>
    {
        public int mainItemId;
        public int offhandItemId;

        public bool Equals(NetworkEquipmentLoadout other)
        {
            return other.mainItemId == mainItemId && other.offhandItemId == offhandItemId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mainItemId);
            serializer.SerializeValue(ref offhandItemId);
        }

        public void Synchronize(EquipmentLoadout loadout)
        {
            this.mainItemId = loadout.MainItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;
            this.offhandItemId = loadout.OffhandItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;
        }
    }

    public class EquipmentLoadout
    {
        public bool activeState = false;
        public GameObject Main { get; private set; }
        public GameObject Offhand { get; private set; }

        public IEquipment MainItem => Main?.GetComponent<IEquipment>();
        public IEquipment OffhandItem => Offhand?.GetComponent<IEquipment>();

        public int MainItemId => MainItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;
        public int OffhandItemId => OffhandItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;

        public static implicit operator NetworkEquipmentLoadout(EquipmentLoadout loadout)
        {
            return new NetworkEquipmentLoadout()
            {
                mainItemId = loadout.MainItemId,
                offhandItemId = loadout.OffhandItemId,
            };
        }

        public bool HasMain => Main != null;
        public bool HasOffhand => Offhand != null;
        public bool CahEquipOffhand => !HasOffhand && (!HasMain || MainItem.Weight == EquipmentWeight.OneHanded);

        public bool RemoveItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Main:
                    bool hasMain = Main != null;
                    this.UpdateMainItem(IEquipment.EmptyEquipmentId, null, null);
                    return hasMain;
                case ItemType.Offhand:
                    bool hasOffhand = Offhand != null;
                    this.UpdateOffhandItem(IEquipment.EmptyEquipmentId, null, null);
                    return hasOffhand;
            }

            return false;
        }

        public bool EquipItem(int equipmentId, Transform parent, EquipmentLibrary library)
        {
            IEquipment equipment = library.GetEquipment(equipmentId);
            if (!CanEquip(equipment))
            {
                return false;
            }

            if (equipment.ItemType == ItemType.Main)
            {
                UpdateMainItem(equipmentId, parent, library);
            }
            else if (equipment.ItemType == ItemType.Offhand)
            {
                UpdateOffhandItem(equipmentId, parent, library);
            }

            return true;
        }

        public bool CanEquip(IEquipment equipment)
        {
            switch (equipment.ItemType)
            {
                case ItemType.Offhand:
                    return CahEquipOffhand;
                case ItemType.Main:
                    return !HasMain && (!HasOffhand || equipment.Weight == EquipmentWeight.OneHanded);
            }

            return false;
        }

        public void SetActive(bool state)
        {
            activeState = state;
            Main?.SetActive(state);
            Offhand?.SetActive(state);
        }

        public void UpdateItemPositions(EquipmentManager manager)
        {
            UnityEngine.Debug.Log($"Loadout attempting to update item positions with manager. Main:{Main} Offhand:{Offhand}");
            if (Main != null)
            {
                Main.transform.SetParent(manager.GetMainHand);
                Main.transform.localPosition = Vector3.zero;
                Main.transform.localRotation = Quaternion.identity;
            }
            
            if (Offhand != null)
            {
                Offhand.transform.SetParent(manager.GetOffHand);
                Offhand.transform.localPosition = Vector3.zero;
                Offhand.transform.localRotation = Quaternion.identity;
            }
        }

        public void UpdateFromNetworkState(NetworkEquipmentLoadout loadout, Transform parent, EquipmentLibrary library)
        {
            UpdateMainItem(loadout.mainItemId, parent, library);
            UpdateOffhandItem(loadout.offhandItemId, parent, library);
        }

        public void UpdateOffhandItem(int equipmentId, Transform parent, EquipmentLibrary library)
        {
            if (Offhand != null && OffhandItem.EquipmentId != equipmentId)
            {
                GameObject.Destroy(Main);
            }

            if (Offhand == null && equipmentId != IEquipment.EmptyEquipmentId)
            {
                GameObject prefab = library.GetEquipment(equipmentId).HeldPrefab;
                Offhand = GameObject.Instantiate(prefab, parent);
                Offhand.SetActive(activeState);
            }
        }

        public void UpdateMainItem(int equipmentId, Transform parent, EquipmentLibrary library)
        {
            if (Main != null && MainItem.EquipmentId != equipmentId)
            {
                GameObject.Destroy(Main);
            }

            if (Main == null && equipmentId != IEquipment.EmptyEquipmentId)
            {
                GameObject prefab = library.GetEquipment(equipmentId).HeldPrefab;
                Main = GameObject.Instantiate(prefab, parent);
                Main.SetActive(activeState);
            }
        }
    }

    public class PlayerLoadout : NetworkBehaviour
    {
        [SerializeField]
        public int MaxLoadouts = 3;

        [SerializeField]
        public EquipmentLibrary library;

        [SerializeField]
        public EquipmentManager manager;

        private EquipmentLoadout[] loadouts;
        private NetworkVariable<int> currentLoadout = new NetworkVariable<int>(
            value: 0,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkList<NetworkEquipmentLoadout> networkLoadouts;

        public void Awake()
        {
            loadouts = Enumerable.Range(0, MaxLoadouts).Select(_ => new EquipmentLoadout()).ToArray();
            networkLoadouts = new NetworkList<NetworkEquipmentLoadout>(
                Enumerable.Range(0, MaxLoadouts).Select(_ => new NetworkEquipmentLoadout()),
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Owner);
            currentLoadout.OnValueChanged += OnLoadoutSelected;
            networkLoadouts.OnListChanged += OnLoadoutModified;

            this.loadouts[0].SetActive(true);
        }

        public EquipmentLoadout CurrentLoadout => loadouts[currentLoadout.Value];
        public EquipmentLoadout GetLoadout(int idx) => loadouts[idx];

        public int CurrentSelected => currentLoadout.Value;

        public void RemoveItemFromLoadout(ItemType itemType, int slot)
        {
            if (IsOwner)
            {
                EquipmentLoadout loadout = loadouts[slot];
                loadout.RemoveItem(itemType);
            }
        }

        public void AddItemToLoadout(int equipmentId, int slot)
        {
            if (IsOwner)
            {
                EquipmentLoadout loadout = loadouts[slot];
                bool didEquip = loadout.EquipItem(equipmentId, transform, library);
                UnityEngine.Debug.Log($"Attempting to equip item with id:{equipmentId}. didEquip:{didEquip}");
                loadout.UpdateItemPositions(manager);
            }
        }

        public void SynchronizeLoadouts()
        {
            for (int i = 0; i < MaxLoadouts; i++)
            {
                networkLoadouts[i].Synchronize(loadouts[i]);
            }
        }

        public void Update()
        {
            if (IsOwner)
            {
                SynchronizeLoadouts();
            }
        }

        public void OnLoadoutSelected(int previousLoadout, int currentLoadout)
        {
            EquipmentLoadout previous = this.loadouts[previousLoadout];
            EquipmentLoadout current = this.loadouts[currentLoadout];

            previous.SetActive(false);
            current.SetActive(true);
            current.UpdateItemPositions(manager);
        }

        public void OnLoadoutModified(NetworkListEvent<NetworkEquipmentLoadout> changeEvent)
        {
            EquipmentLoadout modified = this.loadouts[changeEvent.Index];
            modified.UpdateFromNetworkState(changeEvent.Value, transform, library);
            modified.UpdateItemPositions(manager);
        }
    }
}