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

using System.Linq;
using nickmaltbie.Treachery.Equipment;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace nickmaltbie.Treachery.DebugScripts
{
    [CustomEditor(typeof(PlayerLoadout))]
    public class DebugAddEquipment : Editor
    {
        public int _selected = 0;
        public IEquipment[] equipment;
        public GUIContent[] equipmentIcons;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var loadout = target as PlayerLoadout;

            if (NetworkManager.Singleton?.IsServer ?? false && loadout.GetComponent<NetworkObject>().IsSpawned)
            {
                var library = loadout.library;
                equipment ??= library.EnumerateEquipment().ToArray();
                equipmentIcons ??= equipment.Select(equipment => new GUIContent(equipment.HeldPrefab?.name, equipment.ItemIcon.texture)).ToArray();
                _selected = EditorGUILayout.Popup(
                    new GUIContent("Select Equipment"),
                    _selected,
                    equipmentIcons);

                if (GUILayout.Button("Add Item to Loadout"))
                {
                    IEquipment selected = equipment[_selected];
                    loadout.AddItemToLoadout(selected.EquipmentId, 0);
                }
            }
        }
    }
}
