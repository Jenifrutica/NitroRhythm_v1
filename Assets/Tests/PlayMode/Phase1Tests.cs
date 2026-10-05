using System.Collections;
using NUnit.Framework;
using NitroRhythm.Audio;
using NitroRhythm.Core;
using NitroRhythm.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>Round 2, phase 1: each domain is its own gameplay screen.</summary>
    public class Phase1Tests
    {
        private static void CountPlatforms(out int level3, out int others)
        {
            level3 = 0;
            others = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.name == "Level3_Platform") level3++;
                else if (t.name.StartsWith("Level") && t.name.EndsWith("_Platform")) others++;
            }
        }

        [Test]
        public void GameSession_RunAdvancesThroughAllPlayableDomains()
        {
            GameSession session = GameSession.EnsureExists();
            session.BeginRun();
            Assert.AreEqual(1, session.CurrentLevelIndex);
            Assert.AreEqual(0, session.LastScore);

            int total = PrototypeData.Instance.PlayableCount;
            for (int i = 1; i < total; i++)
            {
                Assert.IsTrue(session.AdvanceLevel(), $"Debe poder pasar del dominio {i} al {i + 1}.");
            }
            Assert.AreEqual(total, session.CurrentLevelIndex);
            Assert.IsFalse(session.AdvanceLevel(), "Tras el último dominio no hay más pantallas.");

            session.BeginRun();
        }

        [Test]
        public void GameSession_RestartRewindsTotalsToTheLevelStart()
        {
            GameSession session = GameSession.EnsureExists();
            session.BeginRun();
            session.LastScore = 500;
            session.LastTime = 40f;
            session.MarkLevelStart();

            session.LastScore = 900;
            session.LastTime = 75f;
            session.RestartLevel();

            Assert.AreEqual(500, session.LastScore);
            Assert.AreEqual(40f, session.LastTime, 0.001f);
            session.BeginRun();
        }

        [UnityTest]
        public IEnumerator LevelManager_BuildsOnlyTheRequestedDomain()
        {
            GameObject go = new GameObject("TestLevelManager");
            LevelManager manager = go.AddComponent<LevelManager>();
            manager.SetAutoBuild(false);

            int total = PrototypeData.Instance.PlayableCount;
            CountPlatforms(out int level3Before, out int othersBefore);   // other tests may leave tracks behind
            manager.GenerateSingleLevel(AudioAnalysisResult.CreateFlat(140f, 64), 3, total, 0f);
            yield return null;

            Assert.IsTrue(manager.SingleLevelMode);
            Assert.AreEqual(3, manager.CurrentLevel);
            Assert.AreEqual(total, manager.TotalLevels);
            Assert.Greater(manager.GetLevelEndX(3), manager.GetLevelStartX(3) + 50f);
            Assert.AreEqual(0f, manager.GetLevelStartX(3), 0.01f);

            CountPlatforms(out int level3After, out int othersAfter);
            int platformsOfLevel3 = level3After - level3Before;
            int platformsOfOthers = othersAfter - othersBefore;
            Assert.Greater(platformsOfLevel3, 5);
            Assert.AreEqual(0, platformsOfOthers, "Solo debe existir el dominio actual.");

            Object.Destroy(go);
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t != null && t.name == "ProceduralTrack") Object.Destroy(t.gameObject);
            }
            yield return null;
        }
    }
}
