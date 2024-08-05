using UnityEngine;

namespace MirrorExample
{
    public class HealthyBarControl : MonoBehaviour
    {
        public SpriteRenderer[] hearts = new SpriteRenderer[] { };

        private void Update()
        {
            //������ʼ�ճ��������
            transform.LookAt(Camera.main.transform.position);
        }

        public void SetColor(Color color)
        {
            for(int i = 0;i < hearts.Length;i++)
            {
                hearts[i].color = color;
            }
        }

        public void SetHealthy(int currentHealthy)
        {
            if(currentHealthy > hearts.Length)
                return;
            for(int i = 0;i < hearts.Length;i++)
            {
                if(i > currentHealthy - 1)
                {
                    hearts[i].gameObject.SetActive(false);
                }
                else
                    hearts[i].gameObject.SetActive(true);
            }
        }
    }

}
