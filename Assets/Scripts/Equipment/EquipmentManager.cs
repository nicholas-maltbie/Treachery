
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    [RequireComponent(typeof(Animator))]
    public class EquipmentManager : MonoBehaviour
    {
        public DominantHand dominantHand;

        private Animator _animator;
        public Animator Animator => _animator ??= GetComponent<Animator>();

        public HumanBodyBones MainHandBone =>
            dominantHand == DominantHand.RightHanded ?
                HumanBodyBones.RightHand :
                HumanBodyBones.LeftHand;

        public HumanBodyBones OffHandBone =>
            dominantHand == DominantHand.RightHanded ?
                HumanBodyBones.LeftHand :
                HumanBodyBones.RightHand;

        public Transform GetMainHand => Animator.GetBoneTransform(MainHandBone);
        public Transform GetOffHand => Animator.GetBoneTransform(OffHandBone);
    }
}