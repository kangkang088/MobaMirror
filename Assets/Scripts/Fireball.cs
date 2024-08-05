using Mirror;
using UnityEngine;

namespace MirrorExample
{
    public class Fireball : MonoBehaviour
    {
        private float moveSpeed = 1000f;

        private Vector3 direction;

        private Rigidbody rigidBody;

        public NetworkConnectionToClient owner;

        public GameObject explode;

        internal void Init(Vector3 direction,NetworkConnectionToClient sender)
        {
            this.direction = direction;

            owner = sender;

            rigidBody = GetComponent<Rigidbody>();
            rigidBody.AddForce(direction * moveSpeed);

            Destroy(gameObject,1.5f);
        }

        [Server]
        internal void Explode(ContactPoint contact)
        {
            GameObject effect = Instantiate(explode,contact.point,Quaternion.identity);
            NetworkServer.Spawn(effect);

            Destroy(effect,1f);
        }
    }
}
