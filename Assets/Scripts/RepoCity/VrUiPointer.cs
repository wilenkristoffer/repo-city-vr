using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RepoCity
{
    // Hand-rolled VR laser pointer for legacy UGUI, since XR Interaction Toolkit is not installed
    // in this project. One of these per hand; set `hand` to which controller it follows.
    public class VrUiPointer : MonoBehaviour
    {
        public UnityEngine.XR.XRNode hand;
        public float maxDistance = 15f;

        LineRenderer line;
        GraphicRaycaster[] raycasters = new GraphicRaycaster[0];
        float rescanTimer;
        const float RescanInterval = 1f;

        bool triggerHeldPrev;
        Slider draggingSlider;

        static bool leftOverUi;
        static bool rightOverUi;

        // True while that hand's laser is on a UI panel, so trigger presses there are UI clicks
        // and Locomotion shouldn't also start a teleport.
        public static bool IsOverUi(UnityEngine.XR.XRNode node)
        {
            return node == UnityEngine.XR.XRNode.LeftHand ? leftOverUi : rightOverUi;
        }

        static void SetOverUi(UnityEngine.XR.XRNode node, bool over)
        {
            if (node == UnityEngine.XR.XRNode.LeftHand) leftOverUi = over;
            else rightOverUi = over;
        }

        void OnDisable()
        {
            SetOverUi(hand, false);
        }

        void Start()
        {
            line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
            }
            line.positionCount = 2;
            line.startWidth = 0.004f;
            line.endWidth = 0.004f;
            // Without a material LineRenderers draw error-magenta. UI/Default is always in the build.
            line.material = new Material(Shader.Find("UI/Default"));
            line.startColor = line.endColor = new Color(1f, 1f, 1f, 0.8f);
            line.enabled = false;

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            RescanRaycasters();
        }

        void Update()
        {
            rescanTimer -= Time.unscaledDeltaTime; // unscaled: must keep finding the pause menu while timeScale is 0
            if (rescanTimer <= 0f)
            {
                RescanRaycasters();
            }

            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            if (!device.isValid)
            {
                SetOverUi(hand, false);
                line.enabled = false;
                triggerHeldPrev = false;
                return;
            }

            Vector3 localPos;
            Quaternion localRot;
            device.TryGetFeatureValue(CommonUsages.devicePosition, out localPos);
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out localRot);
            HeadTracker.ToRigLocal(ref localPos, ref localRot);

            Vector3 worldOrigin = transform.TransformPoint(localPos);
            Vector3 worldDirection = transform.TransformDirection(localRot * Vector3.forward);

            GameObject hitObject;
            Vector3 hitPoint;
            bool hit = FindClosestUiHit(worldOrigin, worldDirection, out hitObject, out hitPoint);

            // Only draw the laser when it's actually on a panel; otherwise it clutters the view and
            // competes with Locomotion's teleport arc, which shares the trigger.
            SetOverUi(hand, hit);
            line.enabled = hit;
            line.SetPosition(0, worldOrigin);
            line.SetPosition(1, hit ? hitPoint : worldOrigin + worldDirection * maxDistance);

            bool triggerHeld = false;
            device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerHeld);
            bool freshPress = triggerHeld && !triggerHeldPrev;
            triggerHeldPrev = triggerHeld;

            // Sliders need dragging, which plain click events can't do: while the trigger is held on
            // a slider, set its value straight from where the laser hits along it.
            if (!triggerHeld)
            {
                draggingSlider = null;
            }
            else if (hit && (freshPress || draggingSlider != null))
            {
                Slider slider = freshPress ? hitObject.GetComponentInParent<Slider>() : draggingSlider;
                if (slider != null && slider.interactable)
                {
                    draggingSlider = slider;
                    RectTransform sliderRect = (RectTransform)slider.transform;
                    Vector3 local = sliderRect.InverseTransformPoint(hitPoint);
                    float t = Mathf.InverseLerp(sliderRect.rect.xMin, sliderRect.rect.xMax, local.x);
                    slider.normalizedValue = t;
                    return;
                }
            }

            // Dropdowns open a popup list the laser can't reliably reach in VR; step through the
            // options on each click instead.
            if (freshPress && hit)
            {
                Dropdown dropdown = hitObject.GetComponentInParent<Dropdown>();
                if (dropdown != null && dropdown.interactable && dropdown.options.Count > 0)
                {
                    dropdown.value = (dropdown.value + 1) % dropdown.options.Count;
                    return;
                }
            }

            if (freshPress && hit)
            {
                GameObject clickTarget = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObject);
                if (clickTarget != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    ExecuteEvents.Execute(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);
                }
            }
        }

        void RescanRaycasters()
        {
            raycasters = FindObjectsOfType<GraphicRaycaster>();
            rescanTimer = RescanInterval;
        }

        // Geometric ray-vs-plane test against each world-space canvas's RectTransform; keeps the
        // closest valid hit among all known raycasters.
        bool FindClosestUiHit(Vector3 origin, Vector3 direction, out GameObject hitObject, out Vector3 hitPoint)
        {
            hitObject = null;
            hitPoint = Vector3.zero;
            float closestDistance = maxDistance;

            for (int i = 0; i < raycasters.Length; i++)
            {
                GraphicRaycaster raycaster = raycasters[i];
                if (raycaster == null) continue;

                Canvas canvas = raycaster.GetComponent<Canvas>();
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) continue;
                if (!canvas.gameObject.activeInHierarchy) continue;

                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                Vector3 planeNormal = canvasRect.forward;
                Vector3 planePoint = canvasRect.position;

                float denom = Vector3.Dot(planeNormal, direction);
                if (Mathf.Abs(denom) < 0.0001f) continue;

                float t = Vector3.Dot(planePoint - origin, planeNormal) / denom;
                if (t <= 0f || t >= closestDistance) continue;

                Vector3 worldPoint = origin + direction * t;
                Vector3 localPoint = canvasRect.InverseTransformPoint(worldPoint);
                if (!canvasRect.rect.Contains(new Vector2(localPoint.x, localPoint.y))) continue;

                GameObject graphicHit = FindTopmostGraphicAt(canvas, worldPoint);
                closestDistance = t;
                hitPoint = worldPoint;
                hitObject = graphicHit != null ? graphicHit : canvas.gameObject;
            }

            return hitObject != null;
        }

        // Later siblings draw on top in UGUI, so walking the list back-to-front finds the
        // topmost raycastable graphic under the point. Good enough for this project's flat
        // kiosk/menu canvas hierarchies; does not account for overlapping sub-canvases with
        // their own sort order.
        GameObject FindTopmostGraphicAt(Canvas canvas, Vector3 worldPoint)
        {
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>();
            for (int i = graphics.Length - 1; i >= 0; i--)
            {
                Graphic graphic = graphics[i];
                if (!graphic.raycastTarget || !graphic.gameObject.activeInHierarchy) continue;

                RectTransform rt = graphic.rectTransform;
                Vector3 local = rt.InverseTransformPoint(worldPoint);
                if (rt.rect.Contains(new Vector2(local.x, local.y)))
                {
                    return graphic.gameObject;
                }
            }

            return null;
        }
    }
}
