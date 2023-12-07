using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV.PointSet;
using DV.Utils;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mapify.Patches
{
    [HarmonyPatch(typeof(RailwayLodGenerator), nameof(RailwayLodGenerator.Generate))]
    public class RailwayLodGeneratorPatch
    {
        private static ReturnType CallPrivateMethod<ReturnType, ClassType>(ClassType instance, string methodName, object[] parameters = null)
        {
            MethodInfo method = typeof(ClassType).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method is null)
            {
                throw new ArgumentException($"{nameof(ReturnType)}.{methodName} not found");
            }
            return (ReturnType)method.Invoke(instance, parameters);
        }

        // private static List<RailTrack> GetRailTracks(RailwayLodGenerator __instance)
        // {
        //     MethodInfo method = typeof(RailwayLodGenerator).GetMethod("GetRailTracks", BindingFlags.NonPublic | BindingFlags.Instance);
        //     return (List<RailTrack>)method.Invoke(__instance, null);
        // }

        private static bool Prefix(RailwayLodGenerator __instance, ref GameObject __result, ref Vector2[] ___profilePoints)
        {
            ___profilePoints = __instance.profile.GetPoints2D();

            Func<RailTrack, EquiPointSet> kutLambdas = aTrack =>
            {
                Mapify.Log($"RailwayLodGenerator.Generate: '{aTrack.logicTrack.ID}'");
                return CallPrivateMethod<EquiPointSet, RailwayLodGenerator>(__instance, "SimplifiedPoints", new object[]{aTrack});
            };

            var a = CallPrivateMethod<List<RailTrack>, RailwayLodGenerator>(__instance, "GetRailTracks");
            var b = a.Select(kutLambdas);

            var list1 = new List<Mesh>();
            foreach (var aaaaa in b)
            {
                list1.Add( CallPrivateMethod<Mesh, RailwayLodGenerator>(__instance, "GenerateMesh", new object[] { aaaaa }) );
            }

            // .Select(new Func<RailTrack, EquiPointSet>(__instance.SimplifiedPoints)).Select(new Func<EquiPointSet, Mesh>(__instance.GenerateMesh)).ToList();

            var list2 = new List<GameObject>();
            foreach (var ukiweh in list1)
            {
                list2.Add( CallPrivateMethod<GameObject, RailwayLodGenerator>(__instance, "MakeGameObject", new object[] { ukiweh } ) );
            }

            // List<GameObject> list2 = list1.Select(new Func<Mesh, GameObject>(__instance.MakeGameObject)).ToList();


            GameObject gameObject1 = new GameObject("Railway LOD");
            foreach (GameObject gameObject2 in list2)
                gameObject2.transform.SetParent(gameObject1.transform);
            if ((bool) (Object) SingletonBehaviour<WorldMover>.Instance)
                gameObject1.transform.SetParent(SingletonBehaviour<WorldMover>.Instance.originShiftParent);
            int num1 = list1.Sum<Mesh>((Func<Mesh, int>) (m => m.vertexCount));
            int num2 = list1.Sum<Mesh>((Func<Mesh, int>) (m => m.triangles.Length / 3));
            Mapify.Log((object) string.Format("{0} generated {1} meshes, total number of vertices: {2}, triangles: {3}", (object) nameof (RailwayLodGenerator), (object) list1.Count, (object) num1, (object) num2));
            __result = gameObject1;

            return false; //skip original
        }

        // private static Exception Finalizer(Exception __exception, ref RailwayLodGenerator __instance)
        // {
        //     Mapify.LogError($"RailTrack_yes exception on '{__instance.logicTrack.ID}'");
        //     return __exception;
        // }
    }
}
