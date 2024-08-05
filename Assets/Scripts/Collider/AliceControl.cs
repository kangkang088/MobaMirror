using Mirror;
using UltraReal.MobaMovement;
using UnityEngine;

namespace MirrorExample
{
    public class AliceControl : MonoBehaviour
    {
        [ServerCallback]
        private void OnCollisionEnter(Collision collision)
        {
            GetComponentInParent<NetPlayerControl>().OnCollisionEnter(collision);
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            GetComponentInParent<NetPlayerControl>().OnTriggerEnter(other);
        }
    }
}