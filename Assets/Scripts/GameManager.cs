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
                    //�ѷ���˵Ķ���ͬ��һ���������ŵ��ͻ���
                    NetworkServer.Spawn(Instantiate(gold,golds[i].position,golds[i].rotation));
                }
                for(int i = 0;i < chests.Count;i++)
                {
                    //�ѷ���˵Ķ���ͬ��һ���������ŵ��ͻ���
                    NetworkServer.Spawn(Instantiate(chest,chests[i].position,chests[i].rotation));
                }
                for(int i = 0;i < dragons.Count;i++)
                {
                    //�ѷ���˵Ķ���ͬ��һ���������ŵ��ͻ���
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
            //�����ɫ
            NetworkClient.AddPlayer();
            localPlayerIsDead = false;

            PostProControl.Instance.Rebirth();

            UIController.Instance.HideTip();
        }
    }
}


