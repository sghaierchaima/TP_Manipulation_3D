using UnityEngine;

[DisallowMultipleComponent]
public class GridDragController : MonoBehaviour
{
    [Header("Grille & Limites")]
    [Tooltip("Taille d'une cellule de la grille (m).")]
    public float gridSize = 0.25f;

    [Tooltip("Demi-largeur (X) et demi-profondeur (Y) de la zone autorisée autour de la position de départ.")]
    public Vector2 boundsSize = new Vector2(2f, 2f);

    [Header("Vitesses")]
    [Tooltip("Gain appliqué au déplacement (drag).")]
    public float dragGain = 1.0f;

    [Tooltip("Vitesse de rotation lors du twist (degrés * facteur).")]
    public float twistSpeed = 0.25f;

    [Header("Options")]
    [Tooltip("Autoriser les contrôles souris/clavier dans l'éditeur.")]
    public bool enableDesktopControls = true;

    // État initial
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 startScale;

    // Drag
    private bool dragging;
    private Vector3 dragPrevWorld;

    // Twist
    private float prevTwistDeg;

    // Plan sol Y = 0
    private Plane groundPlane = new Plane(Vector3.up, 0f);

    // ------------------------------------------------------------------ //
    //  Lifecycle
    // ------------------------------------------------------------------ //

    void Start()
    {
        // On mémorise la pose de départ
        startPos = transform.position;
        startRot = transform.rotation;
        startScale = transform.localScale;
    }

    public void ResetTransform()
    {
        // Remise à zéro : position, rotation, échelle
        transform.position = startPos;
        transform.rotation = startRot;
        transform.localScale = startScale;
    }

    void Update()
    {
        // R = reset
        if (Input.GetKeyDown(KeyCode.R))
            ResetTransform();

        // Contrôles tactile
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
            // Fin du drag : snap + clamp
            if (dragging)
            {
                SnapToGrid();
                ClampToBoundsGrid();    // <-- nouveau nom
            }
            dragging = false;
        }

        // Contrôles souris/clavier pour l'éditeur
#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableDesktopControls)
            HandleMouse();
#endif
    }

    // ------------------------------------------------------------------ //
    //  Tactile : 1 doigt = drag
    // ------------------------------------------------------------------ //

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
            Vector3 curr = ScreenPointToGround(t.position);
            Vector3 delta = curr - dragPrevWorld;
            dragPrevWorld = curr;

            transform.position += delta * dragGain;
        }
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
        {
            SnapToGrid();
            ClampToBoundsGrid();   // <-- nouveau nom
            dragging = false;
        }
    }

    // ------------------------------------------------------------------ //
    //  Tactile : 2 doigts = twist (rotation)
    // ------------------------------------------------------------------ //

    private void HandleMultiTouch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float ang = Mathf.Atan2(
            t1.position.y - t0.position.y,
            t1.position.x - t0.position.x) * Mathf.Rad2Deg;

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            prevTwistDeg = ang;
        }
        else
        {
            float delta = Mathf.DeltaAngle(prevTwistDeg, ang);
            prevTwistDeg = ang;

            transform.Rotate(0f, delta * twistSpeed, 0f, Space.World);
        }
    }

    // ------------------------------------------------------------------ //
    //  Souris (éditeur)
    // ------------------------------------------------------------------ //
#if UNITY_EDITOR || UNITY_STANDALONE
    private void HandleMouse()
    {
        // Drag (clic gauche)
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            dragPrevWorld = ScreenPointToGround(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && dragging)
        {
            Vector3 curr = ScreenPointToGround(Input.mousePosition);
            Vector3 delta = curr - dragPrevWorld;
            dragPrevWorld = curr;

            transform.position += delta * dragGain;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            SnapToGrid();
            ClampToBoundsGrid();   // <-- nouveau nom
            dragging = false;
        }

        // Rotation (clic droit)
        if (Input.GetMouseButton(1))
        {
            float dx = Input.GetAxis("Mouse X");
            transform.Rotate(0f, dx * 5f, 0f, Space.World);
        }
    }
#endif

    // ------------------------------------------------------------------ //
    //  Utilitaires
    // ------------------------------------------------------------------ //

    // Transforme une position écran en point sur le plan sol
    private Vector3 ScreenPointToGround(Vector3 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return transform.position;
    }

    // Aligne X et Z sur la grille
    private void SnapToGrid()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x / gridSize) * gridSize;
        p.z = Mathf.Round(p.z / gridSize) * gridSize;
        transform.position = p;
    }

    // Limite la position dans un rectangle autour de startPos
    private void ClampToBoundsGrid()
    {
        Vector3 p = transform.position;

        p.x = Mathf.Clamp(p.x,
            startPos.x - boundsSize.x,
            startPos.x + boundsSize.x);

        p.z = Mathf.Clamp(p.z,
            startPos.z - boundsSize.y,
            startPos.z + boundsSize.y);

        transform.position = p;
    }
}
