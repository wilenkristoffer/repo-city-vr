using UnityEngine;

namespace RepoCity {

    // The single entry point for the scene: add this to one empty GameObject
    // and everything else (city, rig, HUD) is created at runtime.
    public class SceneBootstrapper : MonoBehaviour {

        int inspectToken = 0;

        void Start() {
            RepoData data;
            CityLayoutResult layout;
            GameObject city;

            try {
                data = RepoDataLoader.Load();
                layout = CityLayoutBuilder.Compute(data);
                city = CityBuilder.Build(layout);
            } catch (System.Exception e) {
                Debug.LogError("SceneBootstrapper: failed to load repo data / build city. " +
                    "Did you run scripts/generate-repo-data.js to generate Assets/Resources/RepoCity/repo-data.json? " +
                    e);
                return;
            }

            city.transform.SetParent(transform, false);

            // Player rig: starts outside the city at +Z, rotated 180 degrees so its forward
            // (and WASD movement, which is rig-relative) actually points at the city at the
            // origin, rather than away from it.
            GameObject rig = new GameObject("Player Rig");
            rig.transform.SetParent(transform, false);
            rig.transform.position = new Vector3(0f, 0f, 40f);
            rig.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Camera cam = Camera.main;
            if (cam != null) {
                cam.transform.SetParent(rig.transform, false);
                cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                cam.transform.localRotation = Quaternion.identity;
            } else {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
                camGO.transform.SetParent(rig.transform, false);
                camGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                camGO.transform.localRotation = Quaternion.identity;
            }

            Locomotion locomotion = rig.AddComponent<Locomotion>();

            // HUD: placed between spawn and the city, facing back toward the spawn point
            // so it reads naturally the moment the player appears.
            GameObject hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(transform, false);
            hudGO.transform.position = new Vector3(0f, 1.5f, 38f);
            // World-space UI canvases read correctly from their LOCAL -Z side, so after
            // LookAt (which points local +Z at the target) we flip 180 degrees so the
            // readable face actually points back at the spawn point/camera.
            hudGO.transform.LookAt(new Vector3(0f, 1.5f, 40f));
            hudGO.transform.Rotate(0f, 180f, 0f);

            HudPanel hudPanel = hudGO.AddComponent<HudPanel>();
            hudPanel.Initialize(layout);

            // Race guard: if the player inspects a new building before a prior
            // narration coroutine finishes, only the latest result wins.
            locomotion.OnInspect = (FileEntry file) => {
                inspectToken++;
                int token = inspectToken;

                if (file == null) {
                    hudPanel.ClearNarration();
                    return;
                }

                hudPanel.ShowNarration(file, "Thinking...");
                StartCoroutine(OllamaNarrator.GetNarration(file, text => {
                    if (token == inspectToken) {
                        hudPanel.ShowNarration(file, text);
                    }
                }));
            };
        }
    }
}
