using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MirrorExample
{
    public class NetworkHUD : MonoBehaviour
    {
        public NetworkDiscovery networkDiscovery;

        readonly Dictionary<long,ServerResponse> discoveredServers = new Dictionary<long,ServerResponse>();

        public GameObject findServerBtn;
        public GameObject startServerBtn;
        public GameObject stopServerBtn;

        public GameObject startHostBtn;
        public GameObject stopHostBtn;

        public GameObject stopClientBtn;

        public GameObject serverList;

        public Button[] servers;


        private void Start()
        {
            if(!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active && NetworkManager.singleton != null)
            {
                findServerBtn.SetActive(true);
                startHostBtn.SetActive(true);
                startServerBtn.SetActive(true);
            }
        }

        private void Update()
        {
            if(!NetworkClient.isConnected && !NetworkServer.active && !NetworkClient.active)
            {
                findServerBtn.SetActive(true);
                startHostBtn.SetActive(true);
                stopHostBtn.SetActive(false);
                startServerBtn.SetActive(true);
                stopServerBtn.SetActive(false);
                stopClientBtn.SetActive(false);
            }
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            discoveredServers[info.serverId] = info;
        }

        public void OnFindServerBtn()
        {
            discoveredServers.Clear();
            networkDiscovery.StartDiscovery();

            StartCoroutine(AddServers());
        }

        private IEnumerator AddServers()
        {
            while(true)
            {

                serverList.SetActive(true);
                int i = 0;

                foreach(var info in discoveredServers.Values)
                {
                    servers[i].GetComponentInChildren<Text>().text = info.EndPoint.Address.ToString();
                    servers[i].onClick.AddListener(() =>
                    {
                        Connect(info);
                    });
                    i++;
                }
                yield return null;
            }
        }

        private void Connect(ServerResponse info)
        {
            StopAllCoroutines();

            findServerBtn.SetActive(false);
            startHostBtn.SetActive(false);
            startServerBtn.SetActive(false);

            stopHostBtn.SetActive(false);
            stopServerBtn.SetActive(false);
            stopClientBtn.SetActive(true);

            serverList.SetActive(false);

            networkDiscovery.StopDiscovery();

            NetworkManager.singleton.StartClient(info.uri);
        }

        public void OnStartHostBtn()
        {
            findServerBtn.SetActive(false);
            startHostBtn.SetActive(false);
            startServerBtn.SetActive(false);
            stopServerBtn.SetActive(false);
            stopClientBtn.SetActive(false);

            discoveredServers.Clear();
            NetworkManager.singleton.StartHost();
            networkDiscovery.AdvertiseServer();

            if(NetworkServer.active || NetworkClient.active)
            {
                if(NetworkServer.active && NetworkClient.isConnected)
                {
                    stopHostBtn.SetActive(true);
                }
            }
        }

        public void OnStartServerBtn()
        {
            findServerBtn.SetActive(false);
            startHostBtn.SetActive(false);
            startServerBtn.SetActive(false);
            stopHostBtn.SetActive(false);
            stopClientBtn.SetActive(false);

            discoveredServers.Clear();
            NetworkManager.singleton.StartServer();
            networkDiscovery.AdvertiseServer();

            if(NetworkServer.active || NetworkClient.active)
            {
                if(NetworkServer.active && !NetworkClient.isConnected)
                {
                    stopServerBtn.SetActive(true);
                }
            }
        }

        public void OnStopServer()
        {
            findServerBtn.SetActive(true);
            startHostBtn.SetActive(true);
            stopHostBtn.SetActive(false);
            startServerBtn.SetActive(true);
            stopServerBtn.SetActive(false);
            stopClientBtn.SetActive(false);

            NetworkManager.singleton.StopServer();
            networkDiscovery.StopDiscovery();
        }

        public void OnStopHost()
        {
            findServerBtn.SetActive(true);
            startHostBtn.SetActive(true);
            stopHostBtn.SetActive(false);
            startServerBtn.SetActive(true);
            stopServerBtn.SetActive(false);
            stopClientBtn.SetActive(false);

            NetworkManager.singleton.StopHost();
            networkDiscovery.StopDiscovery();
        }

        public void OnStopClient()
        {
            findServerBtn.SetActive(true);
            startHostBtn.SetActive(true);
            stopHostBtn.SetActive(false);
            startServerBtn.SetActive(true);
            stopServerBtn.SetActive(false);
            stopClientBtn.SetActive(false);

            NetworkManager.singleton.StopClient();
            networkDiscovery.StopDiscovery();
        }
    }
}


