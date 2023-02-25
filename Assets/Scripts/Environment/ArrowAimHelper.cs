

using nickmaltbie.Treachery.Interactive;
using UnityEngine;

namespace nickmaltbie.Treachery.Environment
{
    public enum DominantHand
    {
        RightHanded,
        LeftHanded,
    }

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
                Quaternion targetBowRotation = Quaternion.LookRotation((targetPosition.position - bowHandTransform.position).normalized, AimNormal);

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