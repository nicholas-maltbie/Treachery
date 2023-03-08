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
