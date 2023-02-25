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

using System;
using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Environment
{
    public class ArrowAimAttribute : Attribute
    {
        public static ArrowAimAttribute Disabled = new ArrowAimAttribute { showArrow = false, drawingArrow = false };

        public bool showArrow = false;
        public bool drawingArrow = false;
    }

    public class StaticArcher : NetworkSMAnim
    {
        public float arrowFireSpeed = 50.0f;

        public class DrawArrowEvent : IEvent { }

        public const string ArcherIdleAnimState = "Idle";
        public const string EquipBowAnimState = "standing draw arrow";
        public const string DrawArrowAnimState = "standing draw arrow";
        public const string AimAnimState = "standing arrow drawn";
        public const string FireArrowAnimState = "standing aim recoil";

        [InitialState]
        [Animation(ArcherIdleAnimState, 0.35f, true)]
        [Transition(typeof(DrawArrowEvent), typeof(DrawArrowState))]
        [TransitionOnAnimationComplete(typeof(DrawArrowState))]
        [ArrowAim(showArrow = false, drawingArrow = false)]
        public class IdleState : State { }

        [Animation(EquipBowAnimState, 0.35f, true)]
        [TransitionOnAnimationComplete(typeof(DrawArrowState))]
        [ArrowAim(showArrow = true, drawingArrow = false)]
        public class EquipBowState : State { }

        [Animation(DrawArrowAnimState, 0.35f, true)]
        [TransitionOnAnimationComplete(typeof(AimState))]
        [ArrowAim(showArrow = true, drawingArrow = true)]
        public class DrawArrowState : State { }

        [TransitionAfterTime(typeof(FireState), 0.1f)]
        [ArrowAim(showArrow = true, drawingArrow = true)]
        public class AimState : State { }

        [Animation(FireArrowAnimState, 0.35f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState))]
        [ArrowAim(showArrow = false, drawingArrow = false)]
        [OnEnterState(nameof(FireArrow))]
        public class FireState : State { }

        public ArrowAimHelper aimHelper;

        public override void Start()
        {
            base.Start();
            NetworkManager.AddNetworkPrefab(aimHelper.arrowPrefab.gameObject);
            aimHelper ??= GetComponentInChildren<ArrowAimHelper>();
        }

        public void FireArrow()
        {
            if (IsServer)
            {
                Transform arrowTransform = aimHelper.ArrowTransform;
                Arrow firedArrow = GameObject.Instantiate(aimHelper.arrowPrefab, arrowTransform.position, arrowTransform.rotation);
                firedArrow.GetComponent<NetworkObject>().Spawn(true);

                Transform targetPosition = aimHelper.targetPosition;
                Vector3 dir = targetPosition.position - firedArrow.transform.position;
                firedArrow.Loose(dir.normalized, arrowFireSpeed);
            }
        }

        public override void Update()
        {
            base.Update();
            var aimAttribute = Attribute.GetCustomAttribute(CurrentState, typeof(ArrowAimAttribute)) as ArrowAimAttribute;
            aimAttribute ??= ArrowAimAttribute.Disabled;

            aimHelper.ShowArrow = aimAttribute.showArrow;
            aimHelper.DrawingArrow = aimAttribute.drawingArrow;
        }
    }
}
