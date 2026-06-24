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

    [Header("Alert Badge")]
    public GameObject alertBadge;
    public TMP_Text   badgeCount;

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