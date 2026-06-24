using UnityEngine;
using System.Collections.Generic;

public class TubeConnector : MonoBehaviour
{
    [Header("References")]
    public BmsController bmsController;
    public Transform     bmsBoard;
    public Transform[]   cellTransforms = new Transform[8];

    [Header("Terminal Connection Points")]
    public float terminalLocalY  = 0.065f;
    public float bmsBoardLocalY  = 0.005f;

    [Header("Path Shape — Rise")]
    [Tooltip("How far above the highest cell top the horizontal run sits")]
    public float riseAboveCells      = 0.08f;
    [Tooltip("Extra Y stagger between each wire at the top (avoids overlap)")]
    public float riseStaggerPerCol   = 0.05f;
    [Tooltip("Extra Y stagger between front and back row")]
    public float riseStaggerPerRow   = 0.03f;

    [Header("Path Shape — Drop Column")]
    [Tooltip("X offset of drop column from BMS X (positive = further left from BMS)")]
    public float dropColumnXOffset   = 0.20f;
    [Tooltip("X spread between wires at the drop column")]
    public float dropSpreadPerCol    = 0.05f;
    [Tooltip("Extra X spread between front and back row")]
    public float dropSpreadPerRow    = 0.025f;

    [Header("Path Shape — Charger Tube")]
    [Tooltip("How high the charger wire rises above the charger block")]
    public float chargerRiseOffset   = 0.10f;
    [Tooltip("X offset of charger drop from BMS X (positive = right of BMS)")]
    public float chargerDropXOffset  = 0.05f;

    [Header("Tube Appearance")]
    public Material tubeMaterial;
    public float    tubeRadius       = 0.025f;

    [Header("Particle Appearance")]
    public Material particleMaterial;
    public int      particlesPerTube = 6;
    public float    particleSpeed    = 1.5f;
    public float    particleSize     = 0.05f;
    public Color    particleCharging    = new Color(0.00f, 0.60f, 1.00f, 1f);
    public Color    particleDischarging = new Color(1.00f, 0.40f, 0.00f, 1f);

    [Header("Tube Colors")]
    public Color colorCharging    = new Color(0.00f, 0.80f, 1.00f, 0.20f);
    public Color colorDischarging = new Color(1.00f, 0.50f, 0.00f, 0.20f);

    [Header("Charger / Load Block")]
    public Transform  chargerBlock;
    public GameObject labelCharger;
    public GameObject labelLoad;

    [Header("Runtime Rebuild")]
    [Tooltip("Press this in Play mode to rebuild tubes after changing parameters")]
    public bool rebuildNow = false;

