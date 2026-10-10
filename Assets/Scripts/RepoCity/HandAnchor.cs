using UnityEngine;
using UnityEngine.XR;

namespace RepoCity {

    // Follows one controller, in the same rig-local space as the HeadTracker camera. Put it under the
    // player rig and parent things to it (e.g. the wrist minimap). Children are hidden while the
    // controller isn't tracked, and optionally toggled by clicking this controller's trackpad.
    public class HandAnchor : MonoBehaviour {

        public XRNode hand = XRNode.LeftHand;
        public bool toggleWithTrackpadClick;

        bool shown = true;
        bool clickHeldPrev;

        void OnEnable() {
            Application.onBeforeRender += ApplyPose;
        }

        void OnDisable() {
            Application.onBeforeRender -= ApplyPose;
        }

        void Update() {
            if (toggleWithTrackpadClick) {
                // The menu button above the trackpad is taken by PauseMenu.
                bool clickHeld = TrackpadClickHeld(hand);
                if (clickHeld && !clickHeldPrev) {
                    shown = !shown;
                }
                clickHeldPrev = clickHeld;
            }
            ApplyPose();
        }

        void ApplyPose() {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            Vector3 pos;
            Quaternion rot;
            bool tracked = device.isValid
                && device.TryGetFeatureValue(CommonUsages.devicePosition, out pos)
                && device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
            SetChildrenActive(tracked && shown);
            if (!tracked) return;

            device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);
            HeadTracker.ToRigLocal(ref pos, ref rot);
            transform.localPosition = pos;
            transform.localRotation = rot;
        }

        void SetChildrenActive(bool active) {
            for (int i = 0; i < transform.childCount; i++) {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.activeSelf != active) child.SetActive(active);
            }
        }

        static bool TrackpadClickHeld(XRNode node) {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            bool held = false;
            return device.isValid && device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out held) && held;
        }
    }
}
