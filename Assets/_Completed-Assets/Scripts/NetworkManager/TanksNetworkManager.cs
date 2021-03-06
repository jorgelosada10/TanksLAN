using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Complete;

public class TanksNetworkManager : NetworkManager
{
    [SerializeField] private CanvasGroup m_Fade;
    [SerializeField] private FadeManager m_FadeManager;

    [Header("Room Settings")]
    [SerializeField] private GameObject m_RoomPlayerPrefab;

    private MatchRoomManager m_MatchRoomManager;
    private GameManager m_GameManager;

    [Scene]
    public string m_GameplayScene = "";

    [HideInInspector] public List<RoomPlayer> m_RoomPlayers;
    [HideInInspector] public List<TankManager> m_Tanks = new List<TankManager>();
    [HideInInspector] public List<GameObject> m_Players;

    private Dictionary<NetworkConnection, RoomPlayer> m_Clients = new Dictionary<NetworkConnection, RoomPlayer>();

    public override void Awake()
    {
        base.Awake();
        m_RoomPlayers = new List<RoomPlayer>();
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("New client has connected");
        // OnClientConnect by default calls AddPlayer but it should not do
        // that when we have online/offline scenes. so we need the
        // clientLoadedScene flag to prevent it.
        if (!clientLoadedScene)
        {
            // Ready/AddPlayer is usually triggered by a scene load
            // completing. if no scene was loaded, then Ready/AddPlayer it
            // here instead.
            if (!NetworkClient.ready) NetworkClient.Ready();
            if (autoCreatePlayer)
            {
                NetworkClient.AddPlayer();
            }
        }
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        if (IsSceneActive(offlineScene))
        {
            return;
        }
        Debug.Log("New player added to the server");

        GameObject player = Instantiate(m_RoomPlayerPrefab);

        NetworkServer.AddPlayerForConnection(conn, player);

        if (IsSceneActive(m_GameplayScene))
        {
            conn.isReady = true;
            AddTankInstance(conn, true);
        }
    }

    public void ReadyStatusChanged()
    {
        int currentPlayers = 0;
        int readyPlayers = 0;

        foreach (RoomPlayer roomPlayer in m_RoomPlayers)
        {
            if (roomPlayer != null)
            {
                currentPlayers++;
                if (roomPlayer.m_readyToBegin)
                    readyPlayers++;
            }
        }

        if (currentPlayers == readyPlayers)
        {
            StartCoroutine(StartGame());
        }
    }

    private IEnumerator StartGame()
    {
        m_MatchRoomManager.FadeOut();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(m_FadeManager.FadeOut(m_Fade));
        ServerChangeScene(m_GameplayScene);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        if (conn != null && conn.identity != null)
        {
            AddTankInstance(conn, false);
        }
        else if (IsSceneActive(m_GameplayScene))
        {
            conn.isReady = false;            
        }
    }

    private void AddTankInstance(NetworkConnection conn, bool isGameOngoing)
    {
        RoomPlayer roomPlayer = conn.identity.gameObject.GetComponent<RoomPlayer>();
        m_Clients.Add(conn, roomPlayer);

        if (roomPlayer.gameObject != null && roomPlayer != null)
        {
            Transform startPos = GetStartPosition();
            GameObject player = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab);

            TankManager tankManager = roomPlayer.m_Tank;
            tankManager.m_Instance = player;

            NetworkServer.ReplacePlayerForConnection(conn, tankManager.m_Instance, true);

            tankManager.m_SpawnPoint = startPos;

            roomPlayer.DisableRoomSettings();

            m_Tanks.Add(tankManager);

            if(isGameOngoing)
            {
                m_GameManager.AddPlayer(tankManager, conn);
            }
        }
    }

    public List<TankManager> GetPlayersTanks()
    {
        return m_Tanks;
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        GameObject disconnectedPlayer = conn.identity.gameObject;
        if(IsSceneActive(onlineScene))
        {
            NetworkServer.Destroy(disconnectedPlayer);
            RoomPlayer.UpdatePlayerIndexDueDisc();
        }
        else
        {
            NetworkServer.Destroy(m_Clients[conn].gameObject);
            m_GameManager.RemovePlayer(disconnectedPlayer);

            for(int i = 0; i < m_Tanks.Count; i++)
            {
                if(m_Tanks[i].m_Instance == disconnectedPlayer)
                {
                    m_Tanks.RemoveAt(i);
                }
            }
            NetworkServer.Destroy(disconnectedPlayer);
        }

        if(m_Tanks.Count < 1)
        {
            // Forces the server to shutdown.
            Shutdown();

            // Reset internal state of the server and start the server again.
            Start();
        }
    }

    public void DestroyAllRoomPlayers()
    {
        foreach(RoomPlayer roomPlayer in m_RoomPlayers)
        {
            if(roomPlayer != null)
            {
                NetworkServer.Destroy(roomPlayer.gameObject);
            }
            RoomPlayer.ResetPlayerIndexDueDisc();
        }
    }

    public void SetMatchRoomManagerInstance(MatchRoomManager matchRoomManager)
    {
        m_MatchRoomManager = matchRoomManager;
    }

    public void SetGameManagerInstance(GameManager gameManager)
    {
        m_GameManager = gameManager;
    }
}
