using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGameOver : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnClose;
    [SerializeField] private Text TextOver;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickClose);
    }

    private void OnDestroy()
    {
        if (btnClose) btnClose.onClick.RemoveAllListeners();
    }

    private void OnClickClose()
    {
        m_mngr.ShowMainMenu();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    public void SetResult(bool isWin)
    {
        if (TextOver == null) return;
        TextOver.text = isWin ? "Level Win" : "You Lose";
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void TextChange(string text)
    {
        if (TextOver == null) return;
        TextOver.text = text;
    }
}