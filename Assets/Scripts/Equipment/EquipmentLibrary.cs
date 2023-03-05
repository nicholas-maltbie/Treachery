
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public class EquipmentLibrary : ScriptableObject
    {
        [SerializeField]
        public IEquipment[] equipment;

        private Dictionary<int, IEquipment> _equipmentLookup;

        public void Reinitialize()
        {
            _equipmentLookup = null;
            Initialize();
        }

        public void Initialize()
        {
            if (_equipmentLookup != null)
            {
                return;
            }

            _equipmentLookup = equipment.ToDictionary(equipment => equipment.EquipmentId);
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
