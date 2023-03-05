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
using nickmaltbie.Treachery.Interactive;
using UnityEngine;

namespace nickmaltbie.Treachery.Animation
{
    [RequireComponent(typeof(Animator))]
    public class ArrowAimHelper : MonoBehaviour
    {
        public Arrow arrowPrefab;

        public DominantHand dominantHand = DominantHand.RightHanded;
        public Transform targetPosition;
        public Transform leftHandTransform;
        public Transform rightHandTransform;
        public bool Tracking { get; set; } = true;
        public bool ShowArrow { get; set; } = true;
        public bool DrawingArrow { get; set; } = true;

        protected Arrow spawnedArrow;
        protected Animator animator;

        public AvatarIKGoal ArrowDrawHand => dominantHand == DominantHand.RightHanded ?
            AvatarIKGoal.RightHand : AvatarIKGoal.LeftHand;
        public AvatarIKGoal BowHoldHand => dominantHand == DominantHand.RightHanded ?
            AvatarIKGoal.LeftHand : AvatarIKGoal.RightHand;
        public HumanBodyBones ArrowDrawHandBone => dominantHand == DominantHand.RightHanded ?
            HumanBodyBones.RightHand : HumanBodyBones.LeftHand;
        public HumanBodyBones BowHoldHandBone => dominantHand == DominantHand.RightHanded ?
            HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        public HumanBodyBones BowHoldShoulderBone => dominantHand == DominantHand.RightHanded ?
            HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder;
        public Vector3 AimNormal => dominantHand == DominantHand.RightHanded ? Vector3.left : Vector3.right;

        public Transform ArrowTransform => spawnedArrow.transform;

        public void Start()
        {
            animator = GetComponent<Animator>();
            spawnedArrow = GameObject.Instantiate(arrowPrefab.gameObject, transform.position, transform.rotation).GetComponent<Arrow>();
        }

        public void OnAnimatorIK()
        {
            if (!animator)
            {
                return;
            }

            if (Tracking && targetPosition != null)
            {
                Transform drawHandTransform = animator.GetBoneTransform(ArrowDrawHandBone);
                Transform bowHandTransform = animator.GetBoneTransform(BowHoldHandBone);
                var targetBowRotation = Quaternion.LookRotation((targetPosition.position - bowHandTransform.position).normalized, -AimNormal);

                Transform shoulderTransform = animator.GetBoneTransform(BowHoldShoulderBone);
                float armLength = Mathf.Min((bowHandTransform.position - shoulderTransform.position).magnitude, 1.5f);
                Vector3 bowArmAimDir = targetPosition.position - shoulderTransform.position;

                animator.SetIKPositionWeight(BowHoldHand, 1.0f);
                animator.SetIKRotationWeight(BowHoldHand, 1.0f);
                animator.SetIKPosition(BowHoldHand, shoulderTransform.position + bowArmAimDir * armLength + AimNormal * 0.1f);
                animator.SetIKRotation(BowHoldHand, targetBowRotation);

                Quaternion arrowRotation = drawHandTransform.rotation;
                if (DrawingArrow)
                {
                    arrowRotation = Quaternion.LookRotation((targetPosition.position - drawHandTransform.position).normalized);
                }

                spawnedArrow.transform.rotation = arrowRotation;
                Vector3 holdOffset = spawnedArrow.transform.localToWorldMatrix * arrowPrefab.holdPosition.localPosition;
                spawnedArrow.transform.position = drawHandTransform.position - holdOffset;
            }
            else
            {
                animator.SetIKPositionWeight(BowHoldHand, 0.0f);
                animator.SetIKRotationWeight(BowHoldHand, 0.0f);
            }

            spawnedArrow.gameObject.SetActive(ShowArrow);
        }
    }
}
