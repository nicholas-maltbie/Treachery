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
using System.Collections.Generic;
using System.Linq;
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using nickmaltbie.Treachery.Interactive.Stamina;
using nickmaltbie.Treachery.Player;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public enum MeleeAttackType
    {
        Punch,
        Basic,
        Stab,
        Cleave
    }

    public class MeleeWeapon : AbstractEquipment, IWeapon
    {
        public const int DegreesPerRay = 5;
        public const int VerticalAttackDegrees = 90;
        public const int MaxHitsPerRay = 10;

        public MeleeAttackType attackType;
        public DamageType damageType = DamageType.Slashing;
        public float degreeRange = 15;
        public float attackRange = 1.0f;
        public bool peirce = false;
        public float damage = 20;
        public float cooldown = 0.1f;
        public float staminaCost = 10;

        public WeaponType WeaponType => WeaponType.Melee;

        protected IDamageable Source { get; set; }
        protected Vector3 AttackBaseOffset { get; set; }
        protected ICameraControls viewHeading { get; set; }
        protected Transform PlayerPosition { get; set; }
        protected IDamageActor DamageActor { get; set; }
        public IActionActor<PlayerAction> Actor { get; set; }

        private RaycastHit[] HitCache = new RaycastHit[MaxHitsPerRay];

        public override bool DisableDefaultPrimary => true;

        public override void SetupItemAction(GameObject player, IActionActor<PlayerAction> actor, IStaminaMeter stamina)
        {
            ItemAction = new ItemAction(
                InputAction,
                actor,
                stamina,
                this,
                PlayerAction.MeleeAttack,
                cooldown,
                staminaCost,
                performWhileHeld: true);
            ItemAction.Setup();

            Source = player.GetComponent<IDamageable>();
            viewHeading = player.GetComponent<ICameraControls>();
            AttackBaseOffset = player.GetComponent<IManagedCamera>().CameraBase.localPosition;
            DamageActor = player.GetComponent<IDamageActor>();
            PlayerPosition = player.transform;
            Actor = actor;
        }

        public IEnumerable<Quaternion> GetOffsets()
        {
            return GetOffsetsDegrees().Select((rot) => Quaternion.Euler(rot.Item2, rot.Item1, 0));
        }

        public IEnumerable<(float, float)> GetOffsetsDegrees()
        {
            // Get the offsets of the attack for attacks with
            // a spreading range.
            for (int yawStep = 0; yawStep <= Mathf.CeilToInt(degreeRange / DegreesPerRay); yawStep++)
            {
                float yawOffset = Mathf.Clamp(DegreesPerRay * yawStep, -degreeRange, degreeRange);

                for (int pitchStep = 0; pitchStep <= Mathf.CeilToInt(VerticalAttackDegrees / DegreesPerRay); pitchStep++)
                {
                    float pitchOffset = Mathf.Clamp(DegreesPerRay * pitchStep, -VerticalAttackDegrees, VerticalAttackDegrees);

                    if (yawOffset == 0 && pitchOffset == 0)
                    {
                        yield return (yawOffset, pitchOffset);
                    }
                    else if (yawOffset != 0 && pitchOffset == 0)
                    {
                        yield return (yawOffset, pitchOffset);
                        yield return (-yawOffset, pitchOffset);
                    }
                    else if (yawOffset == 0 && pitchOffset != 0)
                    {
                        yield return (yawOffset, pitchOffset);
                        yield return (yawOffset, -pitchOffset);
                    }
                    else
                    {
                        // if (yawOffset != 0 && pitchOffset != 0)
                        yield return (yawOffset, pitchOffset);
                        yield return (yawOffset, -pitchOffset);
                        yield return (-yawOffset, pitchOffset);
                        yield return (-yawOffset, -pitchOffset);
                    }
                }
            }
        }

        private DamageEvent GetBasicAttack(Quaternion heading, Vector3 source)
        {
            RaycastHit raycastHit = default;
            IHitbox closestHit = null;
            float closestDistance = Mathf.Infinity;

            // Get all the rays for the attack.
            foreach (Quaternion offset in GetOffsets())
            {
                // Add the offset to the current heading.
                Quaternion rotation = heading * offset;

                // Draw a ray from that point outwards
                Vector3 dir = rotation * Vector3.forward;
                int hitCount = Physics.RaycastNonAlloc(source, dir, HitCache, attackRange, IHitbox.HitLayerMaskComputation, QueryTriggerInteraction.Collide);
                IEnumerable<RaycastHit> hits = Enumerable.Range(0, hitCount).Select(idx => HitCache[idx]);
                IHitbox hit = IHitbox.GetFirstValidHit(hits, Source, out RaycastHit firstHit, out bool didHit);
                
                if (didHit && firstHit.distance < closestDistance)
                {
                    raycastHit = firstHit;
                    closestHit = hit;
                    closestDistance = firstHit.distance;
                }
            }

            if (closestHit != null)
            {
                return IHitbox.DamageEventFromHit(raycastHit, closestHit, damage, raycastHit.normal, damageType);
            }
            else
            {
                return IHitbox.EmptyDamageEvent(Interactive.Health.EventType.Damage, damage);
            }
        }

        public override void PerformAction()
        {
            Actor.RaiseEvent(new MeleeAttackEvent(attackType));
            Vector3 source = PlayerPosition.position + AttackBaseOffset;
            switch (attackType)
            {
                case MeleeAttackType.Basic:
                default:
                    DamageEvent basicAttack = GetBasicAttack(viewHeading.PlayerHeading, source);

                    if (basicAttack.target != null)
                    {
                        DamageActor.AttackServerRpc(basicAttack);
                    }

                    break;
            }
        }
    }
}
