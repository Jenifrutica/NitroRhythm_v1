using System.Collections;
using System.IO;
using NUnit.Framework;
using NitroRhythm.Data;
using NitroRhythm.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace NitroRhythm.Tests
{
    /// <summary>
    /// Verifies the pilot is seated on its own kart in the character showcase (centred sideways,
    /// over the seat zone behind the kart centre).
    /// Each FBX has a different origin, so the pilot offset is per character.
    /// </summary>
    public class CharacterDisplayAlignmentTests
    {
        private static Bounds RuntimeBounds(Transform root, Transform pivot)
        {
            bool init = false;
            Bounds b = new Bounds();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            {
                // Bounds in pivot space so the spinning pivot does not matter.
                Bounds w = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = w.center + Vector3.Scale(w.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = pivot.InverseTransformPoint(corner);
                    if (!init) { b = new Bounds(p, Vector3.zero); init = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        private static void RenderViews(Camera cam, (string name, Vector3 pos, Vector3 look)[] views, string dir, string id)
        {
            const int w = 1024, h = 768;
            foreach (var v in views)
            {
                cam.transform.position = v.pos;
                cam.transform.LookAt(v.look);
                RenderTexture rt = RenderTexture.GetTemporary(w, h, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{id}_{v.name}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);
                Object.Destroy(tex);
            }
        }

        /// <summary>When NITRO_SHOT_DIR is set, saves front/side renders of the showcase (evidence).</summary>
        private static void SaveShots(GameObject display, Transform pivot, string id)
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;

            Directory.CreateDirectory(dir);
            pivot.rotation = Quaternion.identity;

            GameObject camObject = new GameObject("ShotCam");
            Camera cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f);
            cam.fieldOfView = 35f;

            // (name, camera position, look-at point). Kart views first, then pilot close-ups.
            (string name, Vector3 pos, Vector3 look)[] views =
            {
                ("kart_frente", new Vector3(0f, 2.2f, -9f), new Vector3(0f, 1.3f, 0f)),
                ("kart_lado", new Vector3(9f, 2.2f, 0f), new Vector3(0f, 1.3f, 0f)),
                ("kart_atras", new Vector3(0f, 2.2f, 9f), new Vector3(0f, 1.3f, 0f)),
                ("kart_tres_cuartos", new Vector3(6.5f, 3.6f, -6.5f), new Vector3(0f, 1.2f, 0f)),
                ("kart_arriba", new Vector3(0f, 11f, -0.05f), new Vector3(0f, 0.5f, 0f)),
                ("kart_frente_cerca", new Vector3(0f, 2.6f, -6.2f), new Vector3(0f, 1.6f, 0f)),
            };
            RenderViews(cam, views, dir, id);

            // Pilot only: hide the kart and frame the pilot.
            Transform kartModel = null;
            foreach (Transform child in pivot) if (child.name.StartsWith("Kart_")) kartModel = child;
            if (kartModel != null) kartModel.gameObject.SetActive(false);
            Transform pilotModel = null;
            foreach (Transform child in pivot) if (child.name.StartsWith("Piloto_")) pilotModel = child;
            Vector3 c = pilotModel != null ? pilotModel.GetComponentInChildren<Renderer>().bounds.center : new Vector3(0f, 1f, -0.3f);
            (string name, Vector3 pos, Vector3 look)[] pilotViews =
            {
                ("piloto_frente", c + new Vector3(0f, 0.2f, -4.2f), c),
                ("piloto_lado", c + new Vector3(4.2f, 0.2f, 0f), c),
                ("piloto_atras", c + new Vector3(0f, 0.2f, 4.2f), c),
            };
            RenderViews(cam, pilotViews, dir, id);
            if (kartModel != null) kartModel.gameObject.SetActive(true);
            Object.Destroy(camObject);
        }

        [UnityTest]
        public IEnumerator PilotIsCenteredOnKart_ForEveryCharacter()
        {
            var failures = new System.Collections.Generic.List<string>();
            foreach (CharacterDefinition def in PrototypeData.Instance.characters)
            {
                GameObject go = new GameObject($"Display_{def.id}");
                CharacterDisplay display = go.AddComponent<CharacterDisplay>();
                display.Build(def, Vector3.zero, true, false);
                yield return null;
                yield return null;
                yield return null;

                Transform pivot = go.transform.Find("SpinPivot");
                Transform kart = pivot.Find(def.kartModel);
                Transform pilot = pivot.Find(def.pilotModel);
                Assert.IsNotNull(kart, $"{def.id}: falta el kart");
                Assert.IsNotNull(pilot, $"{def.id}: falta el piloto");

                Bounds kb = RuntimeBounds(kart, pivot);
                Bounds pb = RuntimeBounds(pilot, pivot);
                Vector3 d = pb.center - kb.center;
                Debug.Log($"[Alignment] {def.id}: kart c={kb.center:F3} size={kb.size:F3} | pilot c={pb.center:F3} min={pb.min:F3} size={pb.size:F3} | dXZ=({d.x:F3},{d.z:F3}) pilotBottomVsKartTop={(pb.min.y - kb.max.y):F3}");

                if (Mathf.Abs(d.x) >= 0.5f) failures.Add($"{def.id}: el piloto está desplazado lateralmente ({d.x:F2}).");
                // The kart nose points to -Z in the showcase pivot; the seat sits behind the kart centre.
                if (d.z < 0.3f || d.z > 1.2f) failures.Add($"{def.id}: el piloto no está sobre el asiento (dz={d.z:F2}).");
                if (pb.min.y <= -0.1f) failures.Add($"{def.id}: el piloto atraviesa la base.");

                SaveShots(go, pivot, def.id);

                Object.Destroy(go);
                yield return null;
            }
            Assert.IsEmpty(failures, string.Join(" | ", failures));
        }

        /// <summary>Evidence: the three karts exactly as built for the race (scale, facing), from behind, above and the side.</summary>
        [UnityTest]
        public IEnumerator InRaceKarts_RenderViews()
        {
            string dir = System.Environment.GetEnvironmentVariable("NITRO_SHOT_DIR");
            if (string.IsNullOrEmpty(dir) || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("NITRO_SHOT_DIR no definido.");
            Directory.CreateDirectory(dir);

            GameObject camObject = new GameObject("RaceShotCam");
            Camera cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.09f);
            cam.fieldOfView = 35f;
            GameObject light = new GameObject("L");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            foreach (CharacterDefinition def in PrototypeData.Instance.characters)
            {
                GameObject kart = NitroRhythm.Core.VisualEntityFactory.CreateKartEntity("RaceKart", def.Color, null, def.kartModel, def.texture, def.pilotModel, def.pilotOffset, def.pilotScale);
                kart.transform.localScale = new Vector3(2.2f, 1.2f, 3.8f);
                kart.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // faces +X, as in the race
                kart.transform.position = new Vector3(0f, 50f, 0f);
                kart.AddComponent<Rigidbody>().isKinematic = true;
                yield return null; yield return null; yield return null;

                Vector3 c = kart.transform.position + Vector3.up * 0.6f;
                (string name, Vector3 pos, Vector3 look)[] views =
                {
                    ("carrera_atras", c + new Vector3(-7f, 3f, 0f), c),
                    ("carrera_arriba", c + new Vector3(-0.05f, 10f, 0f), c),
                    ("carrera_lado", c + new Vector3(0f, 1.5f, -8f), c),
                };
                RenderViews(cam, views, dir, def.id);
                Object.Destroy(kart);
                yield return null;
            }
            Object.Destroy(camObject);
            Object.Destroy(light);
        }
    }
}
