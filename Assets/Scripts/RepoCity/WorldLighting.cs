using UnityEngine;

namespace RepoCity {

    // Scenes here are built entirely from an empty EditorSceneManager.NewScene by batchmode editor
    // scripts, which means no Directional Light ever gets added the way the Editor UI normally adds
    // one to a new scene. Without it everything renders in flat ambient-only light - no sun, no
    // shadows, no highlights - which reads as dull/grey regardless of material color. Call this once
    // from any scene bootstrapper before building geometry.
    public static class WorldLighting {
        public static void Setup() {
            GameObject sunGO = new GameObject("Sun");
            Light sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunGO.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.24f, 0.22f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.63f, 0.72f, 0.82f);
            RenderSettings.fogDensity = 0.012f;
        }
    }
}
