using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

namespace RepoCity {

    // Entry point for the multi-repo "World Hub" scene: builds every configured island side by
    // side, a Programming Central plaza, portal signs + connector roads between islands, then the
    // player rig/HUD anchored on the first ("home") island - same overall shape as SceneBootstrapper,
    // just fanned out across several cities instead of one.
    public class WorldBootstrapper : MonoBehaviour {

        int inspectToken = 0;
        List<WorldIslandResult> islands;
        Transform rigTransform;
        string currentIslandLabel;

        void Start() {
            GameSettings.EnsureLoaded();
            AudioManager.EnsureInitialized();
            AudioManager.PlayAmbience("city_traffic_loop", 0.2f);
            WorldLighting.Setup();
            gameObject.AddComponent<DayNightCycle>();

            try {
                islands = WorldBuilder.BuildAll(transform);
            } catch (System.Exception e) {
                Debug.LogError("WorldBootstrapper: WorldBuilder.BuildAll threw - " + e);
                islands = new List<WorldIslandResult>();
            }

            if (islands.Count == 0) {
                Debug.LogError("WorldBootstrapper: no islands built - check repo-data.json resources. Creating a fallback camera so the screen isn't blank.");
                GameObject fallbackCamGO = new GameObject("Main Camera");
                fallbackCamGO.tag = "MainCamera";
                fallbackCamGO.AddComponent<Camera>();
                fallbackCamGO.transform.SetParent(transform, false);
                fallbackCamGO.transform.position = new Vector3(0f, 1.6f, 0f);
                return;
            }

            foreach (WorldIslandResult island in islands) {
                Landmarks.Highlight(island.root);
            }

            WorldIslandResult home = islands[0];
            CityLayoutResult homeLayout = home.layout;

            GameObject connectorsRoot = new GameObject("Connectors");
            connectorsRoot.transform.SetParent(transform, false);
            for (int i = 0; i < islands.Count; i++) {
                if (i > 0) {
                    BuildPortalSign(islands[i], connectorsRoot.transform);
                    BuildConnectorRoad(islands[i - 1], islands[i], connectorsRoot.transform);
                }
            }

            // Programming Central sits west of the home island, clear of its ground/road margin.
            float arcadeX = home.worldCenter.x + homeLayout.bounds.minX - 60f;
            GameObject arcade = CodeArcadeBuilder.Build(new Vector3(arcadeX, 0f, home.worldCenter.z));
            arcade.transform.SetParent(transform, false);

            // Spawn just past the home island's sidewalk ring, same offset SceneBootstrapper uses.
            float spawnZ = home.worldCenter.z + homeLayout.bounds.maxZ + 13f;

            GameObject rig = new GameObject("Player Rig");
            rig.transform.SetParent(transform, false);
            rig.transform.position = new Vector3(home.worldCenter.x, 0f, spawnZ);
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
            HeadTracker.EnsureOn(rig.GetComponentInChildren<Camera>());
            Camera playerCam = rig.GetComponentInChildren<Camera>();

            Locomotion locomotion = rig.AddComponent<Locomotion>();
            rigTransform = rig.transform;
            currentIslandLabel = home.config.label;

            rig.AddComponent<PauseMenu>();

            GameObject leftHandPointerGO = new GameObject("LeftHandPointer");
            leftHandPointerGO.transform.SetParent(rig.transform, false);
            leftHandPointerGO.AddComponent<VrUiPointer>().hand = XRNode.LeftHand;

            GameObject rightHandPointerGO = new GameObject("RightHandPointer");
            rightHandPointerGO.transform.SetParent(rig.transform, false);
            rightHandPointerGO.AddComponent<VrUiPointer>().hand = XRNode.RightHand;

            HandVisual.CreateFor(rig.transform, XRNode.LeftHand);
            HandVisual.CreateFor(rig.transform, XRNode.RightHand);

            if (playerCam != null) {
                GameObject minimapGO = new GameObject("MiniMap");
                if (UnityEngine.XR.XRSettings.isDeviceActive) {
                    // In VR a head-locked corner panel sits at the lens edge, can't be read in full and
                    // can't be dismissed. Mount it on the left controller instead: raise the hand to
                    // read it; clicking the left trackpad shows/hides it.
                    GameObject leftHandAnchorGO = new GameObject("LeftHandAnchor");
                    leftHandAnchorGO.transform.SetParent(rig.transform, false);
                    HandAnchor anchor = leftHandAnchorGO.AddComponent<HandAnchor>();
                    anchor.hand = XRNode.LeftHand;
                    anchor.toggleWithTrackpadClick = true;
                    minimapGO.transform.SetParent(leftHandAnchorGO.transform, false);
                    minimapGO.transform.localPosition = new Vector3(0f, 0.06f, 0.06f);
                    // Tilt the readable face up toward the eyes when the controller is held forward.
                    minimapGO.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                    minimapGO.transform.localScale = Vector3.one * 0.6f;
                } else {
                    minimapGO.transform.SetParent(playerCam.transform, false);
                    minimapGO.transform.localPosition = new Vector3(0.35f, 0.22f, 0.6f);
                    minimapGO.transform.localRotation = Quaternion.identity;
                }
                minimapGO.AddComponent<MiniMapUI>().Initialize(islands, rigTransform);

                GameObject toastGO = new GameObject("AchievementToast");
                toastGO.transform.SetParent(playerCam.transform, false);
                toastGO.transform.localPosition = new Vector3(0f, -0.25f, 0.6f);
                toastGO.transform.localRotation = Quaternion.identity;
                toastGO.AddComponent<AchievementToastUI>();
            }

            GameObject hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(transform, false);
            hudGO.transform.position = new Vector3(home.worldCenter.x, 1.5f, spawnZ - 2f);
            hudGO.transform.LookAt(new Vector3(home.worldCenter.x, 1.5f, spawnZ));
            hudGO.transform.Rotate(0f, 180f, 0f);

            HudPanel hudPanel = hudGO.AddComponent<HudPanel>();
            hudPanel.Initialize(homeLayout);

            locomotion.OnInspect = (FileEntry file) => {
                inspectToken++;
                int token = inspectToken;

                if (file == null) {
                    hudPanel.ClearNarration();
                    return;
                }

                AudioManager.PlayUi("ui_click", 0.5f);
                AchievementTracker.NotifyFileInspected(file.path);
                hudPanel.ShowNarration(file, "Thinking...");
                StartCoroutine(OllamaNarrator.GetNarration(file, text => {
                    if (token == inspectToken) {
                        hudPanel.ShowNarration(file, text);
                        AudioManager.PlayUi("npc_blip", 0.4f);
                    }
                }));
            };

            locomotion.OnInteractNpc = (NpcInfo npc) => {
                inspectToken++;
                int token = inspectToken;

                AudioManager.PlayUi("ui_click", 0.5f);
                AchievementTracker.NotifyNpcTalkedTo(npc.personaName);
                hudPanel.ShowNpcLine(npc.personaName, npc.personaJob, "...");
                StartCoroutine(NpcChat.GetLine(npc, line => {
                    if (token == inspectToken) {
                        hudPanel.ShowNpcLine(npc.personaName, npc.personaJob, line);
                        AudioManager.PlayUi("npc_blip", 0.4f);
                    }
                }));
            };

            // Desktop mouse-click UI interaction (Programming Central's Run buttons) needs an
            // EventSystem; VR controller-ray UI interaction is a known gap, not wired up here.
            if (FindObjectOfType<EventSystem>() == null) {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Kiosks were built before the player camera existed, so their world-space canvases
            // couldn't be given a raycast camera at Initialize() time - assign it now.
            foreach (CodeKiosk kiosk in arcade.GetComponentsInChildren<CodeKiosk>()) {
                if (kiosk.canvas != null) kiosk.canvas.worldCamera = playerCam;
            }
        }

        void Update() {
            if (islands == null || rigTransform == null) return;

            float x = rigTransform.position.x;
            for (int i = 0; i < islands.Count; i++) {
                WorldIslandResult island = islands[i];
                float localX = x - island.worldCenter.x;
                // Roads/sidewalks extend ~5.5 units past bounds, so pad the crossing test a little
                // past that so the notification fires once the player is clearly inside the island.
                if (localX >= island.layout.bounds.minX - 8f && localX <= island.layout.bounds.maxX + 8f) {
                    if (island.config.label != currentIslandLabel) {
                        currentIslandLabel = island.config.label;
                        AchievementTracker.NotifyIslandEntered(island.config.label);
                    }
                    break;
                }
            }
        }

        static void BuildPortalSign(WorldIslandResult island, Transform parent) {
            float labelY = 6f;
            Vector3 signPos = new Vector3(
                island.worldCenter.x + island.layout.bounds.minX - 3f,
                labelY,
                island.worldCenter.z);

            GameObject signGO = new GameObject("Portal_" + island.config.label);
            signGO.transform.SetParent(parent, false);
            signGO.transform.position = signPos;

            TextMesh textMesh = signGO.AddComponent<TextMesh>();
            textMesh.text = island.config.label.ToUpperInvariant();
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.5f;
            textMesh.color = Color.white;

            // The connector road approaches this island from -X (from the previous island), so the
            // sign should read correctly to someone arriving from that direction.
            signGO.transform.LookAt(signPos + Vector3.left * 10f);
            signGO.transform.Rotate(0f, 180f, 0f); // TextMesh's face is on the opposite side of its forward axis
        }

        static void BuildConnectorRoad(WorldIslandResult from, WorldIslandResult to, Transform parent) {
            float fromX = from.worldCenter.x + from.layout.bounds.maxX + 5f;
            float toX = to.worldCenter.x + to.layout.bounds.minX - 5f;
            if (toX <= fromX) return; // islands somehow overlap/touch - skip rather than build a backwards road

            RoadNetwork.BuildConnector(new Vector3(fromX, 0f, 0f), new Vector3(toX, 0f, 0f), parent);
        }
    }
}
