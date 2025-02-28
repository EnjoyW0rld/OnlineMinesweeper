using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class MenuHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private GameObject _serverObject;
    [SerializeField] private GameObject _clientObject;
    [SerializeField] private string _gameScene;

    public void ConnectToServer()
    {
        if (ClientBehaviour.Instance == null)
        {
            Instantiate(_clientObject);
        }
        StartCoroutine(DoNextTick(() => ClientBehaviour.Instance.MakeConnection(_ipInput.text)));
        ClientBehaviour.Instance.OnConnected.AddListener(() => { SceneManager.LoadScene(_gameScene); });
        ClientBehaviour.Instance.MakeConnection(_ipInput.text);

    }
    public void DoLuckyConnect()
    {
        if (ClientBehaviour.Instance == null)
        {
            Instantiate(_clientObject);
        }
    }
    public void HostServer()
    {
        if (ServerBehaviour.Instance == null)
        {
            Instantiate(_serverObject);
        }
        ServerBehaviour.Instance.StartServer();
        SceneManager.LoadScene(_gameScene);
    }
    public static IEnumerator DoNextTick(Action pAct)
    {
        yield return null;
        pAct.Invoke();
    }
    
}
