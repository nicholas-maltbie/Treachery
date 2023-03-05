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

using nickmaltbie.Treachery.Equipment;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.UI
{
    public class LoadoutViewer : MonoBehaviour
    {
        public LoadoutSlotView slotViewPrefab;

        public LoadoutSlotView[] spawned;

        public void Setup(PlayerLoadout loadout)
        {
            if (spawned != null || loadout == null)
            {
                return;
            }

            spawned = new LoadoutSlotView[loadout.MaxLoadouts];

            for (int i = 0; i < loadout.MaxLoadouts; i++)
            {
                spawned[i] = GameObject.Instantiate(slotViewPrefab, transform);
                RectTransform rect = spawned[i].GetComponent<RectTransform>();
                spawned[i].transform.localPosition = Vector3.down * i * (rect.sizeDelta.y + 5);
            }
        }

        public void TearDown()
        {
            if (spawned == null)
            {
                return;
            }

            foreach (LoadoutSlotView view in spawned)
            {
                GameObject.Destroy(view.gameObject);
            }

            spawned = null;
        }

        public void Update()
        {
            NetworkObject localPlayer = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
            if (localPlayer == null)
            {
                TearDown();
                return;
            }

            PlayerLoadout playerLoadout = localPlayer.GetComponent<PlayerLoadout>();
            Setup(playerLoadout);

            if (playerLoadout)
            {
                for (int i = 0; i < playerLoadout.MaxLoadouts; i++)
                {
                    EquipmentLoadout loadoutToUpdate = playerLoadout.GetLoadout(i);
                    spawned[i].UpdateView(loadoutToUpdate);
                    spawned[i].SetSelected(i == playerLoadout.CurrentSelected);
                }
            }
        }
    }
}
