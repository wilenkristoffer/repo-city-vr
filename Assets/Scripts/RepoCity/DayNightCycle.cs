using UnityEngine;

namespace RepoCity {

    // Rotates WorldLighting's sun over a fixed-length cycle and lerps ambient/fog between day and
    // night tones. Attached once (WorldBootstrapper does this in Start, after WorldLighting.Setup).
    public class DayNightCycle : MonoBehaviour {
        public float cycleLengthSeconds = 300f;

        Light sun;
        Color daySky = new Color(0.55f, 0.65f, 0.78f);
        Color dayEquator = new Color(0.45f, 0.45f, 0.42f);
        Color dayGround = new Color(0.25f, 0.24f, 0.22f);
        Color dayFog = new Color(0.63f, 0.72f, 0.82f);
        const float DayFogDensity = 0.012f;

        Color nightSky = new Color(0.05f, 0.07f, 0.14f);
        Color nightEquator = new Color(0.05f, 0.05f, 0.08f);
        Color nightGround = new Color(0.02f, 0.02f, 0.03f);
        Color nightFog = new Color(0.03f, 0.04f, 0.08f);
        const float NightFogDensity = 0.02f;

        float t;

        void Start() {
            GameObject sunGO = GameObject.Find("Sun");
            if (sunGO != null) sun = sunGO.GetComponent<Light>();
            t = 0.25f; // start at daytime, matching WorldLighting's default sun angle
        }

        void Update() {
            if (sun == null || cycleLengthSeconds <= 0f) return;

            t += Time.deltaTime / cycleLengthSeconds;
            if (t >= 1f) t -= 1f;

            float sunAngle = t * 360f;
            sun.transform.rotation = Quaternion.Euler(sunAngle - 90f, -35f, 0f);

            // Brightest near t=0.25 (sun near zenith), darkest near t=0.75 (sun below horizon).
            float dayFactor = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) * 0.5f + 0.5f);

            sun.intensity = Mathf.Lerp(0.05f, 1.25f, dayFactor);

            RenderSettings.ambientSkyColor = Color.Lerp(nightSky, daySky, dayFactor);
            RenderSettings.ambientEquatorColor = Color.Lerp(nightEquator, dayEquator, dayFactor);
            RenderSettings.ambientGroundColor = Color.Lerp(nightGround, dayGround, dayFactor);
            RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, dayFactor);
            RenderSettings.fogDensity = Mathf.Lerp(NightFogDensity, DayFogDensity, dayFactor);
        }
    }
}
