using UnityEngine;
using UnityEngine.XR;

namespace RepoCity {

    // Stylised hand built from primitives (no hand models in the project), drawn at a controller.
    // Lives under a HandAnchor, so it uses the controller's grip pose: +Z is the pointing
    // direction (same as the UI/teleport rays), +Y is up/thumb side, and the palm faces the body
    // midline (-X for the right hand, +X for the left), i.e. a handshake pose.
    // Index finger curls with the trigger, the other three with the grip.
    public class HandVisual : MonoBehaviour {

        public XRNode hand;

        Transform[] indexJoints;
        Transform[][] gripFingerJoints;
        Transform thumbJoint;
        float side; // +1 right hand, -1 left: mirrors the model across X
        float triggerCurl;
        float gripCurl;

        // Creates a tracked anchor under `parent` (the rig, or the scene root in the menu) with a hand on it.
        public static void CreateFor(Transform parent, XRNode node) {
            GameObject anchorGO = new GameObject(node == XRNode.LeftHand ? "LeftHandVisual" : "RightHandVisual");
            if (parent != null) anchorGO.transform.SetParent(parent, false);
            anchorGO.AddComponent<HandAnchor>().hand = node;

            GameObject handGO = new GameObject("Hand");
            handGO.transform.SetParent(anchorGO.transform, false);
            HandVisual visual = handGO.AddComponent<HandVisual>();
            visual.hand = node;
            visual.Build();
        }

        void Build() {
            side = hand == XRNode.LeftHand ? -1f : 1f;

            Material skin = new Material(Shader.Find("Standard"));
            skin.color = new Color(0.93f, 0.76f, 0.62f);
            skin.SetFloat("_Glossiness", 0.15f);

            // Back of the hand sits just outside an imaginary 2 cm-radius controller handle.
            float palmX = side * 0.035f;

            Segment(transform, "Palm", new Vector3(palmX, -0.003f, -0.035f), new Vector3(0.028f, 0.085f, 0.095f), skin, true);

            indexJoints = Finger("Index", new Vector3(palmX, 0.03f, 0.012f), 0.042f, 0.034f, 0.0095f, skin);
            gripFingerJoints = new Transform[][] {
                Finger("Middle", new Vector3(palmX, 0.009f, 0.014f), 0.046f, 0.036f, 0.0095f, skin),
                Finger("Ring", new Vector3(palmX, -0.012f, 0.011f), 0.042f, 0.033f, 0.009f, skin),
                Finger("Pinky", new Vector3(palmX, -0.032f, 0.004f), 0.033f, 0.026f, 0.008f, skin),
            };

            // Thumb: from the base of the palm, angled up and forward, slightly toward the palm side.
            thumbJoint = new GameObject("Thumb").transform;
            thumbJoint.SetParent(transform, false);
            thumbJoint.localPosition = new Vector3(palmX - side * 0.01f, 0.035f, -0.055f);
            thumbJoint.localRotation = Quaternion.Euler(-35f, -side * 20f, 0f);
            Segment(thumbJoint, "ThumbProximal", Vector3.zero, new Vector3(0.022f, 0.022f, 0.045f), skin, false);
            Transform thumbDistal = new GameObject("ThumbDistalJoint").transform;
            thumbDistal.SetParent(thumbJoint, false);
            thumbDistal.localPosition = new Vector3(0f, 0f, 0.045f);
            thumbDistal.localRotation = Quaternion.Euler(10f, 0f, 0f);
            Segment(thumbDistal, "ThumbDistal", Vector3.zero, new Vector3(0.02f, 0.02f, 0.032f), skin, false);
        }

        // Two-joint finger along +Z starting at `knuckle`; returns {proximal, distal} joint pivots.
        Transform[] Finger(string name, Vector3 knuckle, float proximalLen, float distalLen, float radius, Material mat) {
            Transform proximal = new GameObject(name + "Proximal").transform;
            proximal.SetParent(transform, false);
            proximal.localPosition = knuckle;
            Segment(proximal, name + "ProximalBone", Vector3.zero, new Vector3(radius * 2f, radius * 2f, proximalLen), mat, false);

            Transform distal = new GameObject(name + "Distal").transform;
            distal.SetParent(proximal, false);
            distal.localPosition = new Vector3(0f, 0f, proximalLen);
            Segment(distal, name + "DistalBone", Vector3.zero, new Vector3(radius * 1.8f, radius * 1.8f, distalLen), mat, false);

            return new Transform[] { proximal, distal };
        }

        // A capsule (or box for the palm) of the given size. Capsules start at `start` and extend
        // along the parent's +Z; colliders are removed so hands never block the inspect raycast.
        static Transform Segment(Transform parent, string name, Vector3 start, Vector3 size, Material mat, bool box) {
            GameObject go = GameObject.CreatePrimitive(box ? PrimitiveType.Cube : PrimitiveType.Capsule);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, false);
            if (box) {
                go.transform.localPosition = start;
                go.transform.localScale = size;
            } else {
                // Unity's capsule is 2 units tall along Y with radius 0.5: rotate Y onto Z.
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localPosition = start + new Vector3(0f, 0f, size.z * 0.5f);
                go.transform.localScale = new Vector3(size.x, size.z * 0.5f, size.y);
            }
            return go.transform;
        }

        void Update() {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            float trigger = 0f;
            float grip = 0f;
            bool gripButton = false;
            if (device.isValid) {
                device.TryGetFeatureValue(CommonUsages.trigger, out trigger);
                device.TryGetFeatureValue(CommonUsages.grip, out grip);
                // Vive grips are buttons; some runtimes only report the bool.
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out gripButton) && gripButton) grip = 1f;
            }
            // Smooth so button-only inputs still animate instead of snapping (unscaled: keeps working while paused).
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
            triggerCurl = Mathf.Lerp(triggerCurl, trigger, k);
            gripCurl = Mathf.Lerp(gripCurl, grip, k);

            // Relaxed hands are slightly curled; curling bends toward the palm (-X for right, +X for left).
            CurlFinger(indexJoints, 0.12f + 0.75f * triggerCurl);
            foreach (Transform[] finger in gripFingerJoints) {
                CurlFinger(finger, 0.18f + 0.75f * gripCurl);
            }
            thumbJoint.localRotation = Quaternion.Euler(-35f + 15f * gripCurl, -side * (20f + 20f * gripCurl), 0f);
        }

        void CurlFinger(Transform[] joints, float curl) {
            joints[0].localRotation = Quaternion.Euler(0f, -side * 80f * curl, 0f);
            joints[1].localRotation = Quaternion.Euler(0f, -side * 95f * curl, 0f);
        }
    }
}
