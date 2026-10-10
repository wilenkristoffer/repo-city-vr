using UnityEngine;
using UnityEngine.XR;

namespace RepoCity {

    // Drives the camera's local pose from the headset. Unity 6 + OpenXR does NOT move the camera
    // on its own without a TrackedPoseDriver (which needs packages this project doesn't have):
    // without this, both eyes render from the camera's fixed transform and the view ignores the
    // head entirely — the "only an empty horizon in every direction" bug. Commit 4cb4243's 180-degree
    // rig rotation only masked this in desktop screenshots.
    //
    // On the first tracked frame it also recenters: wherever the player physically stands and faces
    // in their room, their head starts at the rig's origin facing the rig's +Z, so the bootstrappers'
    // rig placement/rotation means the same thing in VR as on desktop. Anything else that reads raw
    // XR device poses (controllers) must go through ToRigLocal so it lines up with the camera.
    public class HeadTracker : MonoBehaviour {

        static bool originRequested;
        static bool recentered;
        static Quaternion recenterRot = Quaternion.identity;
        static Vector3 recenterPos = Vector3.zero;

        public static void EnsureOn(Camera cam) {
            if (cam != null && cam.GetComponent<HeadTracker>() == null) {
                cam.gameObject.AddComponent<HeadTracker>();
            }
        }

        // Converts a raw tracking-space pose into the rig-local space the camera lives in.
        public static void ToRigLocal(ref Vector3 pos, ref Quaternion rot) {
            pos = recenterRot * pos + recenterPos;
            rot = recenterRot * rot;
        }

        // Re-aims the player: their current head yaw/position becomes the rig's forward/origin.
        public static void Recenter() {
            recentered = false;
        }

        void OnEnable() {
            // Each scene builds a fresh camera/rig, so face the player along that scene's rig.
            recentered = false;
            Application.onBeforeRender += ApplyPose;
        }

        void OnDisable() {
            Application.onBeforeRender -= ApplyPose;
        }

        void Update() {
            ApplyPose();
        }

        // Called again just before rendering so the frame uses the freshest head pose.
        void ApplyPose() {
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!head.isValid) return; // desktop run: leave the bootstrapper's 1.6m eye height alone

            // In XR, MSAA + skybox paints the sky over every mesh and UI canvas (the "only a horizon"
            // bug). Enforce it here too, since the settings menu can switch quality levels at runtime.
            if (QualitySettings.antiAliasing != 0) {
                QualitySettings.antiAliasing = 0;
            }

            if (!originRequested) {
                RequestFloorOrigin();
            }

            Vector3 pos;
            Quaternion rot;
            if (!(head.TryGetFeatureValue(CommonUsages.centerEyePosition, out pos)
                  || head.TryGetFeatureValue(CommonUsages.devicePosition, out pos))) return;
            if (!(head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rot)
                  || head.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))) return;
            // Before the runtime has a pose it reports all zeros; don't recenter on that.
            if (pos == Vector3.zero && rot == Quaternion.identity) return;

            if (!recentered) {
                Vector3 fwd = rot * Vector3.forward;
                fwd.y = 0f;
                float yaw = fwd.sqrMagnitude > 0.0001f ? Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg : 0f;
                recenterRot = Quaternion.Euler(0f, -yaw, 0f);
                Vector3 rotated = recenterRot * pos;
                recenterPos = new Vector3(-rotated.x, 0f, -rotated.z); // keep real eye height
                recentered = true;
                Debug.Log("[HeadTracker] Recentered: head " + pos.ToString("F2") + " yaw " + yaw.ToString("F0"));
            }

            ToRigLocal(ref pos, ref rot);
            transform.localPosition = pos;
            transform.localRotation = rot;
        }

        // Floor origin makes head height your real height (not 0 at the headset), so rigs placed
        // at ground level (y=0) put your eyes at a natural height.
        static void RequestFloorOrigin() {
            var subsystems = new System.Collections.Generic.List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (XRInputSubsystem s in subsystems) {
                if ((s.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0) {
                    s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                    originRequested = true;
                }
            }
        }
    }
}
