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
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace nickmaltbie.Treachery.Enemy.Zombie
{
    public class ZombieManager : NetworkBehaviour
    {
        public static int TotalSpawned = 0;

        [SerializeField]
        public static float zombieCleanupTime = 3.0f;

        [SerializeField]
        public ZombieEnemy[] zombiePrefabs;

        [SerializeField]
        public Transform[] spawnPositions;

        public int targetZombieCount = 10;

        private LinkedList<(GameObject, float)> deathTimes = new LinkedList<(GameObject, float)>();

        public int SpawnedZombies { get; private set; } = 0;

        /// <summary>
        /// Get a zombie config
        /// </summary>
        /// <param name="name"></param>
        /// <param name="strength">Zombie strength should vary in range [0, 10]</param>
        /// <returns></returns>
        public ZombieConfig GetZombieConfig(string name, float strength)
        {
            strength = Mathf.Clamp(strength, 0, 10);
            float normalizedStrength = strength / 10.0f;

            float health = 20 + 10 * strength;
            float scale = 1.0f + normalizedStrength + UnityEngine.Random.Range(-0.1f, 0.1f);
            float attackCooldown = 2.0f - normalizedStrength * 1.75f;
            float attackDamage = 5.0f + normalizedStrength * 20.0f + UnityEngine.Random.Range(-3.0f, 3.0f);
            GameObject zombiePrefab = zombiePrefabs[UnityEngine.Random.Range(0, zombiePrefabs.Length)].gameObject;
            Transform selectedTransform = spawnPositions[UnityEngine.Random.Range(0, spawnPositions.Length)];

            Vector3 offset = UnityEngine.Random.insideUnitCircle * 2;
            Vector3 spawnPos = selectedTransform.transform.position + new Vector3(offset.x, 0, offset.y);

            return new ZombieConfig
            {
                name = name,
                health = health,
                scale = scale,
                attackCooldown = attackCooldown,
                attackDamage = attackDamage,
                zombiePrefab = zombiePrefab,
                spawnPos = spawnPos,
            };
        }

        public GameObject SpawnZombie(ZombieConfig config)
        {
            GameObject spawned = GameObject.Instantiate(config.zombiePrefab, config.spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0));
            spawned.transform.localScale = Vector3.one * config.scale;

            NetworkObject netObj = spawned.GetComponent<NetworkObject>();
            netObj.Spawn(true);

            ZombieEnemy zombie = spawned.GetComponent<ZombieEnemy>();
            zombie.attackDamage = config.attackDamage;
            zombie.attackCooldown = config.attackCooldown;
            zombie.attackRange = 1.5f * config.scale;

            var agent = spawned.GetComponent<NavMeshAgent>();

            Nametag nametag = spawned.GetComponent<Nametag>();
            nametag.UpdateName(config.name);

            Damageable damageable = spawned.GetComponent<Damageable>();
            damageable.AdjustMaxHealth(config.health);
            damageable.OnDeath += OnZombieDie;

            SpawnedZombies++;

            return spawned;
        }

        public void SpawnZombie(string name, float strength)
        {
            ZombieConfig config = GetZombieConfig(name, strength);
            SpawnZombie(config);
        }

        public void OnZombieDie(object source, EventArgs args)
        {
            if (source is Damageable damaged)
            {
                deathTimes.AddFirst(((damaged.gameObject, Time.time)));
                damaged.OnDeath -= OnZombieDie;
            }
        }

        public void Update()
        {
            if (!IsServer)
            {
                return;
            }

            while (deathTimes.Count > 0  && deathTimes.Last.Value.Item2 + zombieCleanupTime <= Time.time)
            {
                GameObject go = deathTimes.Last.Value.Item1;
                deathTimes.RemoveLast();
                go.GetComponent<NetworkObject>().Despawn(true);
                SpawnedZombies--;
            }

            while (SpawnedZombies < targetZombieCount)
            {
                SpawnZombie($"Zombie-{TotalSpawned++}", UnityEngine.Random.Range(0, 2.0f));
            }
        }
    }
}
