using BepInEx.IL2CPP.Utils;
using Enemies;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.InteropTypes.Fields;
using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EnemyIK.IKUpdater
{
    internal enum BodyIKMode
    {
        Legs,
        Arms,
        Both
    }

    internal sealed class BodyIKUpdater : MonoBehaviour
    {
        public readonly int GROUNDLAYER = LayerManager.MASK_CROUCH_TO_STAND_COLLIDERS;

        public Il2CppReferenceField<EnemyAgent> Agent;
        public Il2CppReferenceField<GrounderIK> GrounderIK;
        public Il2CppReferenceField<Animator> Animator;

        public Il2CppReferenceField<LimbIK> LeftLegIK;
        public Il2CppReferenceField<LimbIK> RightLegIK;
        public Il2CppReferenceField<LimbIK> LeftArmIK;
        public Il2CppReferenceField<LimbIK> RightArmIK;

        private EnemyAgent _Agent;
        private GrounderIK _Grounder;
        private Animator _Anim;
        private BodyIKMode _IKMode = BodyIKMode.Legs;
        private bool _IsCrawler = false;

        void Start()
        {
            _Agent = Agent.Get();
            _Grounder = GrounderIK.Get();
            _Anim = Animator.Get();

            switch (_Agent.Locomotion.AnimHandleName)
            {
                case EnemyLocomotion.AnimatorControllerHandleName.EnemyCrawl:
                case EnemyLocomotion.AnimatorControllerHandleName.EnemyPouncer:
                    _IsCrawler = true;
                    _IKMode = BodyIKMode.Both;
                    break;

                case EnemyLocomotion.AnimatorControllerHandleName.EnemyCrawlFlip:
                case EnemyLocomotion.AnimatorControllerHandleName.EnemyBirtherCrawlFlip:
                    _IsCrawler = true;
                    _IKMode = BodyIKMode.Arms;
                    break;

                default:
                    _IsCrawler = false;
                    _IKMode = BodyIKMode.Legs;
                    break;
            }

            if (_IsCrawler)
            {
                _Grounder.solver.rotateSolver = true;
            }

            switch (_IKMode)
            {
                case BodyIKMode.Legs:
                    _Grounder.legs = new(new IK[] { LeftLegIK, RightLegIK });
                    SetArmIKEnabled(false);
                    SetLegIKEnabled(true);
                    break;
                case BodyIKMode.Arms:
                    _Grounder.legs = new(new IK[] { LeftArmIK, RightArmIK });
                    SetArmIKEnabled(true);
                    SetLegIKEnabled(false);
                    break;
                case BodyIKMode.Both:
                    _Grounder.legs = new(new IK[] { LeftLegIK, RightLegIK, LeftArmIK, RightArmIK });
                    SetArmIKEnabled(true);
                    SetLegIKEnabled(true);
                    break;
            }
        }

        void FixedUpdate()
        {
            if (!_Agent.Alive)
            {
                SetGroundIKEnabled(false);
                SetArmIKEnabled(false);
                SetLegIKEnabled(false);
                return;
            }

            if (_IsCrawler)
            {
                UpdateSurface();
            }
        }

        [HideFromIl2Cpp]
        void UpdateSurface()
        {
            var tr = _Agent.MainModelTransform;
            var rayPos = _Agent.transform.position + new Vector3(0.0f, 0.5f, 0.0f);
            var rayRot = Vector3.down;
            if (Physics.Raycast(rayPos, rayRot, out var hit, 30.0f, GROUNDLAYER, QueryTriggerInteraction.Ignore))
            {
                var normal = ExtractNormal(_Agent.transform.forward, hit);
                /*
                var angle = Vector3.Angle(normal, Vector3.up);
                if (angle >= 60.0f)
                {
                    return;
                }
                */

                var newRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(_Agent.transform.forward, normal), normal);
                tr.rotation = Quaternion.RotateTowards(tr.rotation, newRotation, Time.fixedDeltaTime * 120.0f);
                //_Grounder.solver.heightOffset = hit.point.y - tr.position.y;
            }
        }

        [HideFromIl2Cpp]
        Vector3 ExtractNormal(Vector3 forward, RaycastHit reference)
        {
            forward *= 0.75f;

            var normal = reference.normal;
            var newRayPoint = reference.point + Vector3.up;

            TryRayCast(newRayPoint + forward, ref normal);
            TryRayCast(newRayPoint - forward, ref normal);

            return normal.normalized;
        }

        [HideFromIl2Cpp]
        void TryRayCast(Vector3 position, ref Vector3 normal)
        {
            if (Physics.Raycast(position, Vector3.down, out var cast, 10.0f, GROUNDLAYER))
            {
                /*
                var angle = Vector3.Angle(normal, Vector3.up);
                if (angle >= 60.0f)
                {
                    return;
                }
                */

                normal += cast.normal;
            }
        }

        [HideFromIl2Cpp]
        void SetGroundIKEnabled(bool enabled)
        {
            GrounderIK.Value.enabled = enabled;
        }

        [HideFromIl2Cpp]
        void SetArmIKEnabled(bool enabled)
        {
            LeftArmIK.Value.enabled = enabled;
            RightArmIK.Value.enabled = enabled;
        }

        [HideFromIl2Cpp]
        void SetLegIKEnabled(bool enabled)
        {
            LeftLegIK.Value.enabled = enabled;
            RightLegIK.Value.enabled = enabled;
        }
    }
}
