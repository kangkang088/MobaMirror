using Mirror;
using MirrorExample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace UltraReal.MobaMovement
{
    public class NetPlayerControl : NetworkBehaviour
    {
        public List<GameObject> avatars = new();

        private RaycastHit hitInfo;

        public GameObject fireball;

        public GameObject fireballInHands;

        public Transform castPoint;

        public SpriteRenderer spriteRenderer;

        public Color ownColor;
        public Color enemyColor;

        private float magicCD = 1f;
        private float residueCD;

        private float controlSkillCD = 6f;
        private float controlSkillResidueCD = 0f;

        public HealthyBarControl healthyBar;

        private bool isDead = false;

        private bool isDeadInServer = false;

        public bool isControled = false;

        public GameObject debuff;

        private enum E_Avatar
        {
            Alice, Eagle, Fox
        }

        private E_Avatar currentAvatar;

        private float aliceSpeed = 10f;
        private float eagleSpeed = 20f;
        private float foxSpeed = 10f;

        //SyncVar:同步变量。该变量只能在服务端改变，并通知给客户端
        //hook = ：当同步变量改变时，所有客户端执行hook绑定的函数（该函数在客户端执行）
        [SyncVar(hook = nameof(TellClientsWound))]
        public int healthy;

        private void Start()
        {
            if(isLocalPlayer)//如果是本地角色，就激活移动，否则，不激活。
            {
                GetComponent<NavMeshAgent>().enabled = true;

                GetComponent<MobaMover>().enabled = true;

                GetComponent<MobaAnimate>().enabled = true;

                UIController.Instance.UpdateHealthy(healthy);

                spriteRenderer.color = ownColor;

                healthyBar.SetColor(ownColor);

                residueCD = -1f;
                controlSkillResidueCD = -1f;
            }
            else
            {
                spriteRenderer.color = enemyColor;

                healthyBar.SetColor(enemyColor);
            }
        }

        private void Update()
        {
            if(!isLocalPlayer)
                return;

            residueCD -= Time.deltaTime;
            controlSkillResidueCD -= Time.deltaTime;

            if(controlSkillResidueCD > 0)
                UIController.Instance.ShowTip(((int)controlSkillResidueCD).ToString());

            if(isDead || isControled)
                return;

            KeyboardAbouting();

            MouseRaycast();

            OpenChest();
        }

        /// <summary>
        /// 开箱
        /// </summary>
        private void OpenChest()
        {
            if(Input.GetMouseButton(0) && hitInfo.collider.gameObject.tag == "Chest")
            {
                float distance = Vector3.Distance(transform.position,hitInfo.collider.transform.position);
                if(distance < 1.5f)
                {
                    if(hitInfo.collider.GetComponent<ChestControl>().isNew)
                        //告诉服务器开箱
                        OpenChestInServer(hitInfo.collider.gameObject);
                }
            }
        }

        /// <summary>
        /// 服务器开箱
        /// </summary>
        /// <param name="chest"></param>
        [Command]
        private void OpenChestInServer(GameObject chest)
        {
            chest.GetComponent<ChestControl>()?.OpenChest();
            //通知客户端开箱
            TellClientsOpenChest(chest);
            //延时加金币
            StartCoroutine(WillAddScore(3.1f,100));

        }

        private IEnumerator WillAddScore(float delayTime,int score)
        {
            yield return new WaitForSeconds(delayTime);
            TargetClientAddScore(GetComponent<NetworkIdentity>().connectionToClient,score);
        }

        /// <summary>
        /// 客户端开箱
        /// </summary>
        /// <param name="chest"></param>
        [ClientRpc]
        private void TellClientsOpenChest(GameObject chest)
        {
            chest.GetComponent<ChestControl>()?.OpenChest();
        }

        /// <summary>
        /// 鼠标射线检测
        /// </summary>
        private void MouseRaycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray,out hitInfo);
        }

        /// <summary>
        /// 键盘按键相关
        /// </summary>
        private void KeyboardAbouting()
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                //告诉server，我们要变身了。
                TellServerChangeToModel(0);
            }
            if(Input.GetKeyDown(KeyCode.W))
            {
                //告诉server，我们要变身了。
                TellServerChangeToModel(1);
            }
            if(Input.GetKeyDown(KeyCode.E))
            {
                //告诉server，我们要变身了。
                TellServerChangeToModel(2);
            }
            if(Input.GetKeyDown(KeyCode.A) && residueCD <= 0 && currentAvatar == E_Avatar.Alice)
            {
                //防止施法时未到指定地点，施法完成后继续移动。
                GetComponent<NavMeshAgent>().destination = transform.position;
                GetComponent<NavMeshAgent>().isStopped = true;

                transform.LookAt(new Vector3(hitInfo.point.x,transform.position.y,hitInfo.point.z));

                //让服务端对应角色释放魔法攻击
                TellServerMagicAttack(hitInfo.point);

                residueCD = magicCD;
            }
            if(Input.GetKeyDown(KeyCode.A) && hitInfo.collider != null && hitInfo.collider.gameObject.tag == "Player" && currentAvatar == E_Avatar.Fox && controlSkillResidueCD < 0)
            {
                if(hitInfo.collider.transform.parent.gameObject != gameObject)
                {
                    TellServerCharartorBeControled(hitInfo.collider.transform.parent.gameObject);
                    controlSkillResidueCD = controlSkillCD;
                }
            }
        }

        /// <summary>
        /// 服务器角色被控制
        /// </summary>
        /// <param name="player"></param>
        [Command]
        private void TellServerCharartorBeControled(GameObject player)
        {
            TellClientsPlayAttackAnimation();

            player.GetComponent<NetPlayerControl>().isControled = true;
            player.GetComponent<NetPlayerControl>().debuff.SetActive(true);

            TellClientsCharactorBeControled(player);
        }

        /// <summary>
        /// 客户端角色被控制
        /// </summary>
        /// <param name="player"></param>
        [ClientRpc]
        private void TellClientsCharactorBeControled(GameObject player)
        {
            player.GetComponent<MobaAnimate>()._animator.SetTrigger("Control");
            player.GetComponent<NetPlayerControl>().isControled = true;
            player.GetComponent<NetPlayerControl>().debuff.SetActive(true);
            player.GetComponent<NavMeshAgent>().enabled = false;
            StartCoroutine(RecoverState(player,5f));
        }

        /// <summary>
        /// 恢复控制状态
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recoverTime"></param>
        /// <returns></returns>
        private IEnumerator RecoverState(GameObject player,float recoverTime)
        {
            yield return new WaitForSeconds(recoverTime);
            player.GetComponent<NetPlayerControl>().isControled = false;
            player.GetComponent<NetPlayerControl>().debuff.SetActive(false);
            player.GetComponent<NavMeshAgent>().enabled = true;
        }

        /// <summary>
        /// 服务器角色进行攻击
        /// </summary>
        /// <param name="hitPoint"></param>
        /// <param name="sender"></param>
        [Command]
        private void TellServerMagicAttack(Vector3 hitPoint,NetworkConnectionToClient sender = null)
        {
            TellClientsPlayAttackAnimation();
            //等待转身和抬手动作完成后，再发射
            StartCoroutine(WaitForMagicAttack(0.65f,hitPoint,sender));
        }

        /// <summary>
        /// 客户端角色播放攻击动画
        /// </summary>
        [ClientRpc]
        private void TellClientsPlayAttackAnimation()
        {
            if(currentAvatar == E_Avatar.Alice)
                fireballInHands.SetActive(true);

            GetComponent<MobaAnimate>()._animator.SetTrigger("AttackA");
        }

        private IEnumerator WaitForMagicAttack(float delayTime,Vector3 hitPoint,NetworkConnectionToClient sender)
        {
            yield return new WaitForSeconds(delayTime);

            Vector3 dir = (hitPoint - castPoint.position).normalized;

            GameObject obj = Instantiate(fireball,castPoint.position,castPoint.rotation);

            TellClientsHideFireballInHand();

            obj.GetComponent<Fireball>().Init(dir,sender);

            //体现到所有客户端上
            NetworkServer.Spawn(obj);
        }

        /// <summary>
        /// 客户端角色隐藏手中攻击球
        /// </summary>
        [ClientRpc]
        private void TellClientsHideFireballInHand()
        {
            fireballInHands.SetActive(false);
            if(isLocalPlayer)
                GetComponent<NavMeshAgent>().isStopped = false;
        }

        /// <summary>
        /// 服务器变身
        /// </summary>
        /// <param name="index"></param>
        [Command]//client->server.在server上运行
        private void TellServerChangeToModel(int index)
        {
            //完成变身
            for(int i = 0;i < avatars.Count;i++)
            {
                avatars[i].SetActive(false);
            }

            avatars[index].SetActive(true);

            GetComponent<MobaAnimate>()._animator = avatars[index].GetComponent<Animator>();

            GetComponent<NetworkAnimator>().animator = avatars[index].GetComponent<Animator>();


            switch(index)
            {
                case 0:
                    currentAvatar = E_Avatar.Alice;
                    break;
                case 1:
                    currentAvatar = E_Avatar.Eagle;
                    break;
                case 2:
                    currentAvatar = E_Avatar.Fox;
                    break;
            }
            //让所有客户端上的预设体副本变身
            TellClientsChangeToModel(index);
        }

        /// <summary>
        /// 客户端变身
        /// </summary>
        /// <param name="index"></param>
        [ClientRpc]//server->client.在client上运行
        private void TellClientsChangeToModel(int index)
        {
            //完成变身
            for(int i = 0;i < avatars.Count;i++)
            {
                avatars[i].SetActive(false);
            }

            avatars[index].SetActive(true);

            GetComponent<MobaAnimate>()._animator = avatars[index].GetComponent<Animator>();

            GetComponent<NetworkAnimator>().animator = avatars[index].GetComponent<Animator>();

            switch(index)
            {
                case 0:
                    currentAvatar = E_Avatar.Alice;
                    GetComponent<NavMeshAgent>().speed = aliceSpeed;
                    break;
                case 1:
                    currentAvatar = E_Avatar.Eagle;
                    GetComponent<NavMeshAgent>().speed = eagleSpeed;
                    break;
                case 2:
                    currentAvatar = E_Avatar.Fox;
                    GetComponent<NavMeshAgent>().speed = foxSpeed;
                    break;
            }
        }

        /// <summary>
        /// 触发检测，是否碰到金币和加分
        /// </summary>
        /// <param name="other"></param>
        public void OnTriggerEnter(Collider other)
        {
            //本地控制本地UI，不应该每个本地都控制server的UI，因为server本身其实也是一个客户端，一个客户端应该自己控制自己，不应该受其他客户端控制
            //但是这样不公平，可能某一个客户端会使用其他程序修改计分。
            //还有，当我们在广域网时，会有网络延迟，当延迟很高时，客户端1已经碰撞吃掉了，但是客户端2的界面中依然存在，它过去又吃掉了同一个球。这样，一个球发生两次加分，就会出错。
            //if(other.gameObject.tag.Equals("Gold") && isLocalPlayer)

            //通过服务端去判断谁碰到了球加分。
            if(other.gameObject.tag.Equals("Gold") && isServer)
            {
                //因为客户端的球本身就是服务端的副本，当服务端销毁时，自然就会反应到所有客户端上
                Destroy(other.gameObject);

                //服务端看到一个球被某个客户端对象销毁了，通过该对象判断是哪个客户端销毁的，并给该客户端进行加分
                TargetClientAddScore(GetComponent<NetworkIdentity>().connectionToClient,1);
            }
        }

        /// <summary>
        /// 客户端加分
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="num"></param>
        [TargetRpc]
        private void TargetClientAddScore(NetworkConnection connection,int num)
        {
            UIController.Instance.AddGold(num);
        }

        [ServerCallback]//回调函数，服务器上碰撞检测
        public void OnCollisionEnter(Collision other)
        {
            if(other.gameObject.tag == "Fireball")
            {
                if(GetComponent<NetworkIdentity>().connectionToClient == other.gameObject.GetComponent<Fireball>().owner)
                    return;

                if(healthy > 0)
                {
                    TellServerPlayerWound();
                }
                if(healthy == 0)
                {
                    if(!isDeadInServer)
                        TargetClientAddScore(other.gameObject.GetComponent<Fireball>().owner,50);
                    isDeadInServer = true;
                }
                other.collider.GetComponent<Fireball>().Explode(other.contacts[0]);

                Destroy(other.gameObject);
            }
        }

        /// <summary>
        /// 客户端角色受伤
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private void TellClientsWound(int oldValue,int newValue)
        {
            if(isControled)
            {
                isControled = false;
                GetComponent<NavMeshAgent>().enabled = true;
                debuff.SetActive(false);
            }

            if(isLocalPlayer)
            {
                UIController.Instance.UpdateHealthy(healthy);
                PostProControl.Instance.Hurted(1f);
            }
            if(healthy > 0)
            {
                healthyBar.SetHealthy(healthy);

                GetComponent<MobaAnimate>()._animator.SetTrigger("Wound");
            }
            else
            {
                if(currentAvatar == E_Avatar.Eagle)
                {
                    avatars[1].GetComponent<Rigidbody>().useGravity = true;
                    avatars[1].GetComponent<Rigidbody>().isKinematic = false;
                    Invoke(nameof(EagleRecoverPhysicsState),0.6f);
                }

                healthyBar.SetHealthy(healthy);

                GetComponent<MobaAnimate>()._animator.SetTrigger("Dead");
            }
            if(healthy == 0 && isLocalPlayer)
            {
                GetComponent<NavMeshAgent>().isStopped = true;

                isDead = true;

                UIController.Instance.ShowTip("Please press R to recover your healthy!");

                PostProControl.Instance.Dead();
            }
        }

        /// <summary>
        /// 恢复物理状态
        /// </summary>
        private void EagleRecoverPhysicsState()
        {
            avatars[1].GetComponent<Rigidbody>().isKinematic = true;
        }

        /// <summary>
        /// 销毁角色
        /// </summary>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        private IEnumerator DestroyCharacter(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            TargetCharactorDead(GetComponent<NetworkIdentity>().connectionToClient);
            Destroy(gameObject);
        }

        /// <summary>
        /// 对应目标客户端角色死亡
        /// </summary>
        /// <param name="connectionToClient"></param>
        [TargetRpc]
        private void TargetCharactorDead(NetworkConnection connectionToClient)
        {
            GameManager.Instance.localPlayerIsDead = true;
        }

        /// <summary>
        /// 服务器角色
        /// </summary>
        [Server]
        internal void TellServerPlayerWound()
        {
            healthy--;
            healthyBar.SetHealthy(healthy);

            if(healthy == 0)
            {
                StartCoroutine(DestroyCharacter(3f));
            }
        }
    }
}