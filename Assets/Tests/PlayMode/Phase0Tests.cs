using System.Collections;
using NUnit.Framework;
using NitroRhythm.Audio;
using NitroRhythm.Combat;
using NitroRhythm.Controls;
using NitroRhythm.Core;
using NitroRhythm.Data;
using NitroRhythm.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Round 2, phase 0: villain combat, checkpoint count, kart orientation.</summary>
    public class Phase0Tests
    {
        private static readonly string[] Leftovers = { "VillainBarrier", "BadRewardProjectile", "ImpactMarker", "BarrierMarker", "BlastRing", "TestKart" };

        [TearDown]
        public void CleanLeftovers()
        {
            foreach (string leftover in Leftovers)
            {
                GameObject go;
                while ((go = GameObject.Find(leftover)) != null) Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Villain_StaysInRangeAndAttacksTheLeadPlayer()
        {
            // Karts/villains left over from earlier tests would be picked as the "lead player".
            foreach (PlayerKartController stale in Object.FindObjectsByType<PlayerKartController>(FindObjectsSortMode.None)) Object.Destroy(stale.gameObject);
            foreach (VillainBoss stale in Object.FindObjectsByType<VillainBoss>(FindObjectsSortMode.None)) Object.Destroy(stale.gameObject);
            yield return null;

            GameObject managerObject = new GameObject("TestLevelManager");
            LevelManager manager = managerObject.AddComponent<LevelManager>();
            manager.SetAutoBuild(false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(400f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(1200f, 1f, 30f);

            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerObject.transform.position = new Vector3(0f, 1.2f, 0f);
            playerObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            PlayerKartController player = playerObject.AddComponent<PlayerKartController>();
            ScriptedInputProvider input = new ScriptedInputProvider();
            player.SetInputProvider(input);
            input.SetDrive(1f, 0f);
            player.SpeedMultiplier = 0.3f;   // keeps the player inside this short test level (the villain stops at the level end)
            manager.RegisterPlayer(player);

            GameObject villainObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villainObject.transform.localScale = new Vector3(2.2f, 1.2f, 3.8f);
            villainObject.transform.position = new Vector3(60f, 1.2f, 0f);
            villainObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Rigidbody body = villainObject.AddComponent<Rigidbody>();
            body.mass = 50f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            VillainBoss villain = villainObject.AddComponent<VillainBoss>();
            villain.SetPlayerControlled(false);

            float maxGap = 0f;
            float minGap = float.MaxValue;
            float elapsed = 0f;
            while (elapsed < 14f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed > 3f)
                {
                    float gap = villainObject.transform.position.x - playerObject.transform.position.x;
                    maxGap = Mathf.Max(maxGap, gap);
                    minGap = Mathf.Min(minGap, gap);
                }
            }

            Assert.GreaterOrEqual(villain.ShotsFired, 2, "El villano debe atacar varias veces en 14 s.");
            Assert.Less(maxGap, 75f, $"El villano no debe alejarse del líder (gap máx {maxGap:F0} m).");
            Assert.Greater(minGap, -15f, $"El villano no debe quedar muy atrás (gap mín {minGap:F0} m).");

            Object.Destroy(villainObject);
            Object.Destroy(playerObject);
            Object.Destroy(ground);
            Object.Destroy(managerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Villain_EveryAttackKindRunsWithoutErrors()
        {
            GameObject managerObject = new GameObject("TestLevelManager");
            LevelManager manager = managerObject.AddComponent<LevelManager>();
            manager.SetAutoBuild(false);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(100f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(400f, 1f, 30f);

            GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playerObject.transform.position = new Vector3(0f, 1.2f, 0f);
            PlayerKartController player = playerObject.AddComponent<PlayerKartController>();
            manager.RegisterPlayer(player);

            GameObject villainObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            villainObject.transform.position = new Vector3(30f, 1.2f, 0f);
            villainObject.AddComponent<Rigidbody>();
            VillainBoss villain = villainObject.AddComponent<VillainBoss>();
            yield return null;

            foreach (VillainBoss.AttackKind kind in System.Enum.GetValues(typeof(VillainBoss.AttackKind)))
            {
                villain.Attack(player, kind);
                yield return new WaitForSeconds(0.3f);
            }

            Assert.AreEqual(4, villain.ShotsFired);
            Assert.IsNotNull(GameObject.Find("BadRewardProjectile") ?? GameObject.Find("BarrierMarker"));

            yield return new WaitForSeconds(2f);
            Object.Destroy(villainObject);
            Object.Destroy(playerObject);
            Object.Destroy(ground);
            Object.Destroy(managerObject);
            yield return null;
        }

        [Test]
        public void Checkpoints_AreFewAndEvenlySpread()
        {
            AudioAnalysisResult analysis = AudioAnalysisResult.CreateFlat(140f, 64);
            int total = 0;
            for (int level = 1; level <= 7; level++)
            {
                LevelDefinition def = PrototypeData.Instance.PlayableLevels[level - 1];
                ProceduralTrackPlan plan = ProceduralTrackBuilder.Build(analysis, def, 0f);
                var picks = LevelManager.ChooseCheckpointSegments(plan, level);

                Assert.AreEqual(LevelManager.CheckpointCountForLevel(level), picks.Count, $"Nivel {level}: cantidad de checkpoints.");
                Assert.LessOrEqual(picks.Count, 3);
                foreach (int index in picks)
                {
                    Assert.Greater(index, 0, "No debe haber checkpoint en el primer tramo.");
                    Assert.Less(index, plan.segments.Count - 1, "No debe haber checkpoint en el último tramo.");
                }
                total += picks.Count;
            }

            Assert.LessOrEqual(total, 14, "El total de checkpoints de la carrera debe ser pequeño.");
            Assert.GreaterOrEqual(total, 7, "Debe haber al menos uno por nivel.");
        }

        private static Bounds WorldBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        [UnityTest]
        public IEnumerator Kart_ModelFacesForward_AndPilotSitsBehindCenter()
        {
            foreach (CharacterDefinition def in PrototypeData.Instance.characters)
            {
                GameObject kart = VisualEntityFactory.CreateKartEntity("TestKart", def.Color, null, def.kartModel, def.texture, def.pilotModel, def.pilotOffset, def.pilotScale);
                kart.transform.localScale = new Vector3(2.2f, 1.2f, 3.8f);
                kart.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                kart.transform.position = new Vector3(0f, 50f, 0f);
                kart.AddComponent<Rigidbody>().isKinematic = true;
                yield return null;
                yield return null;
                yield return null;

                Transform visual = kart.transform.Find($"{def.kartModel}_Visual");
                Transform pilot = kart.transform.Find($"{def.pilotModel}_Pilot");
                Assert.IsNotNull(visual, $"{def.id}: el kart debe mostrar su modelo real, no un cubo.");
                Assert.IsNotNull(pilot, $"{def.id}: el piloto debe aparecer en carrera.");

                Vector3 delta = WorldBounds(pilot).center - WorldBounds(visual).center;
                float along = Vector3.Dot(delta, kart.transform.forward);
                float side = Vector3.Dot(delta, kart.transform.right);
                Debug.Log($"[Facing] {def.id}: pilot offset along forward={along:F2} side={side:F2}");

                Assert.Less(along, -0.3f, $"{def.id}: el piloto debe ir detrás del centro (asiento), el kart mira a +X.");
                Assert.Less(Mathf.Abs(side), 0.6f, $"{def.id}: el piloto debe ir centrado.");

                Object.Destroy(kart);
                yield return null;
            }
        }
    }
}
