using System;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnTimer;

    [SerializeField] private Button btnFree;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnFree.onClick.AddListener(OnClickFree);
        btnTimer.onClick.AddListener(OnClickTimer);
    }

    private void OnDestroy()
    {
        if (btnFree)  btnFree.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickTimer()
    {
        m_mngr.LoadLevelTimer();
    }

    private void OnClickFree()
    {
        m_mngr.LoadLevelFree();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
}