using UnityEngine;

/// <summary>
/// 敌人死亡时的碎片反馈：向外飞散并逐渐透明消失。
/// 由 Enemy.SpawnDeathBurst() 创建，不需要额外美术资源。
/// </summary>
public sealed class DeathShard : MonoBehaviour
{
    private Vector2 velocity;
    private float remainingLife;
    private float initialLife = 1f;
    private SpriteRenderer spriteRenderer;

    public void Launch(Vector2 speed, float life)
    {
        velocity = speed;
        remainingLife = life;
        initialLife = Mathf.Max(0.01f, life);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        remainingLife -= Time.deltaTime;

        if (remainingLife <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Clamp01(remainingLife / initialLife);
            spriteRenderer.color = color;
        }
    }
}
