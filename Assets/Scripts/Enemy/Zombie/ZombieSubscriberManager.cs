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
using nickmaltbie.Treachery.Enemy.Subs;
using UnityEngine;

namespace nickmaltbie.Treachery.Enemy.Zombie
{
    [RequireComponent(typeof(SubscriberReader))]
    public class ZombieSubscriberManager : ZombieManager
    {
        public float SpawnCooldown = 3.0f;

        public int minZombiePackSize = 5;
        public int maxZombiePackSize = 10;

        private LinkedList<(Subscriber, float)> drawPool = new LinkedList<(Subscriber, float)>();
        private float totalWeight = 0.0f;

        private float timeToNextGroupSpawn;

        public void Awake()
        {
            Subscribers subs = GetComponent<SubscriberReader>().ParseSubs();
            foreach (Subscriber sub in subs.subs)
            {
                float weight = Mathf.Min(sub.months, 1.0f);
                totalWeight += weight;
                drawPool.AddLast((sub, weight));
            }

            timeToNextGroupSpawn = SpawnCooldown;
        }

        public override void Update()
        {
            timeToNextGroupSpawn -= Time.deltaTime;
            base.Update();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            while (SpawnedZombies <= targetZombieCount && drawPool.Count > 0)
            {
                SpawnZombieGroup();
            }
        }

        public Subscriber DrawSubscriber()
        {
            if (drawPool.Count == 0)
            {
                return null;
            }

            float selectedWeight = UnityEngine.Random.Range(0, totalWeight);
            float enumeratedWeight = 0.0f;
            LinkedListNode<(Subscriber, float)> current = drawPool.First;

            while (enumeratedWeight < selectedWeight && current.Next != null)
            {
                enumeratedWeight += current.Value.Item2;
                current = current.Next;
            }

            totalWeight -= current.Value.Item2;
            drawPool.Remove(current);

            return current.Value.Item1;
        }

        private void SpawnZombieGroup()
        {
            int groupSize = Mathf.Min(drawPool.Count, UnityEngine.Random.Range(minZombiePackSize, maxZombiePackSize + 1));
            ZombieSpawnPos groupSpawn = spawnPositions[UnityEngine.Random.Range(0, spawnPositions.Length)];

            for (int i = 0; i < groupSize; i++)
            {
                Subscriber sub = DrawSubscriber();
                ZombieConfig config = GetZombieConfig(sub.name, sub.strength);

                Vector3 spawnPos = groupSpawn.GetSpawnPos();
                config.spawnPos = spawnPos;

                SpawnZombie(config);
            }
        }

        public override void SpawnZombiesIfNeeded()
        {
            if (timeToNextGroupSpawn <= 0)
            {
                timeToNextGroupSpawn = SpawnCooldown;
                while (SpawnedZombies < targetZombieCount && drawPool.Count > 0)
                {
                    SpawnZombieGroup();
                }
            }
        }
    }
}
