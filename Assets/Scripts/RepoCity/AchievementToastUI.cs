using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RepoCity {

    public class AchievementToastUI : MonoBehaviour {
        const float visibleSeconds = 3f;

        GameObject canvasGO;
        Text toastText;
        Coroutine showingRoutine;
        Action<string> onToastHandler;

        void Awake() {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            canvasGO = new GameObject("AchievementToastCanvas");
            canvasGO.transform.SetParent(transform, false);
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(260f, 60f);
            canvasGO.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);
            canvasGO.transform.localPosition = Vector3.zero;
            canvasGO.transform.localRotation = Quaternion.identity;

            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.10f, 0.22f, 0.88f);
            RectTransform bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject textGO = new GameObject("ToastText");
            textGO.transform.SetParent(canvasGO.transform, false);
            toastText = textGO.AddComponent<Text>();
            toastText.font = font;
            toastText.fontSize = 13;
            toastText.fontStyle = FontStyle.Bold;
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.color = Color.white;
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform textRect = toastText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);

            onToastHandler = HandleToast;

            canvasGO.SetActive(false);
        }

        void OnEnable() {
            AchievementTracker.OnToast += onToastHandler;
        }

        void OnDisable() {
            AchievementTracker.OnToast -= onToastHandler;
        }

        void HandleToast(string message) {
            if (showingRoutine != null) {
                StopCoroutine(showingRoutine);
                showingRoutine = null;
            }

            toastText.text = message;
            canvasGO.SetActive(true);
            AudioManager.PlayUi("achievement");

            showingRoutine = StartCoroutine(HideAfterDelay());
        }

        IEnumerator HideAfterDelay() {
            yield return new WaitForSeconds(visibleSeconds);
            canvasGO.SetActive(false);
            showingRoutine = null;
        }
    }
}
