using Enemies;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EnemyIK.IKUpdater
{
    internal sealed class LookAtIKUpdater : MonoBehaviour
    {
        public Il2CppReferenceField<EnemyAgent> Agent;
        public Il2CppReferenceField<Animator> Animator;

        public Il2CppReferenceField<CCDIK> CCDIK;
        public Il2CppReferenceField<Rigidbody> SimRigidBody;
        public Il2CppReferenceField<CollisionChecker> HeadCollision;
        public Il2CppReferenceField<SphereCollider> HeadCollider;

        private EnemyAgent _Agent;
        private Animator _Anim;
        private Rigidbody _HeadProxyRig;
        private Transform _HeadTransform;
        private SphereCollider _HeadCollider;
        private float _IKBuildup = 0.0f;
        private float _IKBuildupSpeed = 5.0f;
        private float _IKCooldownSpeed = 5.0f;

        void Start()
        {
            _Agent = Agent.Get();
            _Anim = Animator.Get();

            _HeadCollider = HeadCollider.Get();
            _HeadTransform = _HeadCollider.transform;
            _HeadProxyRig = SimRigidBody.Get();
            _HeadProxyRig.transform.parent = null;
            _HeadProxyRig.transform.position = _HeadTransform.position;

            var lossyScale = _HeadTransform.lossyScale * 1.5f;
            var sizeMulti = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
            var worldRadius = HeadCollider.Value.radius * sizeMulti;
            var proxyScale = new Vector3(worldRadius, worldRadius, worldRadius);
            _HeadProxyRig.transform.localScale = proxyScale;

            CCDIK.Value.solver.target = _HeadProxyRig.transform;
            CCDIK.Value.solver.SetIKPositionWeight(0.0f);
            SetIKEnabled(true);
        }

        void Update()
        {
            if (HeadCollision.Value.IsColliding)
            {
                _IKBuildup += Time.deltaTime * _IKBuildupSpeed;
            }
            else
            {
                _IKBuildup -= Time.deltaTime * _IKCooldownSpeed;
            }

            _IKBuildup = Mathf.Clamp01(_IKBuildup);
            CCDIK.Value.solver.SetIKPositionWeight(_IKBuildup);
        }

        private Was _Status = Was.Initial;
        private Vector3 _LastSafePositionBuffer;
        private Vector3 _LastSafePosition;
        void OnAnimatorMove()
        {
            if (!_Agent.Alive)
                return;

            if (HeadCollision.Value.IsColliding)
            {
                if (_Status == Was.NotColliding)
                {
                    _HeadProxyRig.position = _LastSafePosition;
                }

                var headPos = _HeadTransform.TransformPoint(_HeadCollider.center);
                var delta = headPos - _HeadProxyRig.transform.position;

                if (delta.magnitude > 1.5f)
                {
                    Logger.Error("Reset!");
                    _HeadProxyRig.transform.position = GetHeadColliderPosition();
                    _LastSafePosition = GetHeadColliderPosition();
                }

                _HeadProxyRig.velocity = delta.normalized;
                _Status = Was.Colliding;
            }
            else
            {
                if (_Status == Was.Colliding)
                {
                    _HeadProxyRig.position = _LastSafePosition;
                }

                _LastSafePosition = _LastSafePositionBuffer;
                _LastSafePositionBuffer = _HeadProxyRig.position = GetHeadColliderPosition();
                _HeadProxyRig.velocity = _HeadTransform.forward * 0.01f;
                _Status = Was.NotColliding;
            }
        }

        Vector3 GetHeadColliderPosition()
        {
            return _HeadTransform.TransformPoint(_HeadCollider.center);
        }

        [HideFromIl2Cpp]
        void SetIKEnabled(bool enabled)
        {
            CCDIK.Get().enabled = enabled;
        }

        internal enum Was : byte
        {
            Initial,
            Colliding,
            NotColliding
        }
    }
}
