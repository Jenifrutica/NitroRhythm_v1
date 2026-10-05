using UnityEngine;

namespace NitroRhythm.Core
{
    /// <summary>
    /// Blender exports can carry cameras, lights and audio listeners (PrizeGood.fbx had a camera that hijacked
    /// the game view). Every instantiated FBX goes through here to switch them off.
    /// They are disabled, not destroyed: URP attaches extra data components to cameras/lights, and destroying
    /// the camera at runtime made <c>UniversalAdditionalCameraData.OnDestroy</c> throw, which broke all 3D
    /// rendering in the release WebGL build.
    /// </summary>
    public static class ModelSanitizer
    {
        public static GameObject Strip(GameObject instance)
        {
            if (instance == null) return null;
            foreach (Camera c in instance.GetComponentsInChildren<Camera>(true)) Disable(c.gameObject, c);
            foreach (Light l in instance.GetComponentsInChildren<Light>(true)) Disable(l.gameObject, l);
            foreach (AudioListener a in instance.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
            return instance;
        }

        private static void Disable(GameObject owner, Behaviour component)
        {
            component.enabled = false;
            // Camera/light-only helper objects can go entirely; meshes keep their object.
            if (owner.GetComponent<Renderer>() == null && owner.transform.childCount == 0) owner.SetActive(false);
        }
    }
}
