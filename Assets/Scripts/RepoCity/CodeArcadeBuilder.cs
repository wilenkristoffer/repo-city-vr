using System.Collections.Generic;
using UnityEngine;

namespace RepoCity {

public static class CodeArcadeBuilder {
    const float KioskSpacing = 4f;
    const float UnityPlaneDefaultSize = 10f; // CreatePrimitive(Plane) is 10x10 world units at scale 1

    public static GameObject Build(Vector3 position) {
        // Root is kept at the identity transform until every child below is built and
        // positioned/rotated with local-space calls, then moved into place at the end -
        // this avoids the world-space Transform.position/rotation footgun on its children.
        GameObject root = new GameObject("ProgrammingCentral");

        List<CodeChallenge> challenges = CodeChallenges.All();
        float rowWidth = challenges.Count > 1 ? (challenges.Count - 1) * KioskSpacing : 0f;
        float groundWidth = Mathf.Max(20f, rowWidth + 10f);
        float groundDepth = 20f;
        float kioskZ = 8f;

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localPosition = new Vector3(0f, 0f, kioskZ);
        ground.transform.localScale = new Vector3(groundWidth / UnityPlaneDefaultSize, 1f, groundDepth / UnityPlaneDefaultSize);

        BuildSign(root.transform);

        for (int i = 0; i < challenges.Count; i++) {
            float x = challenges.Count > 1 ? -rowWidth / 2f + i * KioskSpacing : 0f;
            BuildKiosk(root.transform, challenges[i], new Vector3(x, 0f, kioskZ));
        }

        root.transform.position = position;
        return root;
    }

    static void BuildSign(Transform parent) {
        GameObject signGO = new GameObject("Sign");
        signGO.transform.SetParent(parent, false);
        Vector3 signPos = new Vector3(0f, 5f, 2f);
        signGO.transform.localPosition = signPos;

        TextMesh textMesh = signGO.AddComponent<TextMesh>();
        textMesh.text = "PROGRAMMING CENTRAL";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = 64;
        textMesh.characterSize = 0.4f;
        textMesh.color = Color.white;

        // Player approaches from -Z, so aim the sign's forward at a point further toward -Z.
        signGO.transform.LookAt(signPos + Vector3.back * 10f);
        signGO.transform.Rotate(0f, 180f, 0f); // TextMesh's face is on the opposite side of its forward axis
    }

    static void BuildKiosk(Transform parent, CodeChallenge challenge, Vector3 basePos) {
        GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        desk.name = "Desk_" + challenge.id;
        desk.transform.SetParent(parent, false);
        desk.transform.localPosition = new Vector3(basePos.x, 0.45f, basePos.z);
        desk.transform.localScale = new Vector3(1.2f, 0.9f, 0.7f);

        GameObject mount = new GameObject("Kiosk_" + challenge.id);
        mount.transform.SetParent(parent, false);
        Vector3 mountPos = new Vector3(basePos.x, 1.5f, basePos.z + 0.6f);
        mount.transform.localPosition = mountPos;

        // Same approach-direction assumption as the sign: readable from -Z.
        mount.transform.LookAt(mountPos + Vector3.back * 10f);
        mount.transform.Rotate(0f, 180f, 0f);

        CodeKiosk kiosk = mount.AddComponent<CodeKiosk>();
        kiosk.Initialize(challenge);
    }
}
}
