using UnityEngine;

[RequireComponent(typeof(EnemyMover2D))]
public class EnemyHealth2D : MonoBehaviour
{
    public float maxHP = 10f;
    public float knockbackForce = 3.5f;

    float hp;
    EnemyMover2D mover;

    void Awake()
    {
        hp = maxHP;
        mover = GetComponent<EnemyMover2D>();
    }

    // 弾などから呼ぶ想定
    public void TakeHit(float damage, Vector2 hitFromPosition)
    {
        hp -= damage;
        mover.ApplyKnockback(hitFromPosition, knockbackForce);

        if (hp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // エフェクトやスコア加算など
        Destroy(gameObject);
    }
}
