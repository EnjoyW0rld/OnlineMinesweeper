using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class GameSetUp : MonoBehaviour
{
    [SerializeField] private TMP_InputField _widthInput;
    [SerializeField] private TMP_InputField _heightInput;
    [SerializeField] private TMP_InputField _minesInput;
    private int _seed = 1;
    private void Start()
    {
        if (!ServerBehaviour.IsThisUserServer)
        {
            _widthInput.transform.parent.gameObject.SetActive(false);
            gameObject.SetActive(false);
     
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            StartGame();
            return;
        }
    }
    public void StartGame()
    {
        GameSetUpContainer cont = new GameSetUpContainer();
        cont.Width = Int32.Parse(_widthInput.text);
        cont.Height = Int32.Parse(_heightInput.text);
        cont.Mines = Int32.Parse(_minesInput.text);
        _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue) * _seed;
        cont.Seed = _seed;
        NetworkPacket packet = new NetworkPacket();
        packet.Write(cont);
        ServerBehaviour.Instance.ScheduleMessage(packet);
        cont.Use();
    }
}

public class GameSetUpContainer : ISerializable
{
    public int Width;
    public int Height;
    public int Mines;
    public int Seed;

    public GameSetUpContainer() { }
    public void DeSerialize(NetworkPacket pPacket)
    {
        Width = pPacket.ReadInt();
        Height = pPacket.ReadInt();
        Mines = pPacket.ReadInt();
        Seed = pPacket.ReadInt();
    }

    public void Serialize(NetworkPacket pPacket)
    {
        pPacket.WriteInt(Width);
        pPacket.WriteInt(Height);
        pPacket.WriteInt(Mines);
        pPacket.WriteInt(Seed);
    }

    public void Use()
    {
        Game game = GameObject.FindObjectOfType<Game>();
        game.Initialize(Width, Height,Mines,Seed);
        game.NewGame();
    }
}