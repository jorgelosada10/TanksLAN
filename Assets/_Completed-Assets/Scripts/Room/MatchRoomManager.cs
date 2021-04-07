using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MatchRoomManager : NetworkBehaviour
{
    [SerializeField] private Text[] m_PlayersText;
    private TanksNetworkManager m_TanksNetwork;
    private CanvasGroup m_Fade;
    private FadeManager m_FadeManager;

    [SyncVar (hook = nameof(SyncPlayers))]
    public int m_Players;

    private void Awake()
    {
        m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
        m_Fade = m_TanksNetwork.GetComponentInChildren<CanvasGroup>();
        m_FadeManager = m_TanksNetwork.GetComponentInChildren<FadeManager>();
    }

    public override void OnStartServer()
    {
        m_TanksNetwork.SetMatchRoomManagerInstance(this);
    }

    private void SyncPlayers(int oldValue, int newValue)
    {
        for(int i = 0; i < m_PlayersText.Length; i++)
        {
            if (m_PlayersText[i] != null)
            {
                if (i < m_Players)
                {
                    m_PlayersText[i].gameObject.SetActive(true);
                }
                else
                {
                    m_PlayersText[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void SetPlayerReady(int playerIndex, bool readyState)
    {
        Color color = readyState ? Color.green : Color.red;
        m_PlayersText[playerIndex - 1].color = color;
        RpcSetPlayerReady(playerIndex, color);
    }

    [ClientRpc]
    private void RpcSetPlayerReady(int playerIndex, Color color)
    {
        m_PlayersText[playerIndex - 1].color = color;
    }

    public void FadeOut()
    {
        RpcFadeOut();
    }

    [ClientRpc]
    private void RpcFadeOut()
    {
        StartCoroutine(m_FadeManager.FadeOut(m_Fade));
    }
}
