using UnityEngine;

/// <summary>
/// 命中与死亡时调用的组合特效：
/// 柔光闪光 + 扩散圆环 + 向外飞散的小光点。
/// </summary>
public static class VFXBurst
{
    public static void PlayImpact(Vector3 position, Color color)
    {
        Color bright = Color.Lerp(color, Color.white, 0.55f);

        // 中心短促闪光
        EffectPulse.Create(VFXFactory.GlowSprite, bright, position, 0.35f, 0.7f, 0.12f);

        // 扩散圆环
        EffectPulse.Create(VFXFactory.RingSprite, bright, position, 0.4f, 1.4f, 0.24f);

        // 少量火花
        for (int i = 0; i < 5; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            EffectPulse.Create(
                VFXFactory.GlowSprite,
                color,
                position,
                Random.Range(0.09f, 0.14f),
                0f,
                Random.Range(0.16f, 0.24f),
                direction * Random.Range(2.2f, 4.2f),
                24);
        }
    }

    public static void PlayDeath(Vector3 position, Color color)
    {
        Color bright = Color.Lerp(color, Color.white, 0.6f);

        // 白色大圆环扩散
        EffectPulse.Create(VFXFactory.RingSprite, Color.white, position, 0.45f, 2.3f, 0.35f);

        // 敌人颜色的大柔光爆闪
        EffectPulse.Create(VFXFactory.GlowSprite, bright, position, 0.55f, 1.5f, 0.22f);

        // 大量飞散光点
        for (int i = 0; i < 10; i++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            EffectPulse.Create(
                VFXFactory.GlowSprite,
                color,
                position,
                Random.Range(0.07f, 0.14f),
                0f,
                Random.Range(0.28f, 0.5f),
                direction * Random.Range(3f, 6.5f),
                24);
        }
    }
}
