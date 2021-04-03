using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Complete;

public class RoomPlayer : NetworkBehaviour
{
    [SerializeField] private Text m_ReadyText;

    [SyncVar(hook = nameof(SyncNickname))]
    public string m_Nickname;

    [SyncVar (hook = nameof(SyncColor))]
    public Color m_PlayerColor;

    [SyncVar(hook = nameof(SyncReadyToBegin))]
    public bool m_readyToBegin = false;

    public TankManager m_Tank;

    private TanksNetworkManager m_TanksNetwork;
    private Canvas m_Canvas;
    private Customizer m_Customizer;

    private void Awake()
    {
        m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
        m_Canvas = GetComponentInChildren<Canvas>();
        m_Customizer = GetComponentInChildren<Customizer>();

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
            m_ReadyText.text = "Ready";
            CmdChangeReadyState(false);
        }
        else
        {
            m_ReadyText.text = "Cancel";
            CmdChangeReadyState(true);
        }
    }

    [Command]
    public void CmdSetNickname(string nickname)
    {
        m_Nickname = nickname;
        m_Tank.SetTankNickname(m_Nickname);
    }

    private void SyncNickname(string oldNickname, string newNickname)
    {
        Debug.Log("Player nickname selected");
        m_Tank.SetTankNickname(newNickname);
    }

    [Command]
    public void CmdSetColor(Color color)
    {
        m_PlayerColor = color;
        m_Tank.SetTankColor(m_PlayerColor);
    }

    private void SyncColor(Color oldColor, Color newColor)
    {
        Debug.Log("Player color selected");
        m_Tank.SetTankColor(newColor);
    }

    public void DisableRoomSettings()
    {
        if(!isLocalPlayer)
        {
            return;
        }

        RpcDisableCanvas();
    }

    [ClientRpc]
    private void RpcDisableCanvas()
    {
        //m_Canvas.gameObject.SetActive(false);
    }
}
