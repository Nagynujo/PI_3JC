/// <summary>
/// Nomes em português pra exibir na UI (pedidos, livro de receitas) em vez do
/// nome cru do enum PotionType.
/// </summary>
public static class PotionNomes
{
    public static string EmPortugues(PotionType tipo)
    {
        switch (tipo)
        {
            case PotionType.Fire: return "Fogo";
            case PotionType.Acid: return "Ácido";
            case PotionType.Poison: return "Veneno";
            case PotionType.Invisibility: return "Invisibilidade";
            case PotionType.Cure: return "Cura";
            case PotionType.Fast: return "Velocidade";
            default: return "???";
        }
    }
}
