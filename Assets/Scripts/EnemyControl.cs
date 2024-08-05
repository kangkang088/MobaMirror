using Mirror;
using System;
using System.Collections;
using UltraReal.MobaMovement;
using UnityEngine;
using UnityEngine.AI;

namespace MirrorExample
{
    public class EnemyControl : NetworkBehaviour
    {
        private Animator _animator;

        private NavMeshAgent _agent;

        public float attackDistance = 8f;

        private GameObject attackTarget;

        private bool isDeadInServer = false;

        public ParticleSystem flame;

        [SyncVar(hook = nameof(TellClientsEnemiesWound))]
        public int healthy;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
        }

        [ServerCallback]
        private void OnCollisionEnter(Collision other)
        {
            if(other.gameObject.tag == "Fireball")
            {
                if(GetComponent<NetworkIdentity>().connectionToClient == other.gameObject.GetComponent<Fireball>().owner)
                    return;

                if(healthy > 0)
                {
                    healthy--;

                    attackTarget = other.gameObject.GetComponent<Fireball>().owner.identity.gameObject;

                    StopAllCoroutines();//避免玩家被攻击一次会扣两次血

                    GetComponent<NavMeshAgent>().isStopped = true;

                    StartCoroutine(WillKillPlayer(attackTarget,1f));
                }
                if(healthy == 0)
                {
                    GetComponent<NavMeshAgent>().isStopped = true;

                    StopAllCoroutines();

                    Destroy(gameObject,5f);

                    if(!isDeadInServer)
                        TargetClientAddScore(other.gameObject.GetComponent<Fireball>().owner,50);
                    isDeadInServer = true;
                }

                other.collider.GetComponent<Fireball>().Explode(other.contacts[0]);

                Destroy(other.gameObject);
            }
        }

        /// <summary>
        /// 追杀玩家
        /// </summary>
        /// <param name="attackTarget"></param>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private IEnumerator WillKillPlayer(GameObject attackTarget,float delayTime)
        {
            yield return new WaitForSeconds(delayTime);

            _agent.isStopped = false;

            while(Vector3.Distance(transform.position,attackTarget.transform.position) > attackDistance)
            {
                _agent.destination = attackTarget.transform.position;
                _animator.SetFloat("Forward",_agent.speed);
                yield return null;
            }
            _agent.isStopped = true;
            _animator.SetFloat("Forward",0);

            StartCoroutine(AttackPlayer(attackTarget,Vector3.Distance(transform.position,attackTarget.transform.position),0.8f));
        }

        /// <summary>
        /// 攻击玩家
        /// </summary>
        /// <param name="attackTarget"></param>
        /// <param name="delayTime"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private IEnumerator AttackPlayer(GameObject attackTarget,float distance,float delayTime)
        {
            transform.LookAt(attackTarget.transform.position);

            if(4 < distance && distance < attackDistance)
            {
                TellClientsPlayAttackAnimation("AttackS");
                yield return new WaitForSeconds(1.5f);
            }
            else
            {
                TellClientsPlayAttackAnimation("AttackA");
                yield return new WaitForSeconds(delayTime);
            }

            attackTarget.GetComponent<NetPlayerControl>().TellServerPlayerWound();

            if(attackTarget.GetComponent<NetPlayerControl>().healthy > 0)
                StartCoroutine(WillKillPlayer(attackTarget,1f));
        }

        [ClientRpc]
        private void TellClientsPlayAttackAnimation(string triggerName)
        {
            _animator.SetTrigger(triggerName);
        }

        /// <summary>
        /// 目标客户的加分
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="score"></param>
        [TargetRpc]
        private void TargetClientAddScore(NetworkConnection owner,int score)
        {
            UIController.Instance.AddGold(score);
        }

        /// <summary>
        /// 客户端敌人受伤
        /// </summary>
        private void TellClientsEnemiesWound(int oldValue,int newValue)
        {
            if(healthy > 0)
            {
                _animator.SetTrigger("Wound");
            }
            else
            {
                _animator.SetTrigger("Dead");
            }
        }

        private void BeginFire()
        {
            flame.gameObject.SetActive(true);
            flame.Play();
        }

        private void StopFire()
        {
            flame.Stop();
        }
    }
}


