using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavController : MonoBehaviour
{
    [Header("Nav Buttons")]
    public Button[] navButtons;

    [Header("Active State")]
    public Color colorActiveBG   = new Color(0.05f, 0.16f, 0.26f);
    public Color colorInactiveBG = new Color(0.10f, 0.16f, 0.23f);
    public Color colorActiveText = new Color(0.00f, 0.67f, 1.00f);
    public Color colorInactiveText = new Color(0.80f, 0.87f, 0.93f);

    [Header("Panels to Show per Nav Item")]
    public GameObject[] linkedPanels;

    [Header("Dashboard Panels — hidden when Simulation/History is active")]
    public GameObject[] dashboardPanels;

    [Header("Alert Badge")]
    public GameObject alertBadge;
    public TMP_Text   badgeCount;

    [Header("Runtime Registrations")]
    private SimulationController _simController;

    [Header("3D Scene Components — disabled when Simulation is open")]
    public OrbitCamera   orbitCamera;
    public IdleOrbit     idleOrbit;
    public Camera        simPackCamera;   // Visualization panel secondary camera

    private int _activeIndex = 0;

    void Start()
    {
        for (int i = 0; i < navButtons.Length; i++)
        {
            int index = i;
            navButtons[i].onClick.AddListener(() => SelectNav(index));
        }

        SelectNav(0);
    }

    public void SelectNav(int index)
    {
        _activeIndex = index;

        for (int i = 0; i < navButtons.Length; i++)
        {
            bool active = i == index;

            // Button background
            ColorBlock cb  = navButtons[i].colors;
            cb.normalColor = active ? colorActiveBG : colorInactiveBG;
            navButtons[i].colors = cb;

            // Text color
            TMP_Text label = navButtons[i].GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = active ? colorActiveText : colorInactiveText;

            // Accent line — only on NavBtn_Overview (index 0)
            Transform accent = navButtons[i].transform.Find("ActiveAccent");
            if (accent != null)
                accent.gameObject.SetActive(active);

            // Linked panel visibility
            if (linkedPanels != null && i < linkedPanels.Length && linkedPanels[i] != null)
                linkedPanels[i].SetActive(active);
        }

        // Hide dashboard panels (Right/CellVoltages/ViewModes) when Simulation or History is active.
        // They re-show when the user returns to Overview.
        bool hideDashboard = index == 5 /*Simulation*/ || index == 7 /*History*/;
        if (dashboardPanels != null)
        {
            foreach (var p in dashboardPanels)
            {
                if (p != null) p.SetActive(!hideDashboard);
            }
        }

        // Disable orbit camera when Simulation or History page is active
        // so the mouse does not accidentally control the 3D view.
        if (orbitCamera   != null) orbitCamera.enabled   = !hideDashboard;
        if (idleOrbit    != null) idleOrbit.enabled    = !hideDashboard;
        if (simPackCamera != null) simPackCamera.enabled = hideDashboard;  // on when sim page open

        // Notify the simulation controller when the Simulation tab is opened/closed.
        if (_simController != null)
            _simController.SetSimPageOpen(index == 5);
    }

    /// <summary>
    /// Register a panel that was created at runtime (e.g. Panel_Simulation).
    /// </summary>
    public void RegisterPanel(int index, GameObject panel)
    {
        if (linkedPanels == null || index < 0 || index >= linkedPanels.Length) return;
        linkedPanels[index] = panel;

        // If this index is currently active, make sure the new panel state is honoured.
        if (_activeIndex == index && panel != null)
            panel.SetActive(true);
    }

    /// <summary>
    /// Register the runtime SimulationController so nav switches can pause/resume it.
    /// </summary>
    public void RegisterSimulationController(SimulationController ctrl)
    {
        _simController = ctrl;
        if (_simController != null)
            _simController.SetSimPageOpen(_activeIndex == 5);
    }

    // Call this from AlarmManager when new alarms come in
    public void SetAlertCount(int count)
    {
        if (alertBadge != null)
            alertBadge.SetActive(count > 0);
        if (badgeCount != null)
            badgeCount.text = count.ToString();
    }
}