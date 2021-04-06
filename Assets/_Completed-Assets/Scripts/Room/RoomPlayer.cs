using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Complete;

public class RoomPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject m_TankModel;
    [SerializeField] private Text m_ReadyText;

    [SyncVar(hook = nameof(SyncNickname))]
    public string m_Nickname;

    [SyncVar(hook = nameof(SyncColor))]
    public Color m_PlayerColor;

    [SyncVar(hook = nameof(SyncReadyToBegin))]
    public bool m_readyToBegin = false;

    public TankManager m_Tank;

    private TanksNetworkManager m_TanksNetwork;
    private Canvas m_Canvas;
    private Customizer m_Customizer;
    private Nicknamer m_Nicknamer;
    private MeshRenderer[] m_MeshRenderers;

    private static int m_PlayerIndex = 1;

    private void Awake()
    {
        m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
        m_Canvas = GetComponentInChildren<Canvas>();
        m_Customizer = GetComponentInChildren<Customizer>();
        m_Nicknamer = GetComponentInChildren<Nicknamer>();
        m_MeshRenderers = GetComponentsInChildren<MeshRenderer>();

        m_Canvas.gameObject.SetActive(false);

        m_TankModel.SetActive(false);
    }

    public override void OnStartServer()
    {
        m_Nickname = $"Player{m_PlayerIndex}";
        m_PlayerIndex++;
        m_Tank.SetTankNickname(m_Nickname);
    }

    public override void OnStartLocalPlayer()
    {
        m_Canvas.gameObject.SetActive(true);

        m_TankModel.SetActive(true);

        m_Nicknamer.SetDefaultValue(m_Nickname);
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
        m_Tank.SetTankColor(newColor);
        //Go through all the renderers...
        for (int i = 0; i < m_MeshRenderers.Length; i++)
        {
            // ... set their material color to the color specific to this tank
            m_MeshRenderers[i].material.color = newColor;
        }
    }

    public void DisableRoomSettings()
    {
        RpcDisableCanvas();
    }

    [ClientRpc]
    private void RpcDisableCanvas()
    {
        //m_Canvas.gameObject.SetActive(false);

        m_TankModel.SetActive(false);
    }

    public static void UpdatePlayerIndexDueDisc()
    {
        m_PlayerIndex--;
    }

    public static void ResetPlayerIndexDueDisc()
    {
        m_PlayerIndex = 1;
    }
}
