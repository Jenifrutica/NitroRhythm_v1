using System.Collections.Generic;
using UnityEngine;

namespace NitroRhythm.World
{
    /// <summary>
    /// Low-poly meshes generated in code (zero asset weight for the WebGL build):
    /// torus/arc, cone, crystal, rock/floating island.
    /// </summary>
    public static class ProceduralMeshes
    {
        /// <summary>Un-welds the vertices so every triangle is flat shaded (stylised low-poly look).</summary>
        private static Mesh Flat(List<Vector3> v, List<int> t, string name)
        {
            Vector3[] verts = new Vector3[t.Count];
            Vector2[] uvs = new Vector2[t.Count];
            int[] tris = new int[t.Count];
            for (int i = 0; i < t.Count; i++)
            {
                verts[i] = v[t[i]];
                tris[i] = i;
                uvs[i] = new Vector2(verts[i].x + verts[i].z * 0.5f, verts[i].y + verts[i].z * 0.5f) * 0.15f;
            }

            Mesh mesh = new Mesh { name = name };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Smooth torus (or an open arc when arcDegrees &lt; 360) lying in the XY plane.</summary>
        public static Mesh Torus(float major, float minor, int segments = 28, int sides = 10, float arcDegrees = 360f)
        {
            bool closed = arcDegrees >= 359.9f;
            int ringCount = closed ? segments : segments + 1;
            Vector3[] verts = new Vector3[ringCount * sides];
            Vector2[] uvs = new Vector2[verts.Length];
            List<int> tris = new List<int>();

            for (int i = 0; i < ringCount; i++)
            {
                float a = Mathf.Deg2Rad * arcDegrees * i / segments;
                Vector3 center = new Vector3(Mathf.Cos(a) * major, Mathf.Sin(a) * major, 0f);
                Vector3 radial = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                for (int j = 0; j < sides; j++)
                {
                    float b = Mathf.PI * 2f * j / sides;
                    verts[i * sides + j] = center + (radial * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)) * minor;
                    uvs[i * sides + j] = new Vector2((float)i / segments, (float)j / sides);
                }
            }

            int rings = closed ? segments : segments;
            for (int i = 0; i < rings; i++)
            {
                int next = (i + 1) % ringCount;
                for (int j = 0; j < sides; j++)
                {
                    int jn = (j + 1) % sides;
                    int a = i * sides + j, b = next * sides + j, c = next * sides + jn, d = i * sides + jn;
                    tris.AddRange(new[] { a, b, c, a, c, d });
                }
            }

            Mesh mesh = new Mesh { name = "Torus", vertices = verts, uv = uvs, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Pointed cone (spike/spire). <paramref name="jitter"/> roughens the base ring.</summary>
        public static Mesh Cone(float radius, float height, int sides = 7, float jitter = 0f, int seed = 1)
        {
            System.Random rng = new System.Random(seed);
            List<Vector3> v = new List<Vector3>();
            v.Add(new Vector3(0f, height, 0f));
            v.Add(Vector3.zero);
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 2f * i / sides;
                float r = radius * (1f + ((float)rng.NextDouble() - 0.5f) * 2f * jitter);
                v.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }

            List<int> t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                int a = 2 + i, b = 2 + (i + 1) % sides;
                t.AddRange(new[] { 0, b, a });
                t.AddRange(new[] { 1, a, b });
            }
            return Flat(v, t, "Cone");
        }

        /// <summary>Hexagonal crystal: prism with a pointed tip.</summary>
        public static Mesh Crystal(float radius, float height, int sides = 6)
        {
            List<Vector3> v = new List<Vector3>();
            v.Add(new Vector3(0f, height, 0f));      // tip
            v.Add(Vector3.zero);                     // bottom centre
            for (int i = 0; i < sides; i++)          // bottom ring
            {
                float a = Mathf.PI * 2f * i / sides;
                v.Add(new Vector3(Mathf.Cos(a) * radius * 0.8f, 0f, Mathf.Sin(a) * radius * 0.8f));
            }
            for (int i = 0; i < sides; i++)          // shoulder ring
            {
                float a = Mathf.PI * 2f * i / sides + 0.1f;
                v.Add(new Vector3(Mathf.Cos(a) * radius, height * 0.72f, Mathf.Sin(a) * radius));
            }

            List<int> t = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int b0 = 2 + i, b1 = 2 + n, s0 = 2 + sides + i, s1 = 2 + sides + n;
                t.AddRange(new[] { b0, s0, s1, b0, s1, b1 });     // side quad
                t.AddRange(new[] { s0, 0, s1 });                  // tip
                t.AddRange(new[] { 1, b0, b1 });                  // bottom
            }
            return Flat(v, t, "Crystal");
        }

        /// <summary>Noisy low-poly rock; with <paramref name="island"/> it has a flat top and a tapering underside.</summary>
        public static Mesh Rock(float radius, int seed, bool island = false)
        {
            int lat = 7, lon = 10;
            List<Vector3> v = new List<Vector3>();
            float ox = seed * 13.17f, oz = seed * 7.31f;

            for (int i = 0; i <= lat; i++)
            {
                float phi = Mathf.PI * i / lat;
                for (int j = 0; j < lon; j++)
                {
                    float theta = Mathf.PI * 2f * j / lon;
                    Vector3 dir = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    float noise = 0.78f + Mathf.PerlinNoise(ox + dir.x * 1.7f + 5f, oz + dir.z * 1.7f + dir.y * 1.3f + 5f) * 0.55f;
                    Vector3 p;
                    if (!island)
                    {
                        p = dir * radius * noise;
                        p.y *= 0.72f;
                    }
                    else if (dir.y >= 0f)
                    {
                        p = dir * radius * noise;
                        p.y *= 0.22f;
                    }
                    else
                    {
                        float taper = 1f + dir.y * 0.7f;
                        p = new Vector3(dir.x * radius * taper, dir.y * radius * 1.7f, dir.z * radius * taper) * noise;
                    }
                    v.Add(p);
                }
            }

            List<int> t = new List<int>();
            for (int i = 0; i < lat; i++)
            {
                for (int j = 0; j < lon; j++)
                {
                    int a = i * lon + j, b = i * lon + (j + 1) % lon, c = (i + 1) * lon + (j + 1) % lon, d = (i + 1) * lon + j;
                    t.AddRange(new[] { a, c, b, a, d, c });
                }
            }
            return Flat(v, t, island ? "Island" : "Rock");
        }
    }
}
