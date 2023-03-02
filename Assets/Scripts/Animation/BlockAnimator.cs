using System.Collections;
using System.Collections.Generic;
using nickmaltbie.Treachery.Utils;
using UnityEngine;

namespace nickmaltbie.Treachery.Animation
{
    public class BlockAnimator : MonoBehaviour
    {
        protected Animator animator;
        public bool enableBlock;
        public float forwardOffset = 0.25f;
        public float spacing = 0.15f;
        public float verticalOffset = 0.3f;
        public float elbowSpacing = 0.15f;
        public float elbowVerticalOffset = 0.3f;
        public float transitionTime = 0.25f;

        private bool previousEnableBlock;
        private float elapsed;
        private float currentWeight;
        private float startWeight;
        private float targetWeight;

        public void Start()
        {
            animator = GetComponent<Animator>();

            startWeight = enableBlock ? 1 : 0;
            targetWeight = enableBlock ? 1 : 0;
            elapsed = 0;
            previousEnableBlock = enableBlock;
        }

        public void OnAnimatorIK()
        {
            if (previousEnableBlock != enableBlock)
            {
                startWeight = currentWeight;
                elapsed = 0.0f;
                targetWeight = enableBlock ? 1 : 0;
            }

            elapsed += Time.deltaTime;
            float progress = elapsed / transitionTime;
            float smoothed = MathUtils.SmoothValue(progress);
            float range = targetWeight - startWeight;
            currentWeight = startWeight + range * smoothed;

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, currentWeight);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, currentWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, currentWeight);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, currentWeight);
            animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, currentWeight);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, currentWeight);

            if (currentWeight > 0.0f)
            {
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

                // Get the position of the hands to be slightly in front of the
                // character and spaced a little to the left or the right
                Vector3 rightHandGoal = head.position + head.up * verticalOffset + head.right * spacing + head.forward * forwardOffset;
                Vector3 leftHandGoal = head.position + head.up * verticalOffset - head.right * spacing + head.forward * forwardOffset;
                Vector3 rightElbowGoal = head.position - head.up * elbowVerticalOffset + head.right * elbowSpacing + head.forward * forwardOffset;
                Vector3 leftElbowGoal = head.position - head.up * elbowVerticalOffset - head.right * elbowSpacing + head.forward * forwardOffset;

                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGoal);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandGoal);
                animator.SetIKHintPosition(AvatarIKHint.LeftElbow, leftElbowGoal);
                animator.SetIKHintPosition(AvatarIKHint.RightElbow, rightElbowGoal);

                // Have the hands be rotated back towards the player
                Quaternion handRotation = Quaternion.LookRotation(transform.up, transform.forward);
                animator.SetIKRotation(AvatarIKGoal.RightHand, handRotation);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, handRotation);
            }

            previousEnableBlock = enableBlock;
        }
    }
}
