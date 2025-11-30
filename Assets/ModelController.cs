using UnityEngine;

public class ModelController : MonoBehaviour
{
    [Header("Échelle (zoom)")]
    public float minScale = 0.5f;
    public float maxScale = 2f;

    [Header("Vitesses (mobile)")]
    public float dragSpeed = 0.005f;
    public float rotSpeed = 0.2f;

    [Header("Options desktop (Éditeur)")]
    public bool enableDesktopControls = true;
    public float mouseRotateSpeed = 5f;
    public float mouseDragSpeed = 0.02f;
    public float mouseScrollScaleFactor = 0.1f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 startScale;

    private bool dragging;
    private Vector3 dragPrevWorld;

    private float prevPinchDist;
    private float prevTwistAngleDeg;

    // Plan de glissement (Y=0 par défaut). Vous pouvez l'adapter si votre sol est ailleurs.
    private Plane groundPlane = new Plane(Vector3.up, 0f);

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        startScale = transform.localScale;
    }

    public void ResetTransform()
    {
        transform.position = startPos;
        transform.rotation = startRot;
        transform.localScale = startScale;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetTransform();

        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (Input.touchCount >= 2)
        {
            HandleMultiTouch();
        }
        else
        {
            dragging = false;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableDesktopControls)
        {
            HandleMouseControls();
        }
#endif
    }

    private void HandleSingleTouch()
    {
        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
        {
            dragging = true;
            dragPrevWorld = ScreenPointToGround(t.position);
        }
        else if (t.phase == TouchPhase.Moved && dragging)
        {
            Vector3 currWorld = ScreenPointToGround(t.position);
            Vector3 delta = currWorld - dragPrevWorld;
            dragPrevWorld = currWorld;

            transform.position += delta * (1f + dragSpeed * 100f);
        }
        else if (t.phase == TouchPhase.Ended)
        {
            dragging = false;
        }
    }

    private void HandleMultiTouch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float dist = Vector2.Distance(t0.position, t1.position);
        float ang = Mathf.Atan2(t1.position.y - t0.position.y,
                                t1.position.x - t0.position.x) * Mathf.Rad2Deg;

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            prevPinchDist = dist;
            prevTwistAngleDeg = ang;
        }
        else
        {
            float pinchRatio = prevPinchDist > 0.001f ? dist / prevPinchDist : 1f;
            prevPinchDist = dist;

            Vector3 newScale = transform.localScale * pinchRatio;
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            transform.localScale = newScale;

            float twistDelta = Mathf.DeltaAngle(prevTwistAngleDeg, ang);
            prevTwistAngleDeg = ang;

            transform.Rotate(0f, twistDelta * rotSpeed, 0f);
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void HandleMouseControls()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            dragPrevWorld = ScreenPointToGround(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && dragging)
        {
            Vector3 currWorld = ScreenPointToGround(Input.mousePosition);
            Vector3 delta = currWorld - dragPrevWorld;
            dragPrevWorld = currWorld;

            transform.position += delta * (1f + mouseDragSpeed * 50f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            transform.Rotate(0f, dx * mouseRotateSpeed, 0f);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float factor = 1f + scroll * mouseScrollScaleFactor;

            Vector3 newScale = transform.localScale * factor;
            newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
            newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
            newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

            transform.localScale = newScale;
        }
    }
#endif

    private Vector3 ScreenPointToGround(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return transform.position;
    }
}
