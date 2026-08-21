using UnityEngine;

[System.Serializable]
public class Order
{
    public PotionType tipo;
    public float tempoTotal;
    public float tempoRestante;

    public Order(PotionType tipo, float tempoTotal)
    {
        this.tipo = tipo;
        this.tempoTotal = tempoTotal;
        this.tempoRestante = tempoTotal;
    }

    public float Progresso01 => tempoTotal <= 0f ? 0f : Mathf.Clamp01(tempoRestante / tempoTotal);
    public bool Expirado => tempoRestante <= 0f;
}
