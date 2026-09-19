/// <summary>
/// Implementado por qualquer estação/ação que tem duração e progresso (0 a 1) —
/// ex: Caldeirao (cozinhar), StationBase (cortar/moer). Ações instantâneas
/// (pegar, soltar, entregar, jogar no lixo) NÃO implementam isso.
/// </summary>
public interface IProgressAction
{
    /// <summary>True enquanto a ação está em andamento (a barra deve estar visível).</summary>
    bool EstaEmAndamento { get; }

    /// <summary>Progresso atual, de 0 a 1.</summary>
    float Progresso01 { get; }
}
