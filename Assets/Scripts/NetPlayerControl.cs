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

        //SyncVar:ͬ���������ñ���ֻ���ڷ���˸ı䣬��֪ͨ���ͻ���
        //hook = ����ͬ�������ı�ʱ�����пͻ���ִ��hook�󶨵ĺ������ú����ڿͻ���ִ�У�
        [SyncVar(hook = nameof(TellClientsWound))]
        public int healthy;

        private void Start()
        {
            if(isLocalPlayer)//����Ǳ��ؽ�ɫ���ͼ����ƶ������򣬲����
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
        /// ����
        /// </summary>
        private void OpenChest()
        {
            if(Input.GetMouseButton(0) && hitInfo.collider.gameObject.tag == "Chest")
            {
                float distance = Vector3.Distance(transform.position,hitInfo.collider.transform.position);
                if(distance < 1.5f)
                {
                    if(hitInfo.collider.GetComponent<ChestControl>().isNew)
                        //���߷���������
                        OpenChestInServer(hitInfo.collider.gameObject);
                }
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        /// <param name="chest"></param>
        [Command]
        private void OpenChestInServer(GameObject chest)
        {
            chest.GetComponent<ChestControl>()?.OpenChest();
            //֪ͨ�ͻ��˿���
            TellClientsOpenChest(chest);
            //��ʱ�ӽ��
            StartCoroutine(WillAddScore(3.1f,100));

        }

        private IEnumerator WillAddScore(float delayTime,int score)
        {
            yield return new WaitForSeconds(delayTime);
            TargetClientAddScore(GetComponent<NetworkIdentity>().connectionToClient,score);
        }

        /// <summary>
        /// �ͻ��˿���
        /// </summary>
        /// <param name="chest"></param>
        [ClientRpc]
        private void TellClientsOpenChest(GameObject chest)
        {
            chest.GetComponent<ChestControl>()?.OpenChest();
        }

        /// <summary>
        /// ������߼��
        /// </summary>
        private void MouseRaycast()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray,out hitInfo);
        }

        /// <summary>
        /// ���̰������
        /// </summary>
        private void KeyboardAbouting()
        {
            if(Input.GetKeyDown(KeyCode.Q))
            {
                //����server������Ҫ�����ˡ�
                TellServerChangeToModel(0);
            }
            if(Input.GetKeyDown(KeyCode.W))
            {
                //����server������Ҫ�����ˡ�
                TellServerChangeToModel(1);
            }
            if(Input.GetKeyDown(KeyCode.E))
            {
                //����server������Ҫ�����ˡ�
                TellServerChangeToModel(2);
            }
            if(Input.GetKeyDown(KeyCode.A) && residueCD <= 0 && currentAvatar == E_Avatar.Alice)
            {
                //��ֹʩ��ʱδ��ָ���ص㣬ʩ����ɺ�����ƶ���
                GetComponent<NavMeshAgent>().destination = transform.position;
                GetComponent<NavMeshAgent>().isStopped = true;

                transform.LookAt(new Vector3(hitInfo.point.x,transform.position.y,hitInfo.point.z));

                //�÷���˶�Ӧ��ɫ�ͷ�ħ������
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
        /// ��������ɫ������
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
        /// �ͻ��˽�ɫ������
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
        /// �ָ�����״̬
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
        /// ��������ɫ���й���
        /// </summary>
        /// <param name="hitPoint"></param>
        /// <param name="sender"></param>
        [Command]
        private void TellServerMagicAttack(Vector3 hitPoint,NetworkConnectionToClient sender = null)
        {
            TellClientsPlayAttackAnimation();
            //�ȴ�ת���̧�ֶ�����ɺ��ٷ���
            StartCoroutine(WaitForMagicAttack(0.65f,hitPoint,sender));
        }

        /// <summary>
        /// �ͻ��˽�ɫ���Ź�������
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

            //���ֵ����пͻ�����
            NetworkServer.Spawn(obj);
        }

        /// <summary>
        /// �ͻ��˽�ɫ�������й�����
        /// </summary>
        [ClientRpc]
        private void TellClientsHideFireballInHand()
        {
            fireballInHands.SetActive(false);
            if(isLocalPlayer)
                GetComponent<NavMeshAgent>().isStopped = false;
        }

        /// <summary>
        /// ����������
        /// </summary>
        /// <param name="index"></param>
        [Command]//client->server.��server������
        private void TellServerChangeToModel(int index)
        {
            //��ɱ���
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
            //�����пͻ����ϵ�Ԥ���帱������
            TellClientsChangeToModel(index);
        }

        /// <summary>
        /// �ͻ��˱���
        /// </summary>
        /// <param name="index"></param>
        [ClientRpc]//server->client.��client������
        private void TellClientsChangeToModel(int index)
        {
            //��ɱ���
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
        /// ������⣬�Ƿ�������Һͼӷ�
        /// </summary>
        /// <param name="other"></param>
        public void OnTriggerEnter(Collider other)
        {
            //���ؿ��Ʊ���UI����Ӧ��ÿ�����ض�����server��UI����Ϊserver������ʵҲ��һ���ͻ��ˣ�һ���ͻ���Ӧ���Լ������Լ�����Ӧ���������ͻ��˿���
            //������������ƽ������ĳһ���ͻ��˻�ʹ�����������޸ļƷ֡�
            //���У��������ڹ�����ʱ�����������ӳ٣����ӳٺܸ�ʱ���ͻ���1�Ѿ���ײ�Ե��ˣ����ǿͻ���2�Ľ�������Ȼ���ڣ�����ȥ�ֳԵ���ͬһ����������һ���������μӷ֣��ͻ����
            //if(other.gameObject.tag.Equals("Gold") && isLocalPlayer)

            //ͨ�������ȥ�ж�˭��������ӷ֡�
            if(other.gameObject.tag.Equals("Gold") && isServer)
            {
                //��Ϊ�ͻ��˵�������Ƿ���˵ĸ����������������ʱ����Ȼ�ͻᷴӦ�����пͻ�����
                Destroy(other.gameObject);

                //����˿���һ����ĳ���ͻ��˶��������ˣ�ͨ���ö����ж����ĸ��ͻ������ٵģ������ÿͻ��˽��мӷ�
                TargetClientAddScore(GetComponent<NetworkIdentity>().connectionToClient,1);
            }
        }

        /// <summary>
        /// �ͻ��˼ӷ�
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="num"></param>
        [TargetRpc]
        private void TargetClientAddScore(NetworkConnection connection,int num)
        {
            UIController.Instance.AddGold(num);
        }

        [ServerCallback]//�ص�����������������ײ���
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
        /// �ͻ��˽�ɫ����
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
        /// �ָ�����״̬
        /// </summary>
        private void EagleRecoverPhysicsState()
        {
            avatars[1].GetComponent<Rigidbody>().isKinematic = true;
        }

        /// <summary>
        /// ���ٽ�ɫ
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
        /// ��ӦĿ��ͻ��˽�ɫ����
        /// </summary>
        /// <param name="connectionToClient"></param>
        [TargetRpc]
        private void TargetCharactorDead(NetworkConnection connectionToClient)
        {
            GameManager.Instance.localPlayerIsDead = true;
        }

        /// <summary>
        /// ��������ɫ
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