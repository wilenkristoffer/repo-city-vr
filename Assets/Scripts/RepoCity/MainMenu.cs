using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RepoCity {

    // Entry point for Assets/Scenes/MainMenu.unity: add to one empty 'Bootstrapper'
    // GameObject. Builds a world-space menu canvas in front of the camera since
    // screen-space-overlay canvases don't render inside a VR headset.
    public class MainMenu : MonoBehaviour {

        void Start() {
            AudioManager.EnsureInitialized();
            GameSettings.EnsureLoaded();
            AudioManager.StopAmbience();
            AudioManager.PlayMusic("menu_jingle", false);

            if (Camera.main == null) {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
            }
            HeadTracker.EnsureOn(Camera.main);

            if (FindObjectOfType<EventSystem>() == null) {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            // Without these the controllers can't press Play — StandaloneInputModule only
            // handles mouse/keyboard. Parented at the scene root, same tracking space as the camera.
            GameObject leftHandPointerGO = new GameObject("LeftHandPointer");
            leftHandPointerGO.AddComponent<VrUiPointer>().hand = UnityEngine.XR.XRNode.LeftHand;
            GameObject rightHandPointerGO = new GameObject("RightHandPointer");
            rightHandPointerGO.AddComponent<VrUiPointer>().hand = UnityEngine.XR.XRNode.RightHand;
            HandVisual.CreateFor(null, UnityEngine.XR.XRNode.LeftHand);
            HandVisual.CreateFor(null, UnityEngine.XR.XRNode.RightHand);

            BuildCanvas();
        }

        Transform menuRoot;
        bool placed;
        bool recenterHeldPrev;

        void Update() {
            if (menuRoot == null) return;

            // On the first frames the headset hasn't reported a pose yet, so the camera sits at
            // the origin facing an arbitrary direction — placing the menu then put it on the floor
            // or behind the player. Wait for a real head pose, and allow recentering later.
            bool recenterHeld = Input.GetKey(KeyCode.R) || GripHeld(UnityEngine.XR.XRNode.LeftHand) || GripHeld(UnityEngine.XR.XRNode.RightHand);
            bool recenterPressed = recenterHeld && !recenterHeldPrev;
            recenterHeldPrev = recenterHeld;

            if (recenterPressed) {
                // The camera picks up the new offset on its next pose update; place the menu after.
                HeadTracker.Recenter();
                placed = false;
                return;
            }
            if (!placed) {
                Vector3 headPos;
                Quaternion headRot;
                if (TryGetHeadPose(out headPos, out headRot)) {
                    PlaceMenu(headPos, headRot);
                    placed = true;
                }
            }
        }

        bool GripHeld(UnityEngine.XR.XRNode node) {
            UnityEngine.XR.InputDevice device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
            bool held = false;
            return device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out held) && held;
        }

        bool TryGetHeadPose(out Vector3 pos, out Quaternion rot) {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            Camera cam = Camera.main;
            if (cam == null) return false;
            // In VR, wait until HeadTracker has moved the camera off the origin (first tracked
            // frame); on desktop the camera's own transform is the head.
            if (UnityEngine.XR.XRSettings.isDeviceActive && cam.transform.localPosition == Vector3.zero) return false;
            pos = cam.transform.position;
            rot = cam.transform.rotation;
            return true;
        }

        // Head pose is in tracking space; the camera and hand pointers sit at the scene root, so
        // tracking space == world space here.
        void PlaceMenu(Vector3 headPos, Quaternion headRot) {
            Vector3 flatForward = headRot * Vector3.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;
            flatForward.Normalize();
            menuRoot.position = headPos + flatForward * 2.5f;
            menuRoot.rotation = Quaternion.LookRotation(flatForward);
            Debug.Log("[MainMenu] Menu placed at " + menuRoot.position + " for head at " + headPos);
        }

        void BuildCanvas() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            menuRoot = new GameObject("MenuRoot").transform;
            menuRoot.SetParent(transform, false);

            GameObject canvasGO = new GameObject("MenuCanvas");
            canvasGO.transform.SetParent(menuRoot, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 320f);
            canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            // World-space UI canvases read correctly from their LOCAL -Z side, so the canvas keeps
            // identity under MenuRoot, whose +Z PlaceMenu points away from the player.

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.14f, 0.92f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Text title = CreateText(canvasGO.transform, "Title", font, 40, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -70f), new Vector2(-8f, -10f));
            title.text = "REPO CITY";

            Button playButton = CreateButton(canvasGO.transform, font, "PlayButton", "Play", new Vector2(0f, -90f), () => {
                UnityEngine.SceneManagement.SceneManager.LoadScene("RepoWorld");
            });
            // Keyboard fallback: Enter on the desktop presses Play even if the pointers misbehave.
            if (EventSystem.current != null) {
                EventSystem.current.SetSelectedGameObject(playButton.gameObject);
            }

            CreateButton(canvasGO.transform, font, "SettingsButton", "Settings", new Vector2(0f, -160f), () => {
                // Hide the main menu while settings are open: both canvases share one plane, and the
                // laser hit the (larger) menu canvas first, so Back never received the click.
                canvasGO.SetActive(false);
                GameObject settingsGO = new GameObject("SettingsPanel");
                settingsGO.transform.SetParent(menuRoot, false);
                settingsGO.AddComponent<SettingsPanel>().Initialize(() => {
                    Destroy(settingsGO);
                    canvasGO.SetActive(true);
                });
            });

            CreateButton(canvasGO.transform, font, "QuitButton", "Quit", new Vector2(0f, -230f), () => {
                Application.Quit();
            });
        }

        Button CreateButton(Transform parent, Font font, string name, string label, Vector2 centerOffset, System.Action onClick) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.22f, 0.4f, 1f);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(220f, 52f);
            rect.anchoredPosition = centerOffset;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => AudioManager.PlayUi("ui_click"));
            button.onClick.AddListener(() => onClick());

            Text text = CreateText(go.transform, "Label", font, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.text = label;
            return button;
        }

        Text CreateText(Transform parent, string name, Font font, int fontSize, FontStyle style, TextAnchor anchor) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
