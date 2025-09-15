using UnityEngine;

namespace TD.TDCore
{
    public class Enemy : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<Enemy> Alive = new();

        public float maxHp = 10f;
        public float moveSpeed = 2f;
        public WaypointPath path;

        [Header("FX")]
        public ParticleSystem deathExplosionPrefab; // ← 爆発プレハブをInspectorで割当

        private float _hp;
        private int _wpIndex;

        private void OnEnable()
        {
            Alive.Add(this);
            _hp = maxHp;
        }
        private void OnDisable()
        {
            Alive.Remove(this);
        }

        private void Update()
        {
            if (path == null || path.Count == 0) return;
            Vector3 target = path.GetPoint(_wpIndex);
            Vector3 dir = (target - transform.position);
            float dist = dir.magnitude;
            float step = moveSpeed * Time.deltaTime;
            if (dist <= step)
            {
                _wpIndex++;
                if (_wpIndex >= path.Count)
                {
                    // ゴール到達: 今は破棄
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                transform.position += dir.normalized * step;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
            }
        }

        private bool _isDead = false;
        public void TakeDamage(float dmg)
        {
            if (_isDead) return;   // すでに死んでたら何もしない
            
            _hp -= dmg;
            if (_hp <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;   // 念のため二重チェック
            _isDead = true;
            if (deathExplosionPrefab != null)
            {
                var fx = Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 1f); // 1秒後に削除
            }

            Destroy(gameObject);
        }
    }
}