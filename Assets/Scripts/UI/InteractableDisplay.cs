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

using nickmaltbie.Treachery.Interactive;
using nickmaltbie.Treachery.Player.Action;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace nickmaltbie.Treachery.UI
{
    public class InteractableDisplay : MonoBehaviour
    {
        public Image interactiveSprite;
        public TMP_Text text;

        public void Update()
        {
            NetworkObject localPlayer = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
            if (localPlayer == null)
            {
                return;
            }

            InteractActionBehaviour interact = localPlayer.GetComponent<InteractActionBehaviour>();
            if (interact.Focus is IInteractive focus)
            {
                string labelText = $"Press [E] to interact";
                if (!string.IsNullOrEmpty(focus.InteractionText))
                {
                    labelText += "\n" + focus.InteractionText;
                }

                interactiveSprite.enabled = true;
                text.enabled = true;
                interactiveSprite.sprite = focus.InteractiveIcon;
                text.text = labelText;
            }
            else
            {
                interactiveSprite.enabled = false;
                text.enabled = false;
            }
        }
    }
}
