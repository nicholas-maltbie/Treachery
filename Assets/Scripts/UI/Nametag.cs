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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace nickmaltbie.Treachery.UI
{
    public class Nametag : MonoBehaviour
    {
        public static readonly Quaternion FlipRotation = Quaternion.Euler(0, 180, 0);

        public string nametagText;

        public TMP_Text nametagPrefab;

        public Vector3 nametagOffset = Vector3.up * 2;

        private GameObject spawnedNametag;

        private TMP_Text text;

        public void Awake()
        {
            spawnedNametag = GameObject.Instantiate(nametagPrefab.gameObject, transform.position + nametagOffset, Quaternion.identity, transform);
            text = spawnedNametag.GetComponent<TMP_Text>();
            text.text = nametagText;
        }

        public void UpdateText(string newName)
        {
            nametagText = newName;
            text.text = newName;
        }

        public void Update()
        {
            spawnedNametag.transform.LookAt(Camera.main.transform, Vector3.up);
            spawnedNametag.transform.rotation *= FlipRotation;
        }
    }
}
