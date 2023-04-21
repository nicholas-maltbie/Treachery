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
        public const int MaxHitsPerRay = 20;

        public MeleeAttackType attackType;
        public DamageType damageType = DamageType.Slashing;
        public float horizontalDegreeRange = 15;
        public float verticalDegreeRange = 90;
        public float attackRange = 1.0f;
        public float damage = 20;
        public float cooldown = 1.0f;
        public float staminaCost = 10;

        public WeaponType WeaponType => WeaponType.Melee;

        protected IDamageable Source { get; set; }
        protected Vector3 AttackBaseOffset { get; set; }
        protected IManagedCamera viewHeading { get; set; }
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
            viewHeading = player.GetComponent<IManagedCamera>();
            AttackBaseOffset = player.GetComponent<SurvivorCameraController>().InitialCameraPosition;
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
            for (int yawStep = 0; yawStep <= Mathf.CeilToInt(horizontalDegreeRange / DegreesPerRay); yawStep++)
            {
                float yawOffset = Mathf.Clamp(DegreesPerRay * yawStep, -horizontalDegreeRange, horizontalDegreeRange);

                for (int pitchStep = 0; pitchStep <= Mathf.CeilToInt(verticalDegreeRange / DegreesPerRay); pitchStep++)
                {
                    float pitchOffset = Mathf.Clamp(DegreesPerRay * pitchStep, -verticalDegreeRange, verticalDegreeRange);
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

        private IEnumerable<DamageEvent> GetTargets(Quaternion heading, Vector3 source, bool pierce, int maxTargets)
        {
            var hitLookup = new Dictionary<IDamageable, (RaycastHit, IHitbox)>();

            // Get all the rays for the attack.
            foreach (Quaternion offset in GetOffsets())
            {
                // Add the offset to the current heading
                // and draw a ray from that point outwards
                Quaternion rotation = heading * offset;
                Vector3 dir = rotation * Vector3.forward;

                // Get the hit targets
                int hitCount = Physics.RaycastNonAlloc(source, dir, HitCache, attackRange, IHitbox.HitLayerMaskComputation, QueryTriggerInteraction.Collide);
                IEnumerable<RaycastHit> hits = Enumerable.Range(0, hitCount).Select(idx => HitCache[idx]);
                IEnumerable<(RaycastHit, IHitbox)> filteredHits = null;
                if (pierce)
                {
                    filteredHits = IHitbox.GetAllValidHit(hits, Source);
                }
                else
                {
                    var hitbox = IHitbox.GetFirstValidHit(hits, Source, out RaycastHit firstHit, out bool didHit);
                    if (!didHit || hitbox == null)
                    {
                        filteredHits = Enumerable.Empty<(RaycastHit, IHitbox)>();
                    }
                    else
                    {
                        filteredHits = Enumerable.Repeat((firstHit, hitbox), 1);
                    }
                }

                foreach ((RaycastHit raycastHit, IHitbox hitbox) in filteredHits)
                {
                    IDamageable damageable = hitbox.Source;
                    if (!hitLookup.TryGetValue(damageable, out (RaycastHit, IHitbox) tuple) || raycastHit.distance < tuple.Item1.distance)
                    {
                        hitLookup[damageable] = (raycastHit, hitbox);
                    }
                }
            }

            // Return list of targets struck
            int currentTarget = 0;
            foreach (KeyValuePair<IDamageable, (RaycastHit, IHitbox)> kvp in hitLookup.OrderBy(kvp => kvp.Value.Item1.distance))
            {
                RaycastHit raycastHit = kvp.Value.Item1;
                DamageEvent attack = IHitbox.DamageEventFromHit(raycastHit, kvp.Value.Item2, damage, raycastHit.normal, damageType);
                attack.damageSource = (Source as Component).GetComponent<IDamageSource>();
                yield return attack;
                currentTarget++;
                if (currentTarget >= maxTargets)
                {
                    yield break;
                }
            }
        }

        public override void PerformAction()
        {
            Actor.RaiseEvent(new MeleeAttackEvent(attackType, cooldown));
            Vector3 source = PlayerPosition.position + AttackBaseOffset;
            var rotation = Quaternion.Euler(viewHeading.Pitch, viewHeading.Yaw, 0);
            IEnumerable<DamageEvent> attack = null;
            switch (attackType)
            {
                case MeleeAttackType.Stab:
                    attack = GetTargets(rotation, source, true, int.MaxValue);
                    break;
                case MeleeAttackType.Cleave:
                    attack = GetTargets(rotation, source, false, int.MaxValue);
                    break;
                case MeleeAttackType.Basic:
                default:
                    attack = GetTargets(rotation, source, false, 1);
                    break;
            }

            DamageActor.MultiAttackServerRpc(attack.Select(attack => NetworkDamageEvent.FromDamageEvent(attack)).ToArray());
        }
    }
}
