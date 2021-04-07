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
    private CanvasGroup m_Fade;
    private FadeManager m_FadeManager;

    private NetworkManager m_TanksNetwork;
    private string serverIP;
    readonly private Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();

    private float m_IpButtonOffset = 30f;
    private int m_IpButtonIndex = 0;

    public NetworkDiscovery networkDiscovery;

    void Awake()
    {
        m_TanksNetwork = FindObjectOfType<TanksNetworkManager>();
        m_Fade = m_TanksNetwork.GetComponentInChildren<CanvasGroup>();
        m_FadeManager = m_TanksNetwork.GetComponentInChildren<FadeManager>();

        m_IPAddressInput.onEndEdit.AddListener(delegate { SetIPAddressToJoin(); });
        StartCoroutine(m_FadeManager.FadeIn(m_Fade));
    }

    public void RunServer()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (!NetworkClient.active)
            {
                discoveredServers.Clear();
                m_TanksNetwork.StartServer();
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
                m_TanksNetwork.StartHost();
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
                    m_TanksNetwork.networkAddress = serverIP;
                else
                    m_TanksNetwork.networkAddress = "localhost"; // For debugging. To be removed
                m_TanksNetwork.StartClient();
            }
        }

        AddressData();
    }

    public void CancelJoinGame()
    {
        if (NetworkClient.active)
        {
            m_TanksNetwork.StopClient();
        }
        Debug.Log("Cancelling connection to " + m_TanksNetwork.networkAddress);
    }

    private void AddressData()
    {
        if (NetworkServer.active)
        {
            Debug.Log("Server: active. IP: " + m_TanksNetwork.networkAddress + " - Transport: " + Transport.activeTransport);
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
        m_IpButtonIndex = 0;
        foreach (Transform transform in m_IPList.transform)
        {
            Destroy(transform.gameObject);
        }
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
        RectTransform buttonTransform = button.GetComponent<RectTransform>();
        buttonTransform.localPosition = new Vector2(buttonTransform.localPosition.x, buttonTransform.localPosition.y + (m_IpButtonOffset * (-m_IpButtonIndex)));
        m_IpButtonIndex++;
    }

    private void JoinServer(ServerResponse info)
    {
        m_TanksNetwork.StartClient(info.uri);
    }
}
