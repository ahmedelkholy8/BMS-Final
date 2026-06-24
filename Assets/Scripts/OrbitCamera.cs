using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 0.5f, 0);

    [Header("Distance")]
    public float distance     = 6.0f;
    public float minDistance  = 2.5f;
    public float maxDistance  = 12.0f;
    public float zoomSpeed    = 2.0f;

    [Header("Orbit")]
    public float orbitSpeedX  = 180f;
    public float orbitSpeedY  = 120f;
    public float minYAngle    = -10f;
    public float maxYAngle    = 75f;

    [Header("Damping")]
    public float dampingSpeed = 8.0f;

    // Internal state
    private float _currentX   = 20f;
    private float _currentY   = 28f;
    private float _targetX    = 20f;
    private float _targetY    = 28f;
    private float _targetDist;
    private bool  _isDragging = false;

    /// <summary>Exposed for IdleOrbit — add to _targetX each frame to auto-orbit.</summary>
    public float YawOffset { get => _targetX; set => _targetX = value; }

    void Start()
    {
        _targetDist = distance;

        // If no target assigned, target the BatteryPack
        if (target == null)
        {
            var pack = GameObject.Find("BatteryPack");
            if (pack != null) target = pack.transform;
        }
    }

    void Update()
    {
        HandleInput();
        ApplyOrbit();
    }

    void HandleInput()
    {
        // ── Drag to orbit ────────────────────────────────────
        if (Input.GetMouseButtonDown(0)) _isDragging = true;
        if (Input.GetMouseButtonUp(0))   _isDragging = false;

        if (_isDragging)
        {
            _targetX += Input.GetAxis("Mouse X") * orbitSpeedX * Time.deltaTime;
            _targetY -= Input.GetAxis("Mouse Y") * orbitSpeedY * Time.deltaTime;
            _targetY  = Mathf.Clamp(_targetY, minYAngle, maxYAngle);
        }

        // ── Scroll to zoom ───────────────────────────────────
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _targetDist -= scroll * zoomSpeed;
        _targetDist  = Mathf.Clamp(_targetDist, minDistance, maxDistance);
    }

    void ApplyOrbit()
    {
        // Smooth damp toward targets
        _currentX = Mathf.LerpAngle(_currentX, _targetX, Time.deltaTime * dampingSpeed);
        _currentY = Mathf.Lerp(_currentY, _targetY, Time.deltaTime * dampingSpeed);
        distance  = Mathf.Lerp(distance, _targetDist, Time.deltaTime * dampingSpeed);

        // Calculate position
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
        Vector3 pivot = (target != null ? target.position : Vector3.zero) + targetOffset;
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        transform.position = pivot + offset;
        transform.LookAt(pivot);
    }

    // Call this to snap to a preset angle (used by view mode buttons)
    public void SnapTo(float x, float y, float dist, float duration = 0.5f)
    {
        _targetX    = x;
        _targetY    = y;
        _targetDist = dist;
    }
}