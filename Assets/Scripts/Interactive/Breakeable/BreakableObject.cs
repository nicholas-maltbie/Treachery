using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Breakeable
{
    public enum BreakableObjectState
    {
        Unitialized,
        Fixed,
        Broken,
        CleanedUp
    }

    [RequireComponent(typeof(Damageable))]
    public class BreakableObject : NetworkSMBehaviour
    {
        public BreakableObjectState defaultState = BreakableObjectState.Fixed;

        private NetworkVariable<BreakableObjectState> borkenState;

        public GameObject fixedPrefab;
        public GameObject brokenPrefab;
        public float despawnTime;

        private GameObject fixedSpawn;
        private GameObject brokenSpawn;
        private float brokenElapsed;

        public override void Start()
        {
            borkenState = new NetworkVariable<BreakableObjectState>(value: defaultState);
            base.Start();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                borkenState.Value = defaultState;
                Damageable damageable = GetComponent<Damageable>();
                damageable.HealHealth(damageable.GetMaxHealth(), EmptyDamageSource.Instance);
            }

            UpdatePrefabState();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            borkenState.Value = defaultState;
            UpdatePrefabState();
        }

        public override void Update()
        {
            base.Update();
            
            if (IsServer)
            {
                UpdateObjectState();
            }

            UpdatePrefabState();
        }

        public void UpdateObjectState()
        {
            switch (borkenState.Value)
            {
                case BreakableObjectState.Broken:
                    brokenElapsed += Time.deltaTime;
                    if (brokenElapsed >= despawnTime)
                    {
                        borkenState.Value = BreakableObjectState.CleanedUp;
                    }
                    break;
                case BreakableObjectState.Fixed:
                    if (!GetComponent<Damageable>().IsAlive())
                    {
                        brokenElapsed = 0.0f;
                        borkenState.Value = BreakableObjectState.Broken;
                    }
                    break;
                case BreakableObjectState.CleanedUp:
                    if (GetComponent<Damageable>().IsAlive())
                    {
                        borkenState.Value = BreakableObjectState.Fixed;
                    }
                    break;
            }
        }

        public void UpdatePrefabState()
        {
            if (borkenState.Value == BreakableObjectState.Fixed)
            {
                fixedSpawn ??= GameObject.Instantiate(fixedPrefab, transform.position, transform.rotation, transform);
            }
            else if (fixedSpawn != null)
            {
                GameObject.Destroy(fixedSpawn);
                fixedSpawn = null;
            }

            if (borkenState.Value == BreakableObjectState.Broken)
            {
                brokenSpawn ??= GameObject.Instantiate(brokenPrefab, transform.position, transform.rotation, transform);
            }
            else if (brokenSpawn != null)
            {
                GameObject.Destroy(brokenSpawn);
                brokenSpawn = null;
            }
        }
    }
}