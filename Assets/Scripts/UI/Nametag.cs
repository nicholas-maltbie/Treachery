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
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

namespace nickmaltbie.Treachery.UI
{
    public class Nametag : NetworkBehaviour
    {
        public static readonly Quaternion FlipRotation = Quaternion.Euler(0, 180, 0);

        public string defaultName;

        public TMP_Text nametagPrefab;

        public bool displayWhenOwned = true;

        public Vector3 nametagOffset = Vector3.up * 2;

        public string EntityName => nametagText.Value.ToString();

        protected NetworkVariable<FixedString32Bytes> nametagText = new NetworkVariable<FixedString32Bytes>(
            value: "",
            writePerm: NetworkVariableWritePermission.Owner);

        protected TMP_Text text;

        private GameObject spawnedNametag;

        public void Awake()
        {
            spawnedNametag = GameObject.Instantiate(nametagPrefab.gameObject, transform.position + nametagOffset, Quaternion.identity, transform);
            text = spawnedNametag.GetComponent<TMP_Text>();
        }

        public void OnEnable()
        {
            nametagText.OnValueChanged += UpdateText;
        }

        public void OnDisable()
        {
            nametagText.OnValueChanged -= UpdateText;
        }

        public void UpdateName(string newName)
        {
            nametagText.Value = newName;
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner && string.IsNullOrEmpty(nametagText.Value.ToString()))
            {
                nametagText.Value = defaultName;
            }

            text.text = nametagText.Value.ToString();
        }

        public void UpdateText(FixedString32Bytes previousName, FixedString32Bytes newName)
        {
            text.text = newName.ToString();
        }

        public void LateUpdate()
        {
            if (displayWhenOwned == false && IsOwner)
            {
                spawnedNametag.gameObject.SetActive(false);
            }
            else
            {
                spawnedNametag.gameObject.SetActive(true);
                spawnedNametag.transform.LookAt(Camera.main.transform, Vector3.up);
                spawnedNametag.transform.rotation *= FlipRotation;
            }
        }
    }
}
