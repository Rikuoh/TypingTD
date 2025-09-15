using UnityEngine;

namespace TD.TDCore
{
    public class Projectile : MonoBehaviour
    {
        public float speed = 8f;
        public float damage = 3f;
        public Enemy target;

        private void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 dir = target.transform.position - transform.position;
            float step = speed * Time.deltaTime;
            if (dir.magnitude <= step)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            transform.position += dir.normalized * step;
        }
    }
}