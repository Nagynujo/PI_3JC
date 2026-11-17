
using UnityEngine;

public class PotionColorController : MonoBehaviour
{
    [Header("Objetos da poção")]
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private ParticleSystem bubbleParticles;

    private Material liquidMaterial;

    private void Awake()
    {
        if (liquidRenderer != null)
            liquidMaterial = liquidRenderer.material;
    }

    public void SetPotionColor(Color color)
    {
        if (liquidMaterial != null)
        {
            liquidMaterial.SetColor("_BaseColor", color);

            liquidMaterial.SetColor(
                "_SwirlColor",
                Color.Lerp(color, Color.white, 0.35f)
            );

            liquidMaterial.SetColor(
                "_DeepColor",
                Color.Lerp(color, Color.black, 0.4f)
            );

            liquidMaterial.SetColor(
                "_BubbleColor",
                Color.Lerp(color, Color.white, 0.5f)
            );

            liquidMaterial.SetColor(
                "_GlowColor",
                Color.Lerp(color, Color.white, 0.7f)
            );
        }

        if (bubbleParticles != null)
        {
            var main = bubbleParticles.main;

            main.startColor = Color.Lerp(
                color,
                Color.white,
                0.4f
            );
        }
    }

    public void SetHealingPotion()
    {
        SetPotionColor(new Color(0.4f, 0.95f, 0.6f));
    }

    public void SetManaPotion()
    {
        SetPotionColor(new Color(0.35f, 0.65f, 1f));
    }

    public void SetLovePotion()
    {
        SetPotionColor(new Color(1f, 0.45f, 0.75f));
    }
}
