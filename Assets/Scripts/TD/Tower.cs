using UnityEngine;
using System.Linq;
using System.Collections;

namespace TD.TDCore
{
    /// <summary>
    /// タワー（単発＝緑スキン／強化＝赤スキン・二口）
    /// - 生成直後に一度だけスナップで敵へ向ける（初弾が上へ飛ばない）
    /// - 毎フレームで最も近い敵へ回転
    /// - 強化時は見た目切替＆二口発射（少し時間差で発射）
    /// - 強化時は連射速度が倍率で早くなる
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("Combat")]
        public float range = 5f;
        public float fireRate = 1.0f; // 実際に使用される現在値（毎秒発射回数）
        public float damage = 3f;
        public Projectile projectilePrefab;

        [Tooltip("通常（緑）時の単発マズル")]
        public Transform muzzle;

        [Tooltip("発射時のスパークFX（任意）")]
        public ParticleSystem muzzleSparkPrefab;

        [Header("Level")]
        public int level = 1;
        public float fireRatePerLevel = 0.2f; // +0.2
        public float damagePerLevel = 1f;     // +1
        public float rangePerLevel = 0.5f;    // +0.5

        [Header("Aiming")]
        [Tooltip("回転させる部分（Head）")]
        public Transform head;
        [Tooltip("1秒あたりの旋回速度（度）")]
        public float turnSpeedDeg = 720f;
        [Tooltip("スプライト基準のずれ（右=0°/上=+90° 等）。上向き素材なら +90 推奨")]
        public float spriteAngleOffset = 0f;
        [Tooltip("ある程度向いてから撃つか")]
        public bool fireOnlyWhenAimed = true;
        [Tooltip("この角度以内なら発射")]
        public float fireReadyAngle = 5f;

        [Header("Skins / Multi Muzzle")]
        [Tooltip("通常スキン（緑）")]
        [SerializeField] private GameObject skinGreen;
        [Tooltip("強化スキン（赤・二口）")]
        [SerializeField] private GameObject skinRed;
        [Tooltip("強化（赤）時に使う複数マズル。Size=2 で Muzzle_L / Muzzle_R を割当")]
        [SerializeField] private Transform[] muzzles;

        [Header("Power-up Settings")]
        [Tooltip("起動時から強化状態で開始する（デバッグ用）")]
        [SerializeField] private bool startPowered = false;
        [Tooltip("強化（赤）中の連射倍率（例：1.5 → 緑より1.5倍の連射）")]
        [SerializeField] private float poweredFireRateMultiplier = 1.5f;
        [Tooltip("強化（赤）中、マズル間の発射ディレイ（秒）。少しズラすなら 0.04〜0.12s 程度")]
        [SerializeField] private float muzzleStaggerSeconds = 0.06f;

        private float _cooldown;
        private Enemy _currentTarget;
        private bool _powered;

        // “素の連射速度”（緑基準）。レベルアップで増減し、強化ONで倍率が掛かる
        private float _unpoweredFireRate;

        //========================================
        // 初期化
        //========================================
        private void Start()
        {
            // 素のレートを覚える（プレハブの fireRate を基準化）
            _unpoweredFireRate = Mathf.Max(0.01f, fireRate);

            // 生成直後に一度だけスナップで向ける（初弾が上向きにならない）
            var t = AcquireTarget();
            SnapAimTo(t);

            // スキン＆レート初期化
            SetPowered(startPowered);
        }

        /// <summary>
        /// 一度だけ瞬時に敵の方向へ向ける（滑らか回転ではない）
        /// </summary>
        private void SnapAimTo(Enemy t)
        {
            if (!head || t == null) return;
            Vector3 dir = t.transform.position - head.position;
            float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + spriteAngleOffset;
            head.rotation = Quaternion.Euler(0, 0, targetZ);
        }

        //========================================
        // メインループ
        //========================================
        private void Update()
        {
            // ① ターゲット取得（毎フレーム）
            _currentTarget = AcquireTarget();

            // ② 回転（毎フレーム）
            if (head && _currentTarget != null)
            {
                Vector3 dir = _currentTarget.transform.position - head.position;
                float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + spriteAngleOffset;
                float z = Mathf.MoveTowardsAngle(head.eulerAngles.z, targetZ, turnSpeedDeg * Time.deltaTime);
                head.rotation = Quaternion.Euler(0, 0, z);
            }

            // ③ クールダウン更新
            _cooldown -= Time.deltaTime;

            // ④ 発射判定
            if (_currentTarget != null && _cooldown <= 0f)
            {
                if (fireOnlyWhenAimed && head)
                {
                    Vector3 dir = _currentTarget.transform.position - head.position;
                    float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f + spriteAngleOffset;
                    float diff = Mathf.DeltaAngle(head.eulerAngles.z, targetZ);
                    if (Mathf.Abs(diff) > fireReadyAngle) return; // まだ向けてない
                }

                Shoot(_currentTarget);
                _cooldown = 1f / Mathf.Max(0.01f, fireRate); // 次のCD
            }
        }

        //========================================
        // ターゲット探索
        //========================================
        private Enemy AcquireTarget()
        {
            if (Enemy.Alive.Count == 0) return null;
            var selfPos = transform.position;
            float r2 = range * range;

            // 射程内の最も近い敵
            return Enemy.Alive
                .Where(e => e != null)
                .OrderBy(e => (e.transform.position - selfPos).sqrMagnitude)
                .FirstOrDefault(e => (e.transform.position - selfPos).sqrMagnitude <= r2);
        }

        //========================================
        // 発射（強化時は二口を“わずかに時間差”で撃つ）
        //========================================
        private void Shoot(Enemy target)
        {
            if (projectilePrefab == null) return;

            var outs = (_powered && muzzles != null && muzzles.Length > 0)
                ? muzzles.Where(t => t != null).ToArray()
                : new Transform[] { muzzle };

            if (_powered && muzzleStaggerSeconds > 0.0001f && outs.Length > 1)
            {
                // 二口を少しずらして撃つ（CDはこの呼び出し1回分で管理）
                StartCoroutine(FireBurstStaggered(target, outs, muzzleStaggerSeconds));
            }
            else
            {
                // 通常（同時 or 単発）
                foreach (var mz in outs)
                {
                    if (mz == null) continue;
                    FireFromMuzzle(mz, target);
                }
            }
        }

        private IEnumerator FireBurstStaggered(Enemy target, Transform[] outs, float delay)
        {
            for (int i = 0; i < outs.Length; i++)
            {
                var mz = outs[i];
                if (mz) FireFromMuzzle(mz, target);
                if (i < outs.Length - 1)
                    yield return new WaitForSeconds(delay);
            }
        }

        private void FireFromMuzzle(Transform mz, Enemy target)
        {
            if (muzzleSparkPrefab != null)
            {
                var fx = Instantiate(muzzleSparkPrefab, mz.position, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, 1f);
            }

            var proj = Instantiate(projectilePrefab, mz.position, Quaternion.identity);
            proj.damage = damage;
            proj.target = target;
        }

        //========================================
        // レベルアップ（素のレートを更新して再適用）
        //========================================
        public void Upgrade()
        {
            level++;
            _unpoweredFireRate += fireRatePerLevel;      // 素レートを増加
            damage += damagePerLevel;
            range += rangePerLevel;
            ApplyFireRate();                              // 強化中なら倍率を掛け直す
        }

        //========================================
        // 強化（見た目＆発射口の切替＋連射倍率の適用）
        //========================================
        public void SetPowered(bool on)
        {
            _powered = on;

            // 見た目切替
            if (skinGreen) skinGreen.SetActive(!_powered);
            if (skinRed)   skinRed.SetActive(_powered);

            // 連射レート再計算
            ApplyFireRate();
        }

        public void PowerUpFor(float seconds)
        {
            StopCoroutine(nameof(PowerRoutine));
            StartCoroutine(PowerRoutine(seconds));
        }

        private IEnumerator PowerRoutine(float sec)
        {
            SetPowered(true);
            yield return new WaitForSeconds(sec);
            SetPowered(false);
        }

        private void ApplyFireRate()
        {
            fireRate = _powered
                ? _unpoweredFireRate * Mathf.Max(1f, poweredFireRateMultiplier)
                : _unpoweredFireRate;
        }

#if UNITY_EDITOR
        // ある程度の自動配線（名前が合っていれば拾う）
        private void OnValidate()
        {
            if (!head)
            {
                var h = transform.Find("Head");
                if (h) head = h;
            }
        }
#endif
    }
}
