using UnityEngine;
using System.Linq;

namespace TD.TDCore
{
    public class Tower : MonoBehaviour
    {
        public float range = 5f;
        public float fireRate = 1.0f; // shot/sec
        public float damage = 3f;
        public Projectile projectilePrefab;
        public Transform muzzle;
        public ParticleSystem muzzleSparkPrefab; // ← Tower.prefab の Inspector で FX_HitSpark を割り当てる

        [Header("Level")]
        public int level = 1;
        public float fireRatePerLevel = 0.2f; // +0.2
        public float damagePerLevel = 1f;     // +1
        public float rangePerLevel = 0.5f;    // +0.5

        private float _cooldown;

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            var target = AcquireTarget();
            if (target != null)
            {
                Shoot(target);
                _cooldown = 1f / fireRate;
            }
        }

        private Enemy AcquireTarget()
        {
            if (Enemy.Alive.Count == 0) return null;
            var selfPos = transform.position;
            float r2 = range * range;
            // 単純: 近い敵を選択
            return Enemy.Alive
                .Where(e => e != null)
                .OrderBy(e => (e.transform.position - selfPos).sqrMagnitude)
                .FirstOrDefault(e => (e.transform.position - selfPos).sqrMagnitude <= r2);
        }

        private void Shoot(Enemy target)
        {
            // ★ 火花を出す（最初に）
            if (muzzleSparkPrefab != null && muzzle != null)
            {
                var fx = Instantiate(muzzleSparkPrefab, muzzle.position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 1f);
            }

            if (projectilePrefab == null || muzzle == null) return;
            var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            proj.damage = damage;
            proj.target = target;
        }

        public void Upgrade()
        {
            level++;
            fireRate += fireRatePerLevel;
            damage += damagePerLevel;
            range += rangePerLevel;
        }
    }
}