using Enemies;
using EnemyIK.IKUpdater;
using GameData;
using HarmonyLib;
using RootMotion;
using RootMotion.FinalIK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RootMotion.FinalIK.Grounding;

namespace EnemyIK
{
    [HarmonyPatch(typeof(EnemyPrefabManager), nameof(EnemyPrefabManager.BuildEnemyPrefab))]
    internal static class Inject_EnemyPrefab
    {
        private static void Postfix(EnemyDataBlock data, GameObject __result)
        {
            Logger.Error($"Build EnemyPrefab! : {data.name}");
            var agent = __result.GetComponent<EnemyAgent>();
            var anim = agent.Anim;

            if (anim == null)
            {
                Logger.Error("Anim Was Null!");
                return;
            }

            if (!anim.isHuman)
            {
                Logger.Error("Anim Was Not Human Rig!");
                return;
            }

            var pelvisBone = anim.GetBoneTransform(HumanBodyBones.Hips);
            var headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            if (pelvisBone == null)
            {
                Logger.Error("Hip Bone is missing!");
                return;
            }

            var root = anim.gameObject;

            //Setup Body IK
            
            var leftLegIK = AddLimbIK(root,
                anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                anim.GetBoneTransform(HumanBodyBones.LeftFoot));
            leftLegIK.solver.goal = AvatarIKGoal.LeftFoot;

            var rightLegIK = AddLimbIK(root,
                anim.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                anim.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                anim.GetBoneTransform(HumanBodyBones.RightFoot));
            rightLegIK.solver.goal = AvatarIKGoal.RightFoot;

            var leftArmIK = AddLimbIK(root,
                anim.GetBoneTransform(HumanBodyBones.LeftUpperArm),
                anim.GetBoneTransform(HumanBodyBones.LeftLowerArm),
                anim.GetBoneTransform(HumanBodyBones.LeftHand));
            leftArmIK.solver.goal = AvatarIKGoal.LeftHand;
            leftArmIK.solver.bendModifier = IKSolverLimb.BendModifier.Arm;

            var rightArmIK = AddLimbIK(root,
                anim.GetBoneTransform(HumanBodyBones.RightUpperArm),
                anim.GetBoneTransform(HumanBodyBones.RightLowerArm),
                anim.GetBoneTransform(HumanBodyBones.RightHand));
            rightArmIK.solver.goal = AvatarIKGoal.RightHand;
            rightArmIK.solver.bendModifier = IKSolverLimb.BendModifier.Arm;


            var grounder = root.AddComponent<GrounderIK>();
            grounder.pelvis = pelvisBone;
            grounder.solver.quality = Quality.Simple;
            grounder.solver.maxStep = 0.45f;
            grounder.solver.rootSphereCastRadius = 0.3f;
            grounder.solver.footRadius = 0.25f;
            grounder.solver.pelvisDamper = 0.3f;
            grounder.characterRoot = root.transform;
            grounder.solver.layers = new LayerMask
            {
                value = LayerManager.MASK_LEVELGEN
            };

            var bodyUpdater = root.AddComponent<BodyIKUpdater>();
            bodyUpdater.Agent.Set(agent);
            bodyUpdater.Animator.Set(anim);
            bodyUpdater.LeftLegIK.Set(leftLegIK);
            bodyUpdater.RightLegIK.Set(rightLegIK);
            bodyUpdater.LeftArmIK.Set(leftArmIK);
            bodyUpdater.RightArmIK.Set(rightArmIK);
            bodyUpdater.GrounderIK.Set(grounder);

            //Setup LookAt IK
            var headCollider = headBone.gameObject.GetComponent<SphereCollider>();
            if (false)
            {
                var ccdIK = root.AddComponent<CCDIK>();
                ccdIK.solver.SetChain(new Transform[]
                {
                anim.GetBoneTransform(HumanBodyBones.Spine),
                anim.GetBoneTransform(HumanBodyBones.Chest),
                anim.GetBoneTransform(HumanBodyBones.Head)
                },
                root.transform);

                var headProxyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                headProxyObj.name = $"EnemyIK_HeadProxy";
                headProxyObj.transform.SetParent(headBone);
                headProxyObj.layer = LayerManager.LAYER_DEBRIS;

                var headProxy = headProxyObj.AddComponent<Rigidbody>();
                headProxy.useGravity = false;

                var headCollision = headProxyObj.AddComponent<CollisionChecker>();

                var lookAtUpdater = root.AddComponent<LookAtIKUpdater>();
                lookAtUpdater.Agent.Set(agent);
                lookAtUpdater.Animator.Set(anim);
                lookAtUpdater.CCDIK.Set(ccdIK);
                lookAtUpdater.SimRigidBody.Set(headProxy);
                lookAtUpdater.HeadCollider.Set(headCollider);
                lookAtUpdater.HeadCollision.Set(headCollision);
            }
            else
            {
                Logger.Error("Head Bone is missing! Skip Head Collision checks");
            }
        }

        private static LimbIK AddLimbIK(GameObject root, Transform up, Transform low, Transform end)
        {
            var limb = root.AddComponent<LimbIK>();
            limb.solver.SetChain(up, low, end, root.transform);
            limb.solver.bendModifier = IKSolverLimb.BendModifier.Animation;
            return limb;
        }
    }
}
