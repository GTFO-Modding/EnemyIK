using BepInEx;
using BepInEx.IL2CPP;
using EnemyIK.IKUpdater;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnemyIK
{
    [BepInPlugin("EnemyIK", "EnemyIK", "1.0.0")]
    internal class EntryPoint : BasePlugin
    {
        public override void Load()
        {
            _Harmony = new Harmony("EnemyIK");
            _Harmony.PatchAll();

            ClassInjector.RegisterTypeInIl2Cpp<CollisionChecker>();
            ClassInjector.RegisterTypeInIl2Cpp<LookAtIKUpdater>();
            ClassInjector.RegisterTypeInIl2Cpp<BodyIKUpdater>();
        }

        private Harmony _Harmony;
    }
}