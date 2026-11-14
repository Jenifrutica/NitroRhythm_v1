using System.Collections.Generic;
using UnityEngine;

namespace NitroRhythm.Player
{
    /// <summary>
    /// Gives the single-mesh kart FBX four wheel bones at load time (no re-export, so the model keeps
    /// exactly its original placement): the wheels are found as the low vertices of each corner,
    /// the tyre width from where it touches the ground, and only the largest connected piece inside
    /// each wheel cylinder is kept (fender lips or engine parts nearby stay on the body). The mesh
    /// becomes a SkinnedMeshRenderer with bones Wheel_FL/FR/RL/RR that <see cref="KartAnimator"/> spins.
    /// Same algorithm as Docs/Tools/blender/rig_models.py (kept there for reference).
    /// </summary>
    public static class KartWheelRig
    {
        private class RigData
        {
            public Mesh SkinnedMesh;
            public Vector3[] Centres;   // FL, FR, RL, RR in mesh space
        }

        private static readonly Dictionary<Mesh, RigData> Cache = new Dictionary<Mesh, RigData>();
        private static readonly string[] Names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };

        /// <summary>Rigs the kart visual; returns the 4 wheel bones (or an empty array if it cannot).</summary>
        public static Transform[] Build(GameObject visual)
        {
            MeshFilter filter = visual.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer meshRenderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || meshRenderer == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
            {
                return new Transform[0];
            }

            RigData data = GetOrCreate(filter.sharedMesh);
            if (data == null) return new Transform[0];

            Transform body = filter.transform;
            Transform[] wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                wheels[i] = new GameObject(Names[i]).transform;
                wheels[i].SetParent(body, false);
                wheels[i].localPosition = data.Centres[i];
            }

