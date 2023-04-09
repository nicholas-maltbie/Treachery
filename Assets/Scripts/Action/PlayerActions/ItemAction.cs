
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
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    public class ItemAction : ActorConditionalAction<PlayerAction>
    {
        private float lastActivateTime = Mathf.NegativeInfinity;

        private float swapCooldown;

        private IEquipment equipment;

        public ItemAction(
            InputActionReference actionReference,
            IActionActor<PlayerAction> actor,
            IStaminaMeter stamina,
            IEquipment equipment,
            PlayerAction action,
            float cooldown = 0.0f,
            float staminaCost = 0.0f,
            float swapCooldown = 0.5f,
            bool performWhileHeld = false)
            : base(actionReference, actor, stamina, action, cooldown, staminaCost, performWhileHeld)
        {
            this.equipment = equipment;
            this.swapCooldown = swapCooldown;
        }

        protected override void Perform()
        {
            equipment.PerformAction();
        }

        public override void SetActive(bool state)
        {
            lastActivateTime = Time.time;
            base.SetActive(state);
        }

        protected override bool Condition()
        {
            return Time.time >= lastActivateTime + swapCooldown && base.Condition();
        }

        public override float CooldownTime => cooldown;
    }
}
