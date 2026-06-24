using UnityEngine;

[RequireComponent(typeof(OrbitCamera))]
public class IdleOrbit : MonoBehaviour
{
    public float idleTimeout   = 4.0f;
    public float orbitSpeed    = 4.0f;
    public float resumeSmooth  = 3.0f;

    private OrbitCamera _orbit;
    private float       _idleTimer = 0f;
    private bool        _isIdle    = false;

    void Start() { _orbit = GetComponent<OrbitCamera>(); }

    void Update()
    {
        bool userActive = Input.anyKey ||
                          Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f ||
                          Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f ||
                          Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;

        if (userActive)
        {
            _idleTimer = 0f;
            _isIdle    = false;
            return;
        }

        _idleTimer += Time.deltaTime;
        if (_idleTimer >= idleTimeout) _isIdle = true;

        if (_isIdle)
        {
            _orbit.YawOffset += orbitSpeed * Time.deltaTime;
            if (_orbit.YawOffset > 360f) _orbit.YawOffset -= 360f;
        }
    }
}
