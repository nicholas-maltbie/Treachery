
using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

        public InputActionReference incrementLoadoutSelection;
        public InputActionReference decrementLoadoutSelection;
        public InputActionReference loadoutScroll;

        private EquipmentLoadout[] loadouts;
        private NetworkVariable<int> currentLoadout = new NetworkVariable<int>(
            value: 0,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkList<NetworkEquipmentLoadout> networkLoadouts;

        private KeyControl GetDigitKey(int index)
        {
            switch (index)
            {
                case 0: return Keyboard.current.digit0Key;
                case 1: return Keyboard.current.digit1Key;
                case 2: return Keyboard.current.digit2Key;
                case 3: return Keyboard.current.digit3Key;
                case 4: return Keyboard.current.digit4Key;
                case 5: return Keyboard.current.digit5Key;
                case 6: return Keyboard.current.digit6Key;
                case 7: return Keyboard.current.digit7Key;
                case 8: return Keyboard.current.digit8Key;
                case 9: return Keyboard.current.digit9Key;
            }

            return Keyboard.current.digit0Key;
        }

        public void Awake()
        {
            loadouts = Enumerable.Range(0, MaxLoadouts).Select(_ => new EquipmentLoadout()).ToArray();
            networkLoadouts = new NetworkList<NetworkEquipmentLoadout>(
                Enumerable.Range(0, MaxLoadouts).Select(_ => new NetworkEquipmentLoadout()),
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Owner);
            currentLoadout.OnValueChanged += OnLoadoutSelected;
            networkLoadouts.OnListChanged += OnLoadoutModified;

            loadouts[0].SetActive(true);

            incrementLoadoutSelection.action.Enable();
            decrementLoadoutSelection.action.Enable();
            loadoutScroll.action.Enable();

            for (int i = 0; i < MaxLoadouts; i++)
            {
                if (Keyboard.current != null)
                {
                    var selectSlot = new InputAction(
                        name: $"Select Slot {i}",
                        type: InputActionType.Button,
                        binding: GetDigitKey(i + 1).path,
                        interactions: "Press"
                    );

                    int selected = i;
                    selectSlot.performed += _ =>
                    {
                        ChangeSelectedLoadout(selected);
                    };

                    selectSlot.Enable();
                }
            }

            incrementLoadoutSelection.action.performed += _ => IncrementLoadout();
            decrementLoadoutSelection.action.performed += _ => DecrementLoadout();
            loadoutScroll.action.performed += (action) =>
            {
                float value = action.ReadValue<float>();
                if (value > 0)
                {
                    IncrementLoadout();
                }
                else
                {
                    DecrementLoadout();
                }
            };
        }

        private void IncrementLoadout() => ChangeSelectedLoadout((CurrentSelected + 1) % MaxLoadouts);
        private void DecrementLoadout() => ChangeSelectedLoadout((CurrentSelected - 1 + MaxLoadouts) % MaxLoadouts);

        public EquipmentLoadout CurrentLoadout => loadouts[currentLoadout.Value];
        public EquipmentLoadout GetLoadout(int idx) => loadouts[idx];

        public int CurrentSelected => currentLoadout.Value;

        public void ChangeSelectedLoadout(int selected)
        {
            if (!IsOwner)
            {
                return;
            }

            currentLoadout.Value = selected;
        }

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
