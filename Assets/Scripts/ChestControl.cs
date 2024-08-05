using UnityEngine;

namespace MirrorExample
{
    public class ChestControl : MonoBehaviour
    {
        private Animator _animator;
        public bool isNew = true;
        public ParticleSystem coinRain;
        public GameObject coins;

        void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public void OpenChest()
        {
            _animator.SetTrigger("Open");
            isNew = false;
        }

        public void CoinRain()
        {
            coinRain.Play();
            Invoke(nameof(DestroyCoins),1.9f);
        }

        private void DestroyCoins()
        {
            Destroy(coins);
        }
    }
}
