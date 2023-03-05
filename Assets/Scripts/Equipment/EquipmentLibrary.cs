
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    [CreateAssetMenu(fileName = "EquipmentLibrary", menuName = "ScriptableObjects/EquipmentLibrary", order = 1)]
    public class EquipmentLibrary : ScriptableObject
    {
        [SerializeField]
        private GameObject[] equipment;

        private Dictionary<int, IEquipment> _equipmentLookup;

        public void Reinitialize()
        {
            _equipmentLookup = null;
            Initialize();
        }

        public IEnumerable<IEquipment> EnumerateEquipment()
        {
            Initialize();
            return _equipmentLookup.Values;
        }

        public void Initialize()
        {
            if (_equipmentLookup != null)
            {
                return;
            }

            _equipmentLookup = equipment
                .Select(equip => equip.GetComponent<IEquipment>())
                .ToDictionary(equipment => equipment.EquipmentId);
        }

        public bool HasEquipment(int id)
        {
            Initialize();
            return _equipmentLookup.ContainsKey(id);
        }

        public IEquipment GetEquipment(int id)
        {
            Initialize();
            return _equipmentLookup[id];
        }
    }
}
