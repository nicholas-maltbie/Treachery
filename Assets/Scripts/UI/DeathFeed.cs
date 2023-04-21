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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace nickmaltbie.Treachery.UI
{
    public struct FeedEntry
    {
        public GameObject feedItem;
        public float timeCreation;
        public float offset;
    }

    public class DeathFeed : MonoBehaviour
    {
        public static DeathFeed Singleton;

        public DeathEvent eventPrefab;

        public float scrollSpeed = 80.0f;

        public float elementLifetime = 5.0f;

        private LinkedList<FeedEntry> eventFeed = new LinkedList<FeedEntry>();
        private GameObject feedBase;
        private float currentOffset = 0;

        public void ResetFeed()
        {
            eventFeed.Clear();

            if (feedBase != null)
            {
                GameObject.Destroy(feedBase);
                feedBase = null;
            }

            currentOffset = 0;

            feedBase = new GameObject();
            feedBase.transform.SetParent(transform);
            var rectTransform = feedBase.AddComponent<RectTransform>();
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            feedBase.name = "Feed Base";
            rectTransform.localScale = Vector3.one;
        }

        public void AddEvent(string source, string dest)
        {
            eventFeed.AddFirst(CreateFeedEvent(source, dest));
        }

        private FeedEntry CreateFeedEvent(string source, string dest)
        {
            var item = GameObject.Instantiate(eventPrefab.gameObject);
            item.transform.SetParent(feedBase.transform);
            currentOffset += item.GetComponent<RectTransform>().sizeDelta.y;
            var rectTransform = item.GetComponent<RectTransform>();

            rectTransform.localPosition = new Vector3(0, currentOffset, 0);
            rectTransform.localScale = Vector3.one;

            item.GetComponent<DeathEvent>().UpdateText(source, dest);

            return new FeedEntry()
            {
                feedItem = item,
                timeCreation = Time.time,
                offset = currentOffset,
            };
        }

        public void Awake()
        {
            if (Singleton != null)
            {
                GameObject.Destroy(gameObject);
                return;
            }

            Singleton = this;
            ResetFeed();
        }

        public void OnDestroy()
        {
            if (Singleton == this)
            {
                Singleton = null;
            }
        }

        public void Update()
        {
            var rectTransform = feedBase.GetComponent<RectTransform>();
            if (rectTransform.localPosition.y > -currentOffset)
            {
                rectTransform.localPosition -= Vector3.up * Time.deltaTime * scrollSpeed;
            }

            while (eventFeed.Count > 0 && eventFeed.Last.Value.timeCreation + elementLifetime <= Time.time)
            {
                var last = eventFeed.Last;
                eventFeed.RemoveLast();

                if (eventFeed.Count == 0)
                {
                    ResetFeed();
                }

                GameObject.Destroy(last.Value.feedItem);
            }
        }
    }
}
