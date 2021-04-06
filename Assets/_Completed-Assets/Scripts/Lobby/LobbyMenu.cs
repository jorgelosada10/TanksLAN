using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Discovery;

[DisallowMultipleComponent]
[AddComponentMenu("Network/NetworkDiscoveryHUD")]
[HelpURL("https://mirror-networking.com/docs/Articles/Components/NetworkDiscovery.html")]
[RequireComponent(typeof(NetworkDiscovery))]
public class LobbyMenu : MonoBehaviour
{
    [SerializeField] private InputField m_IPAddressInput;
    [SerializeField] private GameObject m_IPList;
    [SerializeField] private GameObject m_IPButton;

    private NetworkManager manager;
    private string serverIP;
    readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();
    Dictionary<long, ServerResponse> discoveredServersTemp = new Dictionary<long, ServerResponse>();

    public NetworkDiscovery networkDiscovery;

    void Awake()
    {
        manager = FindObjectOfType<NetworkManager>();

        m_IPAddressInput.onEndEdit.AddListener(delegate { SetIPAddressToJoin(); });
    }

    public void RunServer()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (!NetworkClient.active)
            {
                discoveredServers.Clear();
                manager.StartServer();
                networkDiscovery.AdvertiseServer();

            }
        }

        AddressData();
    }

    public void CreateGame()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (!NetworkClient.active)
            {
                discoveredServers.Clear();
                manager.StartHost();
                networkDiscovery.AdvertiseServer();
            }
        }

        AddressData();
    }

    public void JoinGame()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (!NetworkClient.active)
            {
                if (serverIP != null)
                    manager.networkAddress = serverIP;
                else
                    manager.networkAddress = "localhost"; // For debugging. To be removed
                manager.StartClient();
            }
        }

        AddressData();
    }

    public void CancelJoinGame()
    {
        if(NetworkClient.active)
        {
            manager.StopClient();
        }
        Debug.Log("Cancelling connection to " + manager.networkAddress);
    }

    private void AddressData()
    {
        if (NetworkServer.active)
        {
            Debug.Log("Server: active. IP: " + manager.networkAddress + " - Transport: " + Transport.activeTransport);
        }
        else
        {
            Debug.Log("Attempted to join server " + serverIP);
        }

        Debug.Log("Local IP Address: " + GetLocalIPAddress());

        Debug.Log("//////////////");
    }

    private static string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    private void SetIPAddressToJoin()
    {
        Debug.Log($"Ip to join is: {m_IPAddressInput.text}");
        serverIP = m_IPAddressInput.text;
    }

    public void StartDiscovery()
    {
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();
    }

    public void OnDiscoveredServer(ServerResponse info)
    {
        // Note that you can check the versioning to decide if you can connect to the server or not using this method
        discoveredServers[info.serverId] = info;
        foreach (ServerResponse server in discoveredServers.Values)
        {
            AddServerButton(server, server.EndPoint.Address.ToString());
        }
    }

    public void AddServerButton(ServerResponse info, string ip)
    {
        Button button = Instantiate(m_IPButton, m_IPList.transform).GetComponent<Button>();
        button.GetComponentInChildren<Text>().text = ip;
        button.onClick.AddListener(() => JoinServer(info));
    }

    private void JoinServer(ServerResponse info)
    {
        manager.StartClient(info.uri);
    }
}
