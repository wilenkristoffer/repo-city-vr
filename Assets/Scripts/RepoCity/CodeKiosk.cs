using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace RepoCity {

public class CodeKiosk : MonoBehaviour {
    public Canvas canvas;

    CodeChallenge challenge;
    InputField codeInput;
    Text resultsText;

    const int MaxDiffLen = 60;

    public void Initialize(CodeChallenge challenge) {
        this.challenge = challenge;

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject canvasGO = new GameObject("CodeCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(520f, 680f);
        // 0.0025 m per canvas unit: 1.3 m x 1.7 m, 12pt text ~3 cm. At 0.01 the panel was 5.2 x 6.8 m,
        // overlapped neighbouring kiosks and its results area hung ~2 m below the ground.
        canvasGO.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Text titleText = CreateText(canvasGO.transform, "Title", font, 20, FontStyle.Bold, TextAnchor.UpperLeft);
        SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -42f), new Vector2(-10f, -6f));
        titleText.text = challenge.title;

        Text promptText = CreateText(canvasGO.transform, "Prompt", font, 12, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(promptText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -130f), new Vector2(-10f, -44f));
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
        promptText.text = challenge.prompt;

        GameObject inputBgGO = new GameObject("CodeInputField");
        inputBgGO.transform.SetParent(canvasGO.transform, false);
        Image inputBg = inputBgGO.AddComponent<Image>();
        inputBg.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        RectTransform inputBgRect = inputBgGO.GetComponent<RectTransform>();
        SetRect(inputBgRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -400f), new Vector2(-10f, -136f));

        Text inputDisplayText = CreateText(inputBgGO.transform, "Text", font, 12, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(inputDisplayText.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        inputDisplayText.color = Color.black;
        inputDisplayText.horizontalOverflow = HorizontalWrapMode.Wrap;
        inputDisplayText.verticalOverflow = VerticalWrapMode.Overflow;

        codeInput = inputBgGO.AddComponent<InputField>();
        codeInput.targetGraphic = inputBg;
        codeInput.textComponent = inputDisplayText;
        codeInput.lineType = InputField.LineType.MultiLineNewline;
        codeInput.text = challenge.starterCode;

        GameObject buttonGO = new GameObject("RunButton");
        buttonGO.transform.SetParent(canvasGO.transform, false);
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.25f, 0.55f, 0.3f, 1f);
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(10f, -410f);
        buttonRect.sizeDelta = new Vector2(140f, 36f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => AudioManager.PlayUi("ui_click"));
        button.onClick.AddListener(RunTests);

        Text buttonLabel = CreateText(buttonGO.transform, "Label", font, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        buttonLabel.text = "Run";

        resultsText = CreateText(canvasGO.transform, "Results", font, 12, FontStyle.Normal, TextAnchor.UpperLeft);
        SetRect(resultsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 6f), new Vector2(-10f, -458f));
        // Shrink long test output to fit instead of spilling out below the panel.
        resultsText.verticalOverflow = VerticalWrapMode.Truncate;
        resultsText.resizeTextForBestFit = true;
        resultsText.resizeTextMinSize = 6;
        resultsText.resizeTextMaxSize = 12;
        resultsText.text = "Press Run to test your code.";
    }

    void RunTests() {
        StringBuilder sb = new StringBuilder();
        int passed = 0;
        List<CodeTestCase> tests = challenge.tests;

        for (int i = 0; i < tests.Count; i++) {
            CodeTestCase test = tests[i];
            string source = test.setupCode + "\n" + codeInput.text;
            MiniLangResult result = MiniLang.Run(source);

            if (!result.success) {
                string displayMessage = AdjustReportedLine(result.errorMessage, CountLines(test.setupCode));
                sb.Append("Test ").Append(i + 1).Append(": FAIL - ").Append(displayMessage).Append("\n");
                continue;
            }

            string got = string.Join("\n", result.stdoutLines).Trim();
            string expected = (test.expectedOutput ?? "").Trim();

            if (got == expected) {
                passed++;
                sb.Append("Test ").Append(i + 1).Append(": PASS\n");
            } else {
                sb.Append("Test ").Append(i + 1).Append(": FAIL (got \"").Append(Truncate(got))
                    .Append("\" expected \"").Append(Truncate(expected)).Append("\")\n");
            }
        }

        sb.Append("\n").Append(passed).Append("/").Append(tests.Count).Append(" tests passed");
        resultsText.text = sb.ToString();

        if (passed == tests.Count && tests.Count > 0) {
            AudioManager.PlayUi("challenge_complete");
            AchievementTracker.NotifyChallengeCompleted(challenge.id);
        } else {
            AudioManager.PlayUi("test_fail", 0.6f);
        }
    }

    static string Truncate(string s) {
        if (s.Length <= MaxDiffLen) return s;
        return s.Substring(0, MaxDiffLen) + "...";
    }

    // test.setupCode is run concatenated with the player's InputField text, so a MiniLang
    // error's reported line number counts setupCode's lines too. The player never sees
    // setupCode in the InputField, so re-express the line number relative to their own code.
    static string AdjustReportedLine(string errorMessage, int setupLineCount) {
        if (string.IsNullOrEmpty(errorMessage)) return errorMessage;

        int len = errorMessage.Length;
        int i = 0;
        while (i < len && char.IsWhiteSpace(errorMessage[i])) i++;
        int prefixEnd = i;

        if (i + 4 > len) return errorMessage;
        if (char.ToLowerInvariant(errorMessage[i]) != 'l' ||
            char.ToLowerInvariant(errorMessage[i + 1]) != 'i' ||
            char.ToLowerInvariant(errorMessage[i + 2]) != 'n' ||
            char.ToLowerInvariant(errorMessage[i + 3]) != 'e') {
            return errorMessage;
        }
        i += 4;

        int wsStart = i;
        while (i < len && char.IsWhiteSpace(errorMessage[i])) i++;
        if (i == wsStart) return errorMessage;

        int numStart = i;
        while (i < len && char.IsDigit(errorMessage[i])) i++;
        if (i == numStart) return errorMessage;
        int numEnd = i;

        if (numEnd >= len || errorMessage[numEnd] != ':') return errorMessage;

        int reportedLine;
        if (!int.TryParse(errorMessage.Substring(numStart, numEnd - numStart), out reportedLine)) {
            return errorMessage;
        }

        int adjusted = reportedLine - setupLineCount;
        if (adjusted < 1) adjusted = 1;

        return errorMessage.Substring(0, prefixEnd) + "Line " + adjusted + errorMessage.Substring(numEnd);
    }

    static int CountLines(string s) {
        if (string.IsNullOrEmpty(s)) return 1;
        int count = 1;
        for (int i = 0; i < s.Length; i++) {
            if (s[i] == '\n') count++;
        }
        return count;
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
