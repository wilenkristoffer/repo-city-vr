using UnityEngine;
using UnityEngine.XR;

namespace RepoCity
{
    public class Locomotion : MonoBehaviour
    {
        public System.Action<FileEntry> OnInspect;
        public System.Action<NpcInfo> OnInteractNpc;

        public float moveSpeed = 3f;
        public float lookSensitivity = 3f;
        public float teleportMaxRange = 30f;
        public float teleportSpeed = 10f;
        public float teleportGravity = -9.8f;
        public float inspectMaxDistance = 60f;

        Camera rigCamera;
        LineRenderer teleportLine;
        GameObject teleportMarker;

        bool leftTriggerHeldPrev;
        bool rightTriggerHeldPrev;
        bool leftGripHeldPrev;
        bool rightGripHeldPrev;

        float desktopPitch;
        bool dragging;

        void Start()
        {
            rigCamera = GetComponentInChildren<Camera>();

            teleportLine = GetComponent<LineRenderer>();
            if (teleportLine == null)
            {
                teleportLine = gameObject.AddComponent<LineRenderer>();
            }
            teleportLine.positionCount = 2;
            teleportLine.startWidth = 0.03f;
            teleportLine.endWidth = 0.03f;
            teleportLine.enabled = false;

            teleportMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teleportMarker.name = "TeleportMarker";
            Collider markerCollider = teleportMarker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }
            teleportMarker.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            teleportMarker.SetActive(false);
        }

        void Update()
        {
            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool vrActive = leftDevice.isValid || rightDevice.isValid;

            if (!vrActive)
            {
                UpdateDesktopFallback();
                HideTeleportVisuals();
                return;
            }

            bool anyAiming = false;
            anyAiming |= UpdateHandTeleport(leftDevice, ref leftTriggerHeldPrev, ref leftHasValidLanding, ref leftLastValidLanding);
            anyAiming |= UpdateHandTeleport(rightDevice, ref rightTriggerHeldPrev, ref rightHasValidLanding, ref rightLastValidLanding);

            if (!anyAiming)
            {
                HideTeleportVisuals();
            }

            UpdateHandInspect(leftDevice, ref leftGripHeldPrev);
            UpdateHandInspect(rightDevice, ref rightGripHeldPrev);
        }

        void HideTeleportVisuals()
        {
            teleportLine.enabled = false;
            teleportMarker.SetActive(false);
        }

        Vector3 leftLastValidLanding;
        bool leftHasValidLanding;
        Vector3 rightLastValidLanding;
        bool rightHasValidLanding;

        // Returns true while this hand is actively aiming (trigger held) this frame.
        bool UpdateHandTeleport(InputDevice device, ref bool triggerHeldPrev, ref bool hasValidLanding, ref Vector3 lastValidLanding)
        {
            if (!device.isValid)
            {
                triggerHeldPrev = false;
                hasValidLanding = false;
                return false;
            }

            bool triggerHeld = false;
            device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerHeld);

            if (triggerHeld)
            {
                Vector3 localPos;
                Quaternion localRot;
                device.TryGetFeatureValue(CommonUsages.devicePosition, out localPos);
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out localRot);

                Vector3 worldOrigin = transform.TransformPoint(localPos);
                Vector3 worldDirection = transform.TransformDirection(localRot * Vector3.forward);

                Vector3 landing;
                bool found = SolveTeleportArc(worldOrigin, worldDirection, out landing);

                if (found)
                {
                    teleportLine.enabled = true;
                    teleportLine.SetPosition(0, worldOrigin);
                    teleportLine.SetPosition(1, landing);
                    teleportMarker.SetActive(true);
                    teleportMarker.transform.position = landing;

                    hasValidLanding = true;
                    lastValidLanding = landing;
                }
                else
                {
                    teleportLine.enabled = false;
                    teleportMarker.SetActive(false);
                    hasValidLanding = false;
                }

                triggerHeldPrev = true;
                return true;
            }

            bool releasedThisFrame = triggerHeldPrev;
            triggerHeldPrev = false;

            if (releasedThisFrame && hasValidLanding)
            {
                Vector3 newPos = transform.position;
                newPos.x = lastValidLanding.x;
                newPos.z = lastValidLanding.z;
                transform.position = newPos;
                hasValidLanding = false;
            }

            return false;
        }

        bool SolveTeleportArc(Vector3 origin, Vector3 direction, out Vector3 landing)
        {
            landing = Vector3.zero;

            Vector3 velocity = direction * teleportSpeed;

            float dt = 0.02f;
            float maxTime = 5f;
            Vector3 prevPos = origin;

            for (float t = dt; t <= maxTime; t += dt)
            {
                Vector3 pos = origin + velocity * t + 0.5f * new Vector3(0f, teleportGravity, 0f) * t * t;

                if (pos.y <= 0f && prevPos.y > 0f)
                {
                    float denom = (prevPos.y - pos.y);
                    float frac = denom != 0f ? prevPos.y / denom : 0f;
                    Vector3 hitPoint = Vector3.Lerp(prevPos, pos, frac);
                    hitPoint.y = 0f;

                    float dist = Vector3.Distance(new Vector3(origin.x, 0f, origin.z), new Vector3(hitPoint.x, 0f, hitPoint.z));
                    if (dist <= teleportMaxRange)
                    {
                        landing = hitPoint;
                        return true;
                    }
                    return false;
                }

                prevPos = pos;
            }

            return false;
        }

        void UpdateHandInspect(InputDevice device, ref bool gripHeldPrev)
        {
            if (!device.isValid)
            {
                gripHeldPrev = false;
                return;
            }

            bool gripHeld = false;
            device.TryGetFeatureValue(CommonUsages.gripButton, out gripHeld);

            bool freshPress = gripHeld && !gripHeldPrev;
            gripHeldPrev = gripHeld;

            if (!freshPress)
            {
                return;
            }

            Vector3 localPos;
            Quaternion localRot;
            device.TryGetFeatureValue(CommonUsages.devicePosition, out localPos);
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out localRot);

            Vector3 worldOrigin = transform.TransformPoint(localPos);
            Vector3 worldDirection = transform.TransformDirection(localRot * Vector3.forward);

            RaycastHit hit;
            if (Physics.Raycast(worldOrigin, worldDirection, out hit, inspectMaxDistance))
            {
                BuildingInfo buildingInfo = hit.collider.GetComponentInParent<BuildingInfo>();
                if (buildingInfo != null)
                {
                    OnInspect?.Invoke(buildingInfo.file);
                    return;
                }

                NpcInfo npcInfo = hit.collider.GetComponentInParent<NpcInfo>();
                if (npcInfo != null)
                {
                    OnInteractNpc?.Invoke(npcInfo);
                    return;
                }
            }

            OnInspect?.Invoke(null);
        }

        void UpdateDesktopFallback()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;

            Vector3 move = transform.forward * vertical + transform.right * horizontal;
            move.y = 0f;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }
            transform.position += move * moveSpeed * Time.deltaTime;

            if (Input.GetMouseButtonDown(0))
            {
                dragging = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
            }

            if (dragging)
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

                transform.Rotate(Vector3.up, mouseX, Space.World);

                desktopPitch -= mouseY;
                desktopPitch = Mathf.Clamp(desktopPitch, -89f, 89f);

                if (rigCamera != null)
                {
                    Vector3 camEuler = rigCamera.transform.localEulerAngles;
                    rigCamera.transform.localEulerAngles = new Vector3(desktopPitch, camEuler.y, camEuler.z);
                }
            }
        }
    }
}
