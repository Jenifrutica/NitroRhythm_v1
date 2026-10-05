using System.Text;
using NitroRhythm.Core;
using UnityEngine;
using UnityEditor;

namespace NitroRhythm.EditorTools
{
    /// <summary>
    /// Measures kart/pilot bounds exactly as <see cref="NitroRhythm.UI.CharacterDisplay"/>
    /// lays them out, so per-character pilot offsets come from data, not guesses.
    /// Run: -executeMethod NitroRhythm.EditorTools.ModelAlignmentProbe.Run
    /// </summary>
    public static class ModelAlignmentProbe
    {
        private const float KartTarget = 3.8f;
        private const float PilotTarget = 1.9f;

        [MenuItem("NitroRhythm/Probe Model Alignment")]
        public static void Run()
        {
            StringBuilder report = new StringBuilder("[AlignmentProbe]\n");
            for (int i = 1; i <= 3; i++)
            {
                Bounds kart = Measure($"Kart_Neon_0{i}", KartTarget, Quaternion.Euler(0f, 180f, 0f), Vector3.zero);
                Bounds pilot = Measure($"Piloto_Neon_0{i}", PilotTarget, Quaternion.identity, new Vector3(0f, 0.45f, 0f));
                Vector3 delta = pilot.center - kart.center;
                report.AppendLine($"0{i}: kart c={kart.center:F3} min={kart.min:F3} max={kart.max:F3} | pilot c={pilot.center:F3} min={pilot.min:F3} max={pilot.max:F3} | pilot-kart XZ=({delta.x:F3},{delta.z:F3}) pilotBottom-kartTop={(pilot.min.y - kart.max.y):F3}");
            }
            Debug.Log(report.ToString());
        }

        private static Bounds Measure(string model, float target, Quaternion rotation, Vector3 position)
        {
            GameObject prefab = Resources.Load<GameObject>($"NitroRhythm/Models/{model}");
            GameObject go = Object.Instantiate(prefab);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Bounds raw = Combine(go);
            float max = Mathf.Max(raw.size.x, Mathf.Max(raw.size.y, raw.size.z));
            go.transform.localScale = Vector3.one * (target / Mathf.Max(0.0001f, max));
            go.transform.rotation = rotation;
            go.transform.position = position;

            Bounds result = Combine(go);
            Object.DestroyImmediate(go);
            return result;
        }

        /// <summary>World-space bounds from real vertices (skinned meshes are baked first).</summary>
        private static Bounds Combine(GameObject go)
        {
            bool init = false;
            Bounds b = new Bounds();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh;
                Matrix4x4 m;
                if (r is SkinnedMeshRenderer smr)
                {
                    mesh = new Mesh();
                    smr.BakeMesh(mesh, true);
                    m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                }
                else if (r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                {
                    mesh = mf.sharedMesh;
                    m = r.transform.localToWorldMatrix;
                }
                else continue;

                foreach (Vector3 v in mesh.vertices)
                {
                    Vector3 w = m.MultiplyPoint3x4(v);
                    if (!init) { b = new Bounds(w, Vector3.zero); init = true; }
                    else b.Encapsulate(w);
                }
            }
            return b;
        }
    }
}
