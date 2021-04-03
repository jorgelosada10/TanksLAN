using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Complete;

public class TanksNetworkManager : NetworkManager
{
    [Header("Room Settings")]
    [SerializeField] private GameObject m_RoomPlayerPrefab;

    [Scene]
    public string m_GameplayScene = "";

    [HideInInspector] public List<RoomPlayer> m_RoomPlayers;
    [HideInInspector] public List<TankManager> m_Tanks = new List<TankManager>();

    private static int m_PlayerIndex = 1;

    public struct Client
    {
        public NetworkIdentity identity;
        public TankManager tank;
    }

    public override void Awake()
    {
        base.Awake();
        m_RoomPlayers = new List<RoomPlayer>();
    }

    [HideInInspector] public List<GameObject> m_Players;

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("New client has connected");
        if(!IsSceneActive(m_GameplayScene))
            base.OnClientConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("New player added to the server");
        GameObject player = Instantiate(m_RoomPlayerPrefab);

        player.GetComponent<RoomPlayer>().m_Nickname = $"Player{m_PlayerIndex}";
        m_PlayerIndex++;

        NetworkServer.AddPlayerForConnection(conn, player);
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
            CheckReadyToBegin(readyPlayers);
    }

    public void CheckReadyToBegin(int readyPlayers)
    {
        ServerChangeScene(m_GameplayScene);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        if (conn != null && conn.identity != null)
        {
            RoomPlayer roomPlayer = conn.identity.gameObject.GetComponent<RoomPlayer>();

            if (roomPlayer.gameObject != null && roomPlayer != null)
            {
                Transform startPos = GetStartPosition();
                GameObject player = startPos != null
                    ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                    : Instantiate(playerPrefab);

                NetworkServer.ReplacePlayerForConnection(conn, player, true);

                TankManager tankManager = roomPlayer.m_Tank;
                tankManager.m_Instance = player;
                tankManager.m_SpawnPoint = startPos;

                roomPlayer.DisableRoomSettings();

                m_Tanks.Add(tankManager);
            }
        }
    }

    public List<TankManager> GetPlayersTanks()
    {
        return m_Tanks;
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        //OnServerReady();
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        GameObject disconnectedPlayer = conn.identity.gameObject;
        FindObjectOfType<GameManager>().RemovePlayer(disconnectedPlayer);
    }

    void OnGUI()
    {
        NetworkManager manager = NetworkManager.singleton;
        if (manager == null)
            return;

        if (manager.mode == NetworkManagerMode.ServerOnly)
        {
            if (GUILayout.Button("Stop Server"))
            {
                manager.StopServer();
            }
        }
        else if (manager.mode == NetworkManagerMode.Host)
        {
            if (GUILayout.Button("Stop Host"))
            {
                manager.StopHost();
            }
        }
        else if (manager.mode == NetworkManagerMode.ClientOnly)
        {
            if (GUILayout.Button("Stop Client"))
            {
                manager.StopClient();
            }
        }
    }
}