    [Header("Per-Cell Path Control — Lower row (Cell_05 to Cell_08, left to right)")]
    [Tooltip("Z offset from terminal for first horizontal segment")]
    public float[] cellStepZ = new float[4] { 0.1f, 0.1f, 0.1f, 0.1f };
    [Tooltip("X position of vertical rise column")]
    public float[] cellStepX = new float[4] { 0.5f, 0.5f, 0.5f, 0.5f };
    [Tooltip("Z offset at BMS connection point")]
    public float[] cellBmsZ  = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };

    [Header("Per-Cell Path Control — Upper row (Cell_01 to Cell_04, right to left)")]
    [Tooltip("Z offset from terminal for first horizontal segment")]
    public float[] upperCellStepZ = new float[4] { 0.1f, 0.1f, 0.1f, 0.1f };
    [Tooltip("X position of vertical rise column")]
    public float[] upperCellStepX = new float[4] { 0.5f, 0.5f, 0.5f, 0.5f };
    [Tooltip("Z offset at BMS connection point")]
    public float[] upperCellBmsZ  = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
    // Internal
    private GameObject[]    _tubes;
    private GameObject      _chargerTube;
    private List<Vector3>[] _tubePaths;
    private GameObject[][]  _particles;
    private float[][]       _particleOffsets;
    private bool            _lastDischarging;
    private int             _totalTubes;

    void Start()
    {
        _totalTubes = cellTransforms.Length + 1; // cells + charger
        BuildAllTubes();
        BuildAllParticles();
        UpdateChargerBlock();
    }

    void Update()
    {
        // Live rebuild when checkbox ticked in Inspector
        if (rebuildNow)
        {
            rebuildNow = false;
            Rebuild();
        }
        if (bmsController == null) return;

        bool discharging = bmsController.data.IsDischarging;
        Color tubeCol    = discharging ? colorDischarging    : colorCharging;
        Color partCol    = discharging ? particleDischarging : particleCharging;

        // Update tube colors — target segment children not parent
        for (int i = 0; i < _tubes.Length; i++)
            SetTubeColor(_tubes[i], tubeCol);

        SetTubeColor(_chargerTube, tubeCol);

        // Animate particles
        AnimateParticles(partCol, discharging);

        if (discharging != _lastDischarging)
        {
            _lastDischarging = discharging;
            UpdateChargerBlock();
        }
    }
    public void Rebuild()
    {
        // Destroy old tubes
        foreach (var t in _tubes)
            if (t != null) Destroy(t);
        if (_chargerTube != null) Destroy(_chargerTube);

        // Destroy old particles
        if (_particles != null)
            foreach (var arr in _particles)
                if (arr != null)
                    foreach (var p in arr)
                        if (p != null) Destroy(p);

        // Rebuild
        _totalTubes = cellTransforms.Length + 1;
        BuildAllTubes();
        BuildAllParticles();
    }

    // ── Get world-space positive terminal position ────────────────
    Vector3 GetCellTerminalPos(Transform cell)
    {
        // Convert local top Y to world space
        return cell.TransformPoint(new Vector3(0, terminalLocalY, 0));
    }

    Vector3 GetBMSConnectionPos()
    {
        if (bmsBoard == null) return Vector3.zero;
        return bmsBoard.TransformPoint(new Vector3(0, bmsBoardLocalY, 0));
    }

    // ── Build L-shaped path to avoid overlaps ────────────────────
    List<Vector3> BuildPath(Vector3 start, Vector3 end, float riseY, float dropX, float dropZ)
    {
        // All wires rise to the SAME Y plane
        // but each has its own X/Z lane
        Vector3 p1 = new Vector3(start.x, riseY, start.z);   // rise straight up
        Vector3 p2 = new Vector3(dropX,   riseY, start.z);   // travel in X
        Vector3 p3 = new Vector3(dropX,   riseY, dropZ);     // travel in Z
        Vector3 p4 = new Vector3(dropX,   end.y, dropZ);     // drop down
        Vector3 p5 = end;                                      // arrive at BMS

        return new List<Vector3> { start, p1, p2, p3, p4, p5 };
    }

    void BuildAllTubes()
    {
        _tubes     = new GameObject[cellTransforms.Length];
        _tubePaths = new List<Vector3>[_totalTubes];

        Vector3 bmsPos     = GetBMSConnectionPos();
        float   highestTop = GetHighestCellTop();
        float   riseY      = highestTop + riseAboveCells;

        for (int i = 0; i < cellTransforms.Length; i++)
        {
            if (cellTransforms[i] == null) continue;

            Vector3 term = GetCellTerminalPos(cellTransforms[i]);
            List<Vector3> path;

            if (i >= 4)
            {
                // ── Lower 4 cells — full 5-point manual control ──────
                // Safe array access
                int   localIdx = i - 4;   // maps cell index 4-7 → array index 0-3
                float stepZ = localIdx < cellStepZ.Length ? cellStepZ[localIdx] : 0f;
                float stepX = localIdx < cellStepX.Length ? cellStepX[localIdx] : bmsPos.x;
                float bmsZ  = localIdx < cellBmsZ.Length  ? cellBmsZ[localIdx]  : bmsPos.z;

                // Point 1: cell terminal (exact)
                Vector3 p1 = term;

                // Point 2: move in Z (parallel to Z axis) — user sets stepZ
                Vector3 p2 = new Vector3(term.x, term.y, term.z + stepZ);

                // Point 3: rise up in Y (parallel to Y axis) — riseAboveCells controls this
                Vector3 p3 = new Vector3(term.x, riseY, term.z + stepZ);

                // Point 4: move in X (parallel to X axis) — user sets stepX
                Vector3 p4 = new Vector3(stepX, riseY, term.z + stepZ);

                // Point 5: move in Z to BMS (parallel to Z axis) — user sets bmsZ
                Vector3 p5 = new Vector3(stepX, riseY, bmsPos.z + bmsZ);

                // Point 6: drop to BMS connection
                Vector3 p6 = new Vector3(stepX, bmsPos.y, bmsPos.z + bmsZ);

                // Point 7: arrive at BMS
                Vector3 p7 = bmsPos;

                path = new List<Vector3> { p1, p2, p3, p4, p5, p6, p7 };
            }
            else
            {
                // ── Upper 4 cells (Cell_01 to Cell_04, right to left = index 3,2,1,0) ──
                // i goes 0,1,2,3 → map right-to-left so index 3=Cell_01, 0=Cell_04
                int localIdx = 3 - i;

                float stepZ = localIdx < upperCellStepZ.Length ? upperCellStepZ[localIdx] : 0f;
                float stepX = localIdx < upperCellStepX.Length ? upperCellStepX[localIdx] : bmsPos.x;
                float bmsZ  = localIdx < upperCellBmsZ.Length  ? upperCellBmsZ[localIdx]  : bmsPos.z;

                // Point 1: cell terminal (exact)
                Vector3 p1 = term;

                // Point 2: move in Z (parallel to Z axis)
                Vector3 p2 = new Vector3(term.x, term.y, term.z + stepZ);

                // Point 3: rise up in Y
                Vector3 p3 = new Vector3(term.x, riseY, term.z + stepZ);

                // Point 4: move in X (parallel to X axis)
                Vector3 p4 = new Vector3(stepX, riseY, term.z + stepZ);

                // Point 5: move in Z toward BMS
                Vector3 p5 = new Vector3(stepX, riseY, bmsPos.z + bmsZ);

                // Point 6: drop to BMS height
                Vector3 p6 = new Vector3(stepX, bmsPos.y, bmsPos.z + bmsZ);

                // Point 7: arrive at BMS
                Vector3 p7 = bmsPos;

                path = new List<Vector3> { p1, p2, p3, p4, p5, p6, p7 };
            }

            _tubePaths[i] = path;
            _tubes[i]     = BuildSegmentedTube($"Tube_{i + 1:00}", path);
        }

        // Charger tube
        if (chargerBlock != null)
        {
            Vector3 cbPos      = chargerBlock.position;
            Vector3 chargerTop = new Vector3(
                cbPos.x,
                cbPos.y + chargerBlock.localScale.y * 0.5f,
                cbPos.z);

            float chargerRiseY = chargerTop.y + chargerRiseOffset;

            Vector3 c0 = chargerTop;
            Vector3 c1 = new Vector3(chargerTop.x, chargerRiseY, chargerTop.z);
            Vector3 c2 = new Vector3(bmsPos.x + chargerDropXOffset, chargerRiseY, chargerTop.z);
            Vector3 c3 = new Vector3(bmsPos.x + chargerDropXOffset, chargerRiseY, bmsPos.z);
            Vector3 c4 = new Vector3(bmsPos.x + chargerDropXOffset, bmsPos.y,     bmsPos.z);
            Vector3 c5 = bmsPos;

            List<Vector3> chargerPath = new List<Vector3> { c0, c1, c2, c3, c4, c5 };
            _tubePaths[cellTransforms.Length] = chargerPath;
            _chargerTube = BuildSegmentedTube("Tube_Charger", chargerPath);
        }
    }
    // Helper — finds rightmost cell X in world space
    float GetRightmostCellX()
    {
        float maxX = float.MinValue;
        foreach (var cell in cellTransforms)
        {
            if (cell == null) continue;
            if (cell.position.x > maxX) maxX = cell.position.x;
        }
        return maxX;
    }

    // Returns the highest world Y point among all cell tops
    float GetHighestCellTop()
    {
        float maxY = 0f;
        foreach (var cell in cellTransforms)
        {
            if (cell == null) continue;
            Vector3 top = GetCellTerminalPos(cell);
            if (top.y > maxY) maxY = top.y;
        }
        return maxY;
    }

    GameObject BuildSegmentedTube(string name, List<Vector3> path)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(transform);

        for (int s = 0; s < path.Count - 1; s++)
        {
            Vector3 segStart = path[s];
            Vector3 segEnd   = path[s + 1];
            Vector3 dir      = segEnd - segStart;
            float   length   = dir.magnitude;
            if (length < 0.001f) continue;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = $"{name}_seg{s}";
            seg.transform.SetParent(root.transform);
            Destroy(seg.GetComponent<Collider>());

            seg.transform.position   = segStart + dir * 0.5f;
            seg.transform.up         = dir.normalized;
            seg.transform.localScale = new Vector3(tubeRadius, length * 0.5f, tubeRadius);

            if (tubeMaterial != null)
                seg.GetComponent<Renderer>().material = new Material(tubeMaterial);
        }

        return root;
    }

    // ── Build neon particles for every tube ───────────────────────
    void BuildAllParticles()
    {
        _particles       = new GameObject[_totalTubes][];
        _particleOffsets = new float[_totalTubes][];

        for (int t = 0; t < _totalTubes; t++)
        {
            if (_tubePaths[t] == null) continue;

            _particles[t]       = new GameObject[particlesPerTube];
            _particleOffsets[t] = new float[particlesPerTube];

            for (int p = 0; p < particlesPerTube; p++)
            {
                // Small sphere as neon bead
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = $"Particle_T{t}_P{p}";
                sphere.transform.SetParent(transform);
                sphere.transform.localScale = Vector3.one * particleSize;
                Destroy(sphere.GetComponent<Collider>());

                if (particleMaterial != null)
                    sphere.GetComponent<Renderer>().material =
                        new Material(particleMaterial);

                // Stagger start positions along the path
                _particleOffsets[t][p] = (float)p / particlesPerTube;
                _particles[t][p] = sphere;
            }
        }
    }

    void AnimateParticles(Color col, bool discharging)
    {
        float t = Time.time * particleSpeed;

        for (int tube = 0; tube < _totalTubes; tube++)
        {
            if (_tubePaths[tube] == null || _particles[tube] == null) continue;

            float pathLength = GetPathLength(_tubePaths[tube]);

            for (int p = 0; p < particlesPerTube; p++)
            {
                if (_particles[tube][p] == null) continue;

                // Path direction: cell-terminal → BMS-board (see BuildAllTubes).
                // Discharging: cells → BMS → load   = forward  (0→1)
                // Charging:    charger → BMS → cells = reverse (1→0)
                float progress = Mathf.Repeat(
                    discharging
                        ? (t / pathLength + _particleOffsets[tube][p])
                        : 1f - (t / pathLength + _particleOffsets[tube][p]),
                    1f);

                Vector3 pos = SamplePath(_tubePaths[tube], progress);
                _particles[tube][p].transform.position = pos;

                // Pulse scale for neon bead glow effect
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 8f + p * 1.3f + tube);
                _particles[tube][p].transform.localScale =
                    Vector3.one * particleSize * pulse;

                // Apply color
                _particles[tube][p].GetComponent<Renderer>().material.color = col;
            }
        }
    }

    float GetPathLength(List<Vector3> path)
    {
        if (path == null || path.Count < 2) return 1f;
        float len = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float seg = Vector3.Distance(path[i], path[i + 1]);
            if (!float.IsNaN(seg)) len += seg;
        }
        return Mathf.Max(len, 0.001f);
    }

    Vector3 SamplePath(List<Vector3> path, float t)
    {
        if (path == null || path.Count < 2) return Vector3.zero;

        t = Mathf.Clamp01(t);
        float totalLen  = GetPathLength(path);
        float targetLen = t * totalLen;
        float walked    = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            float segLen = Vector3.Distance(path[i], path[i + 1]);
            if (segLen < 0.0001f) continue;  // skip zero-length segments

            if (walked + segLen >= targetLen)
            {
                float localT = (targetLen - walked) / segLen;
                Vector3 result = Vector3.Lerp(path[i], path[i + 1], localT);
                if (float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z))
                    return path[i];  // fallback to segment start
                return result;
            }
            walked += segLen;
        }
        return path[path.Count - 1];
    }

    void UpdateChargerBlock()
    {
        if (bmsController == null) return;
        bool discharging = bmsController.data.IsDischarging;
        if (labelCharger != null) labelCharger.SetActive(!discharging);
        if (labelLoad    != null) labelLoad.SetActive(discharging);
    }

    void SetTubeColor(GameObject tubeRoot, Color col)
    {
        if (tubeRoot == null) return;
        foreach (Renderer r in tubeRoot.GetComponentsInChildren<Renderer>())
            r.material.color = col;
    }
}