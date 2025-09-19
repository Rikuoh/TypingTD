using UnityEngine;

// 縦移動でも回転しない：GFXの「ワールド回転」を固定し、
// 横に動く時だけ左右反転（flipX）を切り替える。
public class SpriteLRWorldLocked2D : MonoBehaviour
{
    public Rigidbody2D rb;            // 親のRigidbody2D（なくてもOK）
    public SpriteRenderer sr;         // GFXのSpriteRenderer

    [Header("Facing")]
    public bool rightIsFlipX = true;  // 右に進む時にflipXをONにする？（左向き素材なら true）
    public bool defaultFaceRight = false; // 縦/停止時の既定向き（true=右, false=左）
    public float deadX = 0.03f;       // 横速度がこれ未満なら向きを更新しない

    [Header("Rotation Lock (world space)")]
    public float worldZ = 0f;         // GFXのワールド回転をこの角度に固定（0/90/-90など）

    Vector2 _prevPos;
    bool _inited;
    bool _faceRight;

    void Reset(){
        rb = GetComponentInParent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (!sr) return;

        // 1) ワールド回転を完全固定（親が回ってもGFXは回らない）
        transform.rotation = Quaternion.Euler(0f, 0f, worldZ);

        // 2) 横の動きだけで左右を決める（rb.velocityが無ければ位置差分で推定）
        Vector2 pos = rb ? rb.position : (Vector2)transform.parent.position;
        Vector2 v;

        if (rb && rb.velocity.sqrMagnitude > 1e-6f) {
            v = rb.velocity;
        } else {
            if (!_inited) { _prevPos = pos; _inited = true; _faceRight = defaultFaceRight; return; }
            Vector2 delta = pos - _prevPos; _prevPos = pos;
            v = (Time.deltaTime > 0f) ? (delta / Time.deltaTime) : delta;
        }

        if (Mathf.Abs(v.x) >= deadX) _faceRight = v.x > 0f;

        // 3) 反映（右進行時のflipXの有無は rightIsFlipX で校正）
        sr.flipX = _faceRight ? rightIsFlipX : !rightIsFlipX;
    }

    // 念のため：描画直前にももう一度ロック（他スクリプトの後書きを無効化）
    void OnWillRenderObject()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, worldZ);
    }
}
