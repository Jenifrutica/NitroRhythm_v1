using NitroRhythm.Audio;
using NitroRhythm.Player;
using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Intermediate checkpoint volume. The first active player to touch it
    /// advances the LevelManager's respawn point (forward only). It is drawn as
    /// a neon gate that is amber while pending and turns green once reached, so
    /// players can read where they will respawn (hallazgo P9).
    /// </summary>
    public class CheckpointComponent : MonoBehaviour
    {
        private static readonly Color PendingColor = new Color(1f, 0.72f, 0.1f);
        private static readonly Color ReachedColor = new Color(0.2f, 1f, 0.45f);

        private LevelManager _levelManager;
        private Renderer[] _gateRenderers = new Renderer[0];
        private bool _reached;
        private Light _light;

        public bool Reached => _reached;

        /// <summary>
        /// Gate across the track: the user's speaker towers (Torre) as pillars and a glowing beam
        /// that is amber while pending and green once reached.
        /// </summary>
        public void BuildGate(float trackHalfWidth)
        {
            GameObject torre = Resources.Load<GameObject>("NitroRhythm/Props/Torre1");
            System.Collections.Generic.List<Renderer> glow = new System.Collections.Generic.List<Renderer>();

            if (torre != null)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    NitroRhythm.World.PropSpec spec = new NitroRhythm.World.PropSpec(NitroRhythm.World.PropKind.Torre, 0, 0, 0, 1, 1, 0, 0, Color.white, PendingColor, 1);
                    GameObject pillar = NitroRhythm.World.PropFactory.Create(spec, NitroRhythm.World.DomainTheme.Current ?? new NitroRhythm.World.DomainTheme(), new System.Random(side + 9));
                    pillar.transform.SetParent(transform, false);
                    pillar.transform.localScale = Vector3.one * 4.6f;
                    // The gate object sits 1.2 above the platform; bring the tower base down to the floor.
                    pillar.transform.localPosition = new Vector3(0f, -1.2f, side * (trackHalfWidth - 0.7f));
                }
            }
            else
            {
                glow.Add(CreatePart("PillarL", new Vector3(0f, 0.4f, -trackHalfWidth + 0.3f), new Vector3(0.45f, 4.2f, 0.45f)));
                glow.Add(CreatePart("PillarR", new Vector3(0f, 0.4f, trackHalfWidth - 0.3f), new Vector3(0.45f, 4.2f, 0.45f)));
            }

            glow.Add(CreatePart("Beam", new Vector3(0f, 3.4f, 0f), new Vector3(0.35f, 0.3f, trackHalfWidth * 2f - 1.4f)));
            glow.Add(CreatePart("BeamLow", new Vector3(0f, 3.0f, 0f), new Vector3(0.12f, 0.1f, trackHalfWidth * 2f - 1.4f)));
            _gateRenderers = glow.ToArray();
            Paint(PendingColor, 2.4f);

            Light light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = PendingColor;
            light.range = 14f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;
            _light = light;
        }

        private Renderer CreatePart(string partName, Vector3 localPosition, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            Destroy(part.GetComponent<Collider>());
            return part.GetComponent<Renderer>();
        }

        private void Paint(Color color, float intensity)
        {
            Material material = VisualEntityFactory.CreateEmissiveMaterial(color, intensity);
            foreach (Renderer renderer in _gateRenderers)
            {
                if (renderer != null) renderer.sharedMaterial = material;
            }
        }

        private void Start()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null) collider.isTrigger = true;

            _levelManager = FindFirstObjectByType<LevelManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_levelManager == null)
            {
                _levelManager = FindFirstObjectByType<LevelManager>();
                if (_levelManager == null) return;
            }

            PlayerKartController kart = other.GetComponentInParent<PlayerKartController>();
            if (kart == null) return;

            if (_levelManager.ActivateCheckpoint(transform.position) && !_reached)
            {
                _reached = true;
                Paint(ReachedColor, 3.2f);
                if (_light != null) _light.color = ReachedColor;
                SfxPlayer.Play(SfxLibrary.Checkpoint, 0.7f);
            }
        }
    }
}
