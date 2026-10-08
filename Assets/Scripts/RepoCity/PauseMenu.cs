using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RepoCity {

public class PauseMenu : MonoBehaviour {
    const float CanvasDistance = 2.2f;
    const float CanvasWidth = 340f;
    const float CanvasHeight = 440f;
    const float CanvasScale = 0.0035f;

    bool paused;

    GameObject canvasGO;
    Canvas canvas;
    SettingsPanel activeSettingsPanel;

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (paused) Resume();
            else Pause();
        }
    }

    void Pause() {
        paused = true;
        Time.timeScale = 0f;
        EnsureBuilt();
        PositionInFrontOfCamera();
        canvasGO.SetActive(true);
    }

    void Resume() {
        paused = false;
        Time.timeScale = 1f;
        if (canvasGO != null) canvasGO.SetActive(false);
    }

    void EnsureBuilt() {
        if (canvasGO != null) return;

        if (FindObjectOfType<EventSystem>() == null) {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        canvasGO = new GameObject("PauseMenuCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        canvasGO.transform.localScale = new Vector3(CanvasScale, CanvasScale, CanvasScale);

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Text titleText = CreateText(canvasGO.transform, "Title", font, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -70f), new Vector2(-10f, -10f));
        titleText.text = "PAUSED";

        CreateButton(canvasGO.transform, font, "ResumeButton", "Resume", -110f, new Color(0.25f, 0.55f, 0.3f, 1f), () => {
            Resume();
        });

        CreateButton(canvasGO.transform, font, "SettingsButton", "Settings", -186f, new Color(0.25f, 0.4f, 0.6f, 1f), () => {
            OpenSettings();
        });

        CreateButton(canvasGO.transform, font, "QuitToMenuButton", "Quit to Main Menu", -262f, new Color(0.55f, 0.45f, 0.2f, 1f), () => {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });

        CreateButton(canvasGO.transform, font, "QuitAppButton", "Quit App", -338f, new Color(0.55f, 0.2f, 0.2f, 1f), () => {
            Application.Quit();
        });

        canvasGO.SetActive(false);
    }

    void OpenSettings() {
        canvasGO.SetActive(false);
        activeSettingsPanel = gameObject.AddComponent<SettingsPanel>();
        activeSettingsPanel.Initialize(() => {
            Destroy(activeSettingsPanel);
            activeSettingsPanel = null;
            if (paused) canvasGO.SetActive(true);
        });
    }

    void PositionInFrontOfCamera() {
        Camera cam = Camera.main;
        if (cam == null) return;
        Transform camT = cam.transform;

        canvas.worldCamera = cam;
        canvasGO.transform.position = camT.position + camT.forward * CanvasDistance;
        canvasGO.transform.LookAt(camT.position);
        canvasGO.transform.Rotate(0f, 180f, 0f); // canvas is only readable from its local -Z side
    }

    void CreateButton(Transform parent, Font font, string name, string label, float topOffset, Color color, UnityEngine.Events.UnityAction onClick) {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent, false);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = color;
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        SetRect(buttonRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, topOffset - 56f), new Vector2(-20f, topOffset));

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => AudioManager.PlayUi("ui_click"));
        button.onClick.AddListener(onClick);

        Text buttonLabel = CreateText(buttonGO.transform, "Label", font, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        buttonLabel.text = label;
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
