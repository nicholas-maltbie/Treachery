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
using UnityEditor;
using UnityEngine;

namespace nickmaltbie.Treachery.DebugScripts
{
    [CustomEditor(typeof(GeneratedItemSpawner))]
    public class GeneratedItemSpawnerDebug : Editor
    {
        public int _selected = 0;
        public static EquipmentLibrary library;
        public static IEquipment[] equipment;
        public static GUIContent[] equipmentIcons;
        private SerializedProperty startupEquipment;

        [InitializeOnLoadMethod]
        public static void Setup()
        {
            EditorApplication.update -= VerifyGenerateWorldItem;
            EditorApplication.update += VerifyGenerateWorldItem;
        }

        public void OnEnable()
        {
            startupEquipment = serializedObject.FindProperty("startupEquipment");
        }

        public static void VerifyGenerateWorldItem()
        {
            foreach (GeneratedItemSpawner item in GameObject.FindObjectsOfType<GeneratedItemSpawner>())
            {
                item.UpdatePreviewState();
            }
        }

        public static void ResetCache()
        {
            library = EquipmentLibrary.Singleton;
            equipment ??= EquipmentLibrary.Singleton?.EnumerateEquipment().ToArray();
            equipmentIcons ??= equipment.Select(equipment => new GUIContent(equipment.HeldPrefab?.name, equipment.ItemIcon.texture)).ToArray();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();

            var item = target as GeneratedItemSpawner;

            if (library != EquipmentLibrary.Singleton || equipment.Length != EquipmentLibrary.Singleton.EquipmentCount())
            {
                ResetCache();
            }
            
            if (EquipmentLibrary.Singleton?.HasEquipment(item.startupEquipment) ?? false)
            {
                for (int i = 0; i < equipment.Length; i++)
                {
                    if (equipment[i].EquipmentId == item.startupEquipment)
                    {
                        _selected = i;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            _selected = EditorGUILayout.Popup(
                new GUIContent("Select Equipment"),
                _selected,
                equipmentIcons);

            if (EditorGUI.EndChangeCheck())
            {
                item.startupEquipment = equipment[_selected].EquipmentId;
                startupEquipment.intValue = item.startupEquipment;
                serializedObject.ApplyModifiedProperties();

                if (EquipmentLibrary.Singleton?.HasEquipment(item.startupEquipment) ?? false)
                {
                    IEquipment equipment = EquipmentLibrary.Singleton.GetEquipment(item.startupEquipment);
                    equipment.WorldShape.AttachCollider(item.CurrentPreview.gameObject, destroyImmediate: true);
                }
            }
        }
    }
}
