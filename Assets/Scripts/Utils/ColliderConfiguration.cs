
using System;
using UnityEngine;

namespace nickmaltbie.Treachery.Utils
{
    public enum CapsuleDirection
    {
        X = 0,
        Y = 1,
        Z = 2,
    }

    public enum ColliderType
    {
        Box,
        Sphere,
        Capsule,
    }

    [Serializable]
    public struct ColliderConfiguration
    {
        public ColliderType type;
        public Vector3 center;
        public Vector3 size;
        public float radius;
        public float height;
        public CapsuleDirection capsuleDirection;

        public ColliderConfiguration(
            ColliderType type = ColliderType.Box,
            Vector3? center = null,
            Vector3? size = null,
            float radius = 0.5f,
            float height = 2.0f,
            CapsuleDirection capsuleDirection = CapsuleDirection.Y)
        {
            this.type = type;
            this.center = center ?? Vector3.zero;
            this.size = size ?? Vector3.one;
            this.radius = radius;
            this.height = height;
            this.capsuleDirection = capsuleDirection;
        }

        public Collider AttachCollider(GameObject go, bool cleanupCollider = true, bool destroyImmediate = false)
        {
            if (cleanupCollider)
            {
                if (destroyImmediate)
                {
                    foreach (Collider col in go.GetComponents<Collider>())
                    {
                        GameObject.DestroyImmediate(col);
                    }
                }
                else
                {
                    GameObject.Destroy(go.GetComponent<Collider>());
                }
            }

            switch (type)
            {
                case ColliderType.Box:
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    box.size = size;
                    box.center = center;
                    return box;
                case ColliderType.Sphere:
                    SphereCollider sphere = go.AddComponent<SphereCollider>();
                    sphere.center = center;
                    sphere.radius = radius;
                    return sphere;
                case ColliderType.Capsule:
                    CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
                    capsule.center = center;
                    capsule.radius = radius;
                    capsule.height = height;
                    capsule.direction = (int) capsuleDirection;
                    return capsule;
            }

            return null;
        }
    }
}