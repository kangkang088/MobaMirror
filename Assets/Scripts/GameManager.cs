using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace MirrorExample
{
    public class GameManager : NetworkBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance => instance;

        public GameObject gold;
        public GameObject chest;
        public GameObject dragon;

        public List<Transform> golds;
        public List<Transform> chests;
        public List<Transform> dragons;

        public bool localPlayerIsDead = false;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            if(isServer)
            {
                for(int i = 0;i < golds.Count;i++)
                {
                    //把服务端的对象，同步一个副本，放到客户端
                    NetworkServer.Spawn(Instantiate(gold,golds[i].position,golds[i].rotation));
                }
                for(int i = 0;i < chests.Count;i++)
                {
                    //把服务端的对象，同步一个副本，放到客户端
                    NetworkServer.Spawn(Instantiate(chest,chests[i].position,chests[i].rotation));
                }
                for(int i = 0;i < dragons.Count;i++)
                {
                    //把服务端的对象，同步一个副本，放到客户端
                    NetworkServer.Spawn(Instantiate(dragon,dragons[i].position,dragons[i].rotation));
                }
            }
        }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.R) && localPlayerIsDead)
            {
                RecoverCharactor();
            }
        }

        private void RecoverCharactor()
        {
            //复活角色
            NetworkClient.AddPlayer();
            localPlayerIsDead = false;

            PostProControl.Instance.Rebirth();

            UIController.Instance.HideTip();
        }
    }
}


