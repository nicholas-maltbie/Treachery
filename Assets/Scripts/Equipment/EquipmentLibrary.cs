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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    [CreateAssetMenu(fileName = "EquipmentLibrary", menuName = "ScriptableObjects/EquipmentLibrary", order = 1)]
    public class EquipmentLibrary : ScriptableObject
    {
        public static EquipmentLibrary Singleton { get; internal set; }

        [SerializeField]
        private GameObject worldItemPrefab;

        [SerializeField]
        private GameObject[] equipment;

        private Dictionary<int, IEquipment> _equipmentLookup;

        public GeneratedWorldItem WorldItemPrefab => worldItemPrefab?.GetComponent<GeneratedWorldItem>();

        public void Reinitialize()
        {
            _equipmentLookup = null;
            Initialize();
        }

        public void OnValidate()
        {
            Reinitialize();
        }

        public int EquipmentCount()
        {
            return _equipmentLookup.Count;
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
