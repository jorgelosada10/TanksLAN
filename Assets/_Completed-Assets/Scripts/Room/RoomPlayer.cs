using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RoomPlayer : NetworkBehaviour
{
    [SerializeField] private Text m_Start;

    [SyncVar(hook = nameof(SyncReadyToBegin))]
    public bool m_readyToBegin = false;

    private TanksNetworkManager m_TanksNetwork;
    private Canvas m_Canvas;

    private void Awake()
    {
        m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
        m_Canvas = GetComponentInChildren<Canvas>();
        m_Canvas.gameObject.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        m_Canvas.gameObject.SetActive(true);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        m_TanksNetwork.m_RoomPlayers.Add(this);
    }


    [Command]
    public void CmdChangeReadyState(bool readyState)
    {
        m_readyToBegin = readyState;
        m_TanksNetwork.ReadyStatusChanged();
    }

    private void SyncReadyToBegin(bool oldReadyState, bool newReadyState)
    {
        Debug.Log("Player ready");
    }

    public void OnStartClick()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (m_readyToBegin)
        {
            m_Start.text = "Ready";
            CmdChangeReadyState(false);
        }
        else
        {
            m_Start.text = "Cancel";
            CmdChangeReadyState(true);
        }
    }
}
