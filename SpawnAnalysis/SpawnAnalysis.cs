using AIGraph;
using HarmonyLib;
using Player;
using UnityEngine;

namespace SpawnAnalysis
{
    [HarmonyPatch]
    internal static class SpawnAnalysis
    {
        internal const int NUM = 100;  // Does NUM^2 samples.

        internal static bool run = true;

        [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.SpawnGroup))]
        [HarmonyPrefix]
        public static void SpawnGroupPatch()
        {
            // Set `run` so that our other patches know to run their first-time-call info.
            run = true;
            Logger.Info("==================== Spawn Info ====================");
        }

        [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.GetScoredSpawnPoint_DirectionalWeight))]
        [HarmonyPostfix]
        public static void GetScoredSpawnPoint_DirectionalWeightPatch(SurvivalWave __instance, SurvivalWave.ScoredSpawnPoint __result, Vector3 dir, float minDistance, float maxDistance)
        {
            if (!run)
            {
                return;
            }
            SpawnParamsInfo(__instance, dir, minDistance, maxDistance);
            AvailableSpawnsInfo(__instance, dir, minDistance, maxDistance);
            WinningSpawnPointInfo(__result);
        }

        [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.GetScoredSpawnPoint))]
        [HarmonyPostfix]
        public static void GetScoredSpawnPointPatch(SurvivalWave __instance)
        {
            if (!run)
            {
                return;
            }
            // This is the last patch in the chain, so we set `run` to false to avoid everything
            // running while we sim spawn probabilities.
            run = false;
            SpawnProbInfo(__instance);
        }

        public static void SpawnParamsInfo(SurvivalWave instance, Vector3 dir, float minDistance, float maxDistance)
        {
            AIG_CourseNode centerNode = !PlayerManager.TryGetClosestAlivePlayerAgent(instance.m_courseNode, out PlayerAgent playerAgent) ? instance.m_courseNode : (playerAgent.CourseNode == null || !playerAgent.CourseNode.IsValid ? instance.m_courseNode : playerAgent.CourseNode);
            Vector3 position = centerNode.Position;
            Logger.Info($"Spawn parameters:\n  minDistance: {minDistance:F1} maxDistance: {maxDistance:F1} dir: ({dir.x:F2} {dir.z:F2})\n" +
                $"  alarmArea: {NodeName(instance.m_courseNode)} centerArea: {NodeName(centerNode)} centerAreaPos: ({position.x:F1} {position.z:F1})");
        }

        public static void AvailableSpawnsInfo(SurvivalWave instance, Vector3 dir, float minDistance, float maxDistance)
        {
            // Track some extra info.
            System.Collections.Generic.Dictionary<int, (int Index, float PathCost, float DirCost)> extraInfo = new();
            int i = 0;
            // Perform the scoring that `GetScoredSpawnPoint_DirectionalWeight` does.
            AIG_CourseNode centerNode = !PlayerManager.TryGetClosestAlivePlayerAgent(instance.m_courseNode, out PlayerAgent playerAgent) ? instance.m_courseNode : (playerAgent.CourseNode == null || !playerAgent.CourseNode.IsValid ? instance.m_courseNode : playerAgent.CourseNode);
            Il2CppSystem.Collections.Generic.List<SurvivalWave.ScoredSpawnPoint> availableSpawnPoints = instance.GetAvailableSpawnPoints(centerNode);
            Vector3 position = centerNode.Position;
            foreach (SurvivalWave.ScoredSpawnPoint spawnPoint in availableSpawnPoints)
            {
                Vector3 lhs = (spawnPoint.firstCoursePortal.Position - position) with { y = 0.0f };
                lhs.Normalize();
                spawnPoint.m_dir = lhs;
                spawnPoint.totalCost = Mathf.Clamp01(Vector3.Dot(lhs, dir));
                if ((double)spawnPoint.pathHeat > (double)minDistance - 0.0099999997764825821)
                    spawnPoint.totalCost += (float)(1.0 + (1.0 - (double)Mathf.Clamp(spawnPoint.pathHeat - minDistance, 0.0f, maxDistance) / (double)maxDistance));

                // The extra info we're tracking.
                extraInfo[spawnPoint.courseNode.NodeID] = (
                    i,
                    (double)spawnPoint.pathHeat > (double)minDistance - 0.0099999997764825821 ? (float)(1.0 + (1.0 - (double)Mathf.Clamp(spawnPoint.pathHeat - minDistance, 0.0f, maxDistance) / (double)maxDistance)) : 0,
                    Mathf.Clamp01(Vector3.Dot(lhs, dir))
                );
                i++;
            }
            // Convert to System list so we can sort it by other metrics if we want.
            System.Collections.Generic.List<SurvivalWave.ScoredSpawnPoint> orderableSpawnPoints = new();
            foreach (SurvivalWave.ScoredSpawnPoint spawnPoint in availableSpawnPoints)
            {
                orderableSpawnPoints.Add(spawnPoint);
            }
            // Print info on each spawn point.
            string spawnPointsString = "";
            foreach (SurvivalWave.ScoredSpawnPoint spawnPoint in orderableSpawnPoints)
            {
                string extraInfoString = $"  index: {extraInfo[spawnPoint.courseNode.NodeID].Index} pathCost: {extraInfo[spawnPoint.courseNode.NodeID].PathCost} dirCost: {extraInfo[spawnPoint.courseNode.NodeID].DirCost}";
                spawnPointsString += SpawnPointInfo(spawnPoint) + "\n" + extraInfoString + "\n";
            }
            Logger.Info("Available Spawn Points:\n" + spawnPointsString.TrimEnd());
        }

        public static void WinningSpawnPointInfo(SurvivalWave.ScoredSpawnPoint winner)
        {
            Logger.Info("Winning spawn point:\n" + SpawnPointInfo(winner));
        }

        public static void SpawnProbInfo(SurvivalWave instance)
        {
            System.Collections.Generic.Dictionary<int, int> usedSpawnPoints = new();
            System.Collections.Generic.Dictionary<int, string> spawnPointNames = new();
            for (int i = 0; i < NUM; ++i)
            {
                for (int j = 0; j < NUM; ++j)
                {
                    float x = (i + 0.5f) / NUM;
                    float y = (j + 0.5f) / NUM;
                    // +ve x +ve y
                    Vector3 vec = new Vector3(x, 0, y).normalized;
                    SurvivalWave.ScoredSpawnPoint spawnPoint = instance.GetScoredSpawnPoint(vec);
                    if (usedSpawnPoints.ContainsKey(spawnPoint.courseNode.NodeID))
                    {
                        usedSpawnPoints[spawnPoint.courseNode.NodeID]++;
                    }
                    else
                    {
                        usedSpawnPoints[spawnPoint.courseNode.NodeID] = 1;
                        spawnPointNames[spawnPoint.courseNode.NodeID] = SpawnPointName(spawnPoint);
                    }
                    // -ve x -ve y
                    vec = new Vector3(-x, 0, -y).normalized;
                    spawnPoint = instance.GetScoredSpawnPoint(vec);
                    if (usedSpawnPoints.ContainsKey(spawnPoint.courseNode.NodeID))
                    {
                        usedSpawnPoints[spawnPoint.courseNode.NodeID]++;
                    }
                    else
                    {
                        usedSpawnPoints[spawnPoint.courseNode.NodeID] = 1;
                        spawnPointNames[spawnPoint.courseNode.NodeID] = SpawnPointName(spawnPoint);
                    }
                }
            }
            string spawnProbString = "";
            foreach (var sp in usedSpawnPoints.OrderBy(kv => -kv.Value))
            {
                spawnProbString += $"{spawnPointNames[sp.Key]}: {(100f * sp.Value) / (2 * NUM * NUM):F2}%\n";
            }
            Logger.Info("Spawn Probabilities:\n" + spawnProbString.TrimEnd());
        }

        public static string SpawnPointInfo(SurvivalWave.ScoredSpawnPoint spawnPoint)
        {
            return SpawnPointName(spawnPoint) + $"\n  totalCost: {spawnPoint.totalCost} pathHeat: {spawnPoint.pathHeat} pathDistance: {spawnPoint.pathDistance}\n" +
                $"  firstCoursePortal: {NodeName(spawnPoint.firstCoursePortal.m_nodeA)}-{NodeName(spawnPoint.firstCoursePortal.m_nodeB)} pos: ({spawnPoint.firstCoursePortal.Position.x:F1} {spawnPoint.firstCoursePortal.Position.z:F1}) m_dir: ({spawnPoint.m_dir.x:F2} {spawnPoint.m_dir.z:F2})";
        }

        public static string SpawnPointName(SurvivalWave.ScoredSpawnPoint spawnPoint)
        {
            return NodeName(spawnPoint.courseNode);
        }

        public static string NodeName(AIG_CourseNode courseNode)
        {
            //return courseNode.m_zone.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_No_Formatting) + " " + courseNode.m_area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_No_Formatting);
            return courseNode.m_zone.m_navInfo.Number + courseNode.m_area.m_navInfo.Suffix;
        }
    }
}