using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class ServerBehaviour : MonoBehaviour
{
    public static bool IsThisUserServer { get { return _instance != null; } }
    public static ServerBehaviour Instance { get { return _instance; } }
    private static ServerBehaviour _instance;

    private NetworkDriver _networkDriver;
    private NativeList<NetworkConnection> _connections;
    private NetworkConnection _currentReadConnetion;

    private List<ScheduledMessage> _packetsToSend;

    public int ConnectionsCount { get { return _connections.Length; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initializing arrays
        _packetsToSend = new List<ScheduledMessage>();
    }
    private void Start()
    {
        Application.runInBackground = true;
    }

    public void StartServer()
    {
        if (_networkDriver.IsCreated)
        {
            Debug.Log($"Server is already created, is running - {_networkDriver.Listening}");
            return;
        }
        _networkDriver = NetworkDriver.Create(new WebSocketNetworkInterface());
        _connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(9001);
        endpoint.Family = NetworkFamily.Ipv4;

        print(endpoint);
        if (_networkDriver.Bind(endpoint) != 0)
        {
            Debug.LogError("Failed to bind to port 7777");
            return;
        }
        _networkDriver.Listen();
        
    }


    private void RemoveFaultyConnections()
    {
        // Removing old connections
        for (int i = 0; i < _connections.Length; i++)
        {
            if (!_connections[i].IsCreated)
            {
                _connections.RemoveAtSwapBack(i);
                i--;
                Debug.Log("Removed connection");
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!_networkDriver.IsCreated) return;
        _networkDriver.ScheduleUpdate().Complete();
        RemoveFaultyConnections();
        AcceptIncomingConnections();
        ReadIncomingData();
        SendData();
    }

    public void ScheduleMessage(NetworkPacket pPacket, NetworkConnection pConn)
    {
        _packetsToSend.Add(new ScheduledMessage(pPacket, pConn));
    }
    public void ScheduleMessage(NetworkPacket pPacket, int pTeamID = -1)
    {
        _packetsToSend.Add(new ScheduledMessage(pPacket, pTeamID));
    }

    private void AcceptIncomingConnections()
    {
        // Accepting new connections
        NetworkConnection connection;
        while ((connection = _networkDriver.Accept()) != default)
        {
            //UserData newUser = new UserData(connection, (uint)_connections.Length + 1);
            _connections.Add(connection);
            Debug.Log("Added new connection");
        }
    }
    private void SendData()
    {
        if (_packetsToSend.Count == 0) return;

        //Sends data to every connection
        for (int i = 0; i < _packetsToSend.Count; i++)
        {
            if (_packetsToSend[i].IsSendToAll)
            {
                for (int t = 0; t < _connections.Length; t++)
                {
                    if (!_connections[t].IsCreated || _connections[t] == default) continue;
                    _networkDriver.BeginSend(NetworkPipeline.Null, _connections[t], out DataStreamWriter dataWriter);
                    dataWriter.WriteBytes(_packetsToSend[i].Packet.GetBytes());
                    _networkDriver.EndSend(dataWriter);

                }
                continue;
            }

/*
            _networkDriver.BeginSend(NetworkPipeline.Null, conn, out DataStreamWriter writer);
            writer.WriteBytes(_packetsToSend[i].Packet.GetBytes());
            _networkDriver.EndSend(writer);*/
        }
        _packetsToSend.Clear();

    }
    /// <summary>
    /// Dispatches and handles all the incoming messages
    /// </summary>
    private void ReadIncomingData()
    {
        // Reading incoming data
        for (int i = 0; i < _connections.Length; i++)
        {
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = _networkDriver.PopEventForConnection(_connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    _currentReadConnetion = _connections[i];
                    NetworkPacket packet = new NetworkPacket(stream);
                    ISerializable data = packet.Read();
                    data.Use();

                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from the server");
                    _connections[i] = default;
                    break;

                }
            }
        }
        _currentReadConnetion = default;
    }

    private void OnDestroy()
    {
        if (_networkDriver.IsCreated)
        {
            _networkDriver.Dispose();
            _connections.Dispose();
        }
    }
    public static string GetLocalIPv4(NetworkInterfaceType _type)
    {
        string output = "";
        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        output = ip.Address.ToString();
                    }
                }
            }
        }
        return output;
    }

    // ---------------------
    // GET FUNCTIONS
    // ---------------------

    public NetworkConnection GetCurrentConnection()
    {
        return _currentReadConnetion;
    }
    
    private struct ScheduledMessage
    {
        public NetworkPacket Packet;
        public NetworkConnection Connection;
        public int TeamID;
        public bool IsSendToAll { get { return TeamID == -1 && Connection == default; } }

        public ScheduledMessage(NetworkPacket pPacket, NetworkConnection pConn)
        {
            Packet = pPacket;
            Connection = pConn;
            TeamID = -1;
        }
        public ScheduledMessage(NetworkPacket pPacket, int pTeamID = -1)
        {
            Packet = pPacket;
            Connection = default;
            TeamID = pTeamID;
        }

    }
}