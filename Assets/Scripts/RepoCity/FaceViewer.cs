using UnityEngine;

namespace RepoCity {

    // Keeps world-space text readable from any side: turns around the vertical axis only, so text
    // stays upright instead of tilting when the player stands underneath. A fixed facing read
    // mirrored from behind (e.g. "MOST ACTIVE FILE" seen from across the city).
    public class FaceViewer : MonoBehaviour {
        Camera cam;

        void LateUpdate() {
            if (cam == null) {
                cam = Camera.main;
                if (cam == null) return;
            }
            Vector3 away = transform.position - cam.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) return;
            // TextMesh's readable face is on the opposite side of its forward axis, so forward
            // points away from the viewer.
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
