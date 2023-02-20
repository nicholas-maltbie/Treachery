
using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Breakeable
{
    [RequireComponent(typeof(Damageable))]
    public class BreakableObject : NetworkSMBehaviour, IDamageListener
    {
        NetworkVariable<bool> isFixed = new NetworkVariable<bool>(value: true);

        public GameObject fixedPrefab;
        public GameObject brokenPrefab;
        public float despawnTime;

        private GameObject fixedSpawn;
        private GameObject brokenSpawn;
        private float brokenElapsed;

        public override void Update()
        {
            base.Update();
            
            if (IsServer && !isFixed.Value)
            {
                brokenElapsed += Time.deltaTime;
                if (brokenElapsed >= despawnTime)
                {
                    GetComponent<NetworkObject>().Despawn(true);
                }
            }

            if (isFixed.Value)
            {
                fixedSpawn ??= GameObject.Instantiate(fixedPrefab, transform.position, transform.rotation, transform);
            }
            else if (!isFixed.Value && brokenSpawn == null)
            {
                GameObject.Destroy(fixedSpawn);
                brokenSpawn ??= GameObject.Instantiate(brokenPrefab, transform.position, transform.rotation, transform);
            }
        }

        public void OnDamage(IDamageable target, IDamageSource source, float previous, float current, float damage)
        {
            if (isFixed.Value && !GetComponent<Damageable>().IsAlive())
            {
                isFixed.Value = false;
            }
        }

        public void OnHeal(IDamageable target, IDamageSource source, float previous, float current, float amount)
        {
            
        }
    }
}