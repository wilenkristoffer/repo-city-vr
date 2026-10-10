using UnityEngine;
using UnityEngine.UI;

namespace RepoCity {

    // One shared world-space bubble for NPC lines and building narration. The fixed HUD panel sits
    // at the spawn point, so once the player walks away replies were invisible; this shows them
    // where the player is looking instead. Only one conversation is active at a time (callers
    // already race-guard with inspectToken), so a single instance is reused.
    public class SpeechBubble : MonoBehaviour {
        const float FallbackHeadHeight = 1.8f;
        // Space between the top of the head and the bubble's bottom edge.
        const float HeadGap = 0.12f;
        const float FrontDistance = 1.6f;
        const float BaseScale = 0.004f;
        // Beyond this distance the bubble grows so text keeps roughly the same apparent size.
        const float ReadableDistance = 3f;
        const float FadeSeconds = 0.4f;

        static SpeechBubble instance;

        Text headingText;
        Text subheadingText;
        Text bodyText;
        CanvasGroup group;

        Transform followTarget;
        float followHeight;
        Vector3 worldPosition;
        float hideAt = float.MaxValue;

        public static void ShowNpc(NpcInfo npc, string line, bool final) {
            SpeechBubble b = Get();
            b.followTarget = npc.transform;
            b.followHeight = HeadTop(npc.transform) + HeadGap;
            b.Show(npc.personaName, npc.personaJob, line, final);
        }

        // Narration floats in front of the player at eye level, fixed in the world (not head-locked,
        // which is uncomfortable in VR).
        public static void ShowNarration(FileEntry file, string text, bool final) {
            SpeechBubble b = Get();
            Camera cam = Camera.main;
            if (cam != null && b.followTarget == null && b.group.alpha > 0f && b.headingText.text == file.name) {
                // Same building updating from "Thinking..." to the real text: keep the bubble where it is.
            } else if (cam != null) {
                // Flatten the gaze; looking straight up/down falls back to the head's up vector.
                Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
                b.worldPosition = cam.transform.position + forward.normalized * FrontDistance + Vector3.down * 0.15f;
            }
            b.followTarget = null;
            b.Show(file.name, file.path, text, final);
        }

        // Height of the model's top above its feet; character models vary, so measure instead of guessing.
        static float HeadTop(Transform npc) {
            float top = 0f;
            foreach (Renderer r in npc.GetComponentsInChildren<Renderer>()) {
                top = Mathf.Max(top, r.bounds.max.y - npc.position.y);
            }
            return top > 0.3f ? top : FallbackHeadHeight;
        }

        public static void Hide() {
            if (instance != null) instance.hideAt = Time.time;
        }

        static SpeechBubble Get() {
            if (instance == null) {
                GameObject go = new GameObject("SpeechBubble");
                instance = go.AddComponent<SpeechBubble>();
                instance.Build();
            }
            return instance;
        }

        void Show(string heading, string subheading, string body, bool final) {
            headingText.text = heading;
            subheadingText.text = subheading;
            bodyText.text = body;
            group.alpha = 1f;
            // Placeholders ("...", "Thinking...") stay until the real text replaces them.
            hideAt = final ? Time.time + 5f + body.Length * 0.05f : float.MaxValue;
            LateUpdate();
        }

        void LateUpdate() {
            if (group.alpha <= 0f) return;

            if (Time.time >= hideAt) {
                group.alpha = Mathf.Max(0f, 1f - (Time.time - hideAt) / FadeSeconds);
                if (group.alpha <= 0f) followTarget = null;
            }

            if (followTarget != null) {
                worldPosition = followTarget.position + Vector3.up * followHeight;
            }

            transform.position = worldPosition;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 away = worldPosition - cam.transform.position;
            if (away.sqrMagnitude < 0.0001f) return;
            // World-space canvases read from their -Z side, so point +Z away from the viewer.
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            float scale = BaseScale * Mathf.Max(1f, away.magnitude / ReadableDistance);
            transform.localScale = new Vector3(scale, scale, scale);
        }

        void Build() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 140f);
            // Pivot at the bottom so the bubble sits above the NPC's head rather than through it.
            rect.pivot = new Vector2(0.5f, 0f);

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.10f, 0.22f, 0.88f);
            bg.raycastTarget = false;
            SetRect(bgGO.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            headingText = CreateText(font, "Heading", 20, FontStyle.Bold);
            SetRect(headingText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -32f), new Vector2(-12f, -6f));

            subheadingText = CreateText(font, "Subheading", 12, FontStyle.Italic);
            subheadingText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            SetRect(subheadingText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -48f), new Vector2(-12f, -32f));

            bodyText = CreateText(font, "Body", 16, FontStyle.Normal);
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(bodyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -52f));
        }

        Text CreateText(Font font, string name, int size, FontStyle style) {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
