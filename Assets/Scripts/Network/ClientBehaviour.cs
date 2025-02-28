using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Events;

/// <summary>
/// Script for client side that holds connection with the host as well as sends/recieves messages
/// Is singleton
/// </summary>
public class ClientBehaviour : MonoBehaviour
{
    private static ClientBehaviour _instance;
    public static ClientBehaviour Instance { get { return _instance; } }

    private NetworkDriver _networkDriver;
    private NetworkConnection _connection;

    public UnityEvent OnConnected;

    private List<NetworkPacket> _scheduledPackets;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        _networkDriver = NetworkDriver.Create(new WebSocketNetworkInterface());
        _scheduledPackets = new List<NetworkPacket>();
        Application.runInBackground = true;
    }
    /// <summary>
    /// Connect to the server passing adress, uses 9001 port by default
    /// </summary>
    /// <param name="adress"></param>
    public void MakeConnection(string adress)
    {
        print("Make connection called");
        if (!_connection.IsCreated)
        {
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(adress, (ushort)9001);
            endpoint.Family = NetworkFamily.Ipv4;
            _connection = _networkDriver.Connect(endpoint);
        }
    }
    public bool TryMakeConnection(string adress)
    {
        if (!_connection.IsCreated)
        {
            NetworkEndpoint endpoint = NetworkEndpoint.Parse(adress, (ushort)9001);
            endpoint.Family = NetworkFamily.Ipv4;
            _connection = _networkDriver.Connect(endpoint);
        }
        return _connection.IsCreated;
    }
    private void OnDestroy()
    {
        _networkDriver.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        _networkDriver.ScheduleUpdate().Complete();
        if (!_connection.IsCreated)
        {
            return;
        }
        ReadData();
        SendData();
    }
    public void SchedulePackage(NetworkPacket pPacket)
    {
        if (!_connection.IsCreated) return;
        print("Package successfully scheduled");
        _scheduledPackets.Add(pPacket);
    }
    private void SendData()
    {
        if (_scheduledPackets == null || _scheduledPackets.Count == 0) return;
        for (int i = 0; i < _scheduledPackets.Count; i++)
        {
            _networkDriver.BeginSend(NetworkPipeline.Null, _connection, out DataStreamWriter writer);
            writer.WriteBytes(_scheduledPackets[i].GetBytes());
            _networkDriver.EndSend(writer);
        }
        _scheduledPackets.Clear();
    }
    private void ReadData()
    {
        DataStreamReader streamReader;
        NetworkEvent.Type cmd;
        while ((cmd = _connection.PopEvent(_networkDriver, out streamReader)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to server!");
                OnConnected?.Invoke();

            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                print("recieved !");
                NetworkPacket pPacket = new NetworkPacket(streamReader);
                ISerializable data = pPacket.Read();
                data.Use();

            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                _connection = default;
            }
        }
    }
}
