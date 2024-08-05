using UnityEngine;
using UnityEngine.UI;

namespace MirrorExample
{
    public class UIController : MonoBehaviour
    {
        private static UIController instance;

        public static UIController Instance => instance;

        public Text textGold;
        public Text textHealthy;

        public Text textTip;

        private int count;

        private void Awake()
        {
            instance = this;
        }

        public void AddGold(int num)
        {
            count += num;

            textGold.text = "Gold:" + count;
        }

        internal void UpdateHealthy(int num)
        {
            textHealthy.text = "Healthy:" + num;
        }

        internal void ShowTip(string tips)
        {
            textTip.text = tips;

            textTip.gameObject.SetActive(true);
        }

        internal void HideTip()
        {
            textTip.gameObject.SetActive(false);
        }
    }
}