            GameObject skinnedObject = new GameObject("SkinnedBody");
            skinnedObject.transform.SetParent(body, false);
            SkinnedMeshRenderer skinned = skinnedObject.AddComponent<SkinnedMeshRenderer>();
            Transform[] bones = { body, wheels[0], wheels[1], wheels[2], wheels[3] };
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bindposes[i] = bones[i].worldToLocalMatrix * skinnedObject.transform.localToWorldMatrix;
            }
            Mesh mesh = Object.Instantiate(data.SkinnedMesh);
            mesh.bindposes = bindposes;
            skinned.sharedMesh = mesh;
            skinned.bones = bones;
            skinned.rootBone = body;
            skinned.sharedMaterials = meshRenderer.sharedMaterials;
            skinned.shadowCastingMode = meshRenderer.shadowCastingMode;
            skinned.localBounds = mesh.bounds;
            meshRenderer.enabled = false;
            return wheels;
        }

        private static RigData GetOrCreate(Mesh source)
        {
            if (Cache.TryGetValue(source, out RigData cached) && cached != null && cached.SkinnedMesh != null) return cached;

            Vector3[] v = source.vertices;
            if (v.Length == 0) return null;
            Bounds b = source.bounds;
            Vector3 mn = b.min, size = b.size, c = b.center;

            // Mesh space = kart space: +Y up, +Z forward, +X right.
            int[] owner = new int[v.Length];   // 0 body, 1..4 wheels
            Vector3[] centres = new Vector3[4];
            float[] radii = new float[4];
            float[] xMin = new float[4], xMax = new float[4];
            for (int w = 0; w < 4; w++)
            {
                float sz = w < 2 ? 1f : -1f;          // front = +Z
                float sx = (w % 2 == 0) ? -1f : 1f;   // FL / RL on the left (−X)
                float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
                int count = 0;
                foreach (Vector3 p in v)
                {
                    if (p.y > mn.y + size.y * 0.35f) continue;
                    if ((p.x - c.x) * sx <= size.x * 0.25f || (p.z - c.z) * sz <= size.z * 0.15f) continue;
                    x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x);
                    z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                    count++;
                }
                if (count < 20) return null;
                float r = (z1 - z0) * 0.5f;
                // Tyre width from the ground contact only.
                float cx0 = float.MaxValue, cx1 = float.MinValue;
                foreach (Vector3 p in v)
                {
                    if (p.y > mn.y + r * 0.3f) continue;
                    if ((p.x - c.x) * sx <= size.x * 0.25f || (p.z - c.z) * sz <= size.z * 0.15f) continue;
                    cx0 = Mathf.Min(cx0, p.x); cx1 = Mathf.Max(cx1, p.x);
                }
                if (cx0 <= cx1) { x0 = cx0; x1 = cx1; }
                centres[w] = new Vector3((x0 + x1) * 0.5f, mn.y + r, (z0 + z1) * 0.5f);
                radii[w] = r;
                xMin[w] = x0 - size.x * 0.004f;
                xMax[w] = x1 + size.x * 0.004f;
            }

            for (int i = 0; i < v.Length; i++)
            {
                for (int w = 0; w < 4; w++)
                {
                    Vector3 p = v[i];
                    if (p.x < xMin[w] || p.x > xMax[w]) continue;
                    float dy = p.y - centres[w].y, dz = p.z - centres[w].z;
                    if (dy * dy + dz * dz <= radii[w] * radii[w] * 0.96f) { owner[i] = w + 1; break; }
                }
            }

            KeepLargestIslands(source, owner);

            BoneWeight[] weights = new BoneWeight[v.Length];
            for (int i = 0; i < v.Length; i++) weights[i] = new BoneWeight { boneIndex0 = owner[i], weight0 = 1f };

            Mesh skinnedMesh = Object.Instantiate(source);
            skinnedMesh.name = source.name + "_WheelRig";
            skinnedMesh.boneWeights = weights;

            RigData data = new RigData { SkinnedMesh = skinnedMesh, Centres = centres };
            Cache[source] = data;
            return data;
        }

        private static void KeepLargestIslands(Mesh mesh, int[] owner)
        {
            int n = owner.Length;
            // UV seams duplicate vertices: weld by position so a tyre is one connected piece.
            Vector3[] verts = mesh.vertices;
            int[] canon = new int[n];
            Dictionary<Vector3Int, int> byPosition = new Dictionary<Vector3Int, int>();
            float q = 1f / Mathf.Max(1e-6f, mesh.bounds.size.magnitude * 1e-4f);
            for (int i = 0; i < n; i++)
            {
                Vector3Int key = Vector3Int.RoundToInt(verts[i] * q);
                if (!byPosition.TryGetValue(key, out int first)) { first = i; byPosition[key] = i; }
                canon[i] = first;
            }

            List<int>[] neighbours = new List<int>[n];
            int[] tris = mesh.triangles;
            for (int t = 0; t < tris.Length; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int a = canon[tris[t + e]], b = canon[tris[t + (e + 1) % 3]];
                    if (owner[a] == 0 || owner[a] != owner[b]) continue;
                    (neighbours[a] ??= new List<int>()).Add(b);
                    (neighbours[b] ??= new List<int>()).Add(a);
                }
            }

            bool[] seen = new bool[n];
            List<int>[] best = new List<int>[5];
            Stack<int> stack = new Stack<int>();
            for (int i = 0; i < n; i++)
            {
                if (canon[i] != i || owner[i] == 0 || seen[i]) continue;
                List<int> island = new List<int>();
                seen[i] = true;
                stack.Push(i);
                while (stack.Count > 0)
                {
                    int k = stack.Pop();
                    island.Add(k);
                    if (neighbours[k] == null) continue;
                    foreach (int m in neighbours[k])
                    {
                        if (!seen[m]) { seen[m] = true; stack.Push(m); }
                    }
                }
                int w = owner[i];
                if (best[w] == null || island.Count > best[w].Count) best[w] = island;
            }

            bool[] keep = new bool[n];
            for (int w = 1; w <= 4; w++)
            {
                if (best[w] == null) continue;
                foreach (int k in best[w]) keep[k] = true;
            }
            for (int i = 0; i < n; i++) if (owner[i] != 0 && !keep[canon[i]]) owner[i] = 0;
        }
    }
}
