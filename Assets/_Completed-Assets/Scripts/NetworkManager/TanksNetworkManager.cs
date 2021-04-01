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
        base.OnClientConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("New player added to the server");
        GameObject player = Instantiate(m_RoomPlayerPrefab);

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
        RoomChangeScene(m_GameplayScene);
    }

    public void RoomChangeScene(string newSceneName)
    {
        ServerChangeScene(newSceneName);
    }

    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);

        if (conn != null && conn.identity != null)
        {
            GameObject roomPlayer = conn.identity.gameObject;

            if (roomPlayer != null && roomPlayer.GetComponent<RoomPlayer>() != null)
            {
                Transform startPos = GetStartPosition();
                GameObject player = startPos != null
                    ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                    : Instantiate(playerPrefab);

                NetworkServer.ReplacePlayerForConnection(conn, player, true);

                TankManager tankManager = new TankManager();
                tankManager.m_Instance = player;
                tankManager.m_SpawnPoint = playerPrefab.transform;
                tankManager.m_PlayerColor = Color.red;
                m_Tanks.Add(tankManager);
            }
        }
    }

    public List<TankManager> GetPlayersTanks()
    {
        return m_Tanks;
    }
}
