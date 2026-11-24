using System.Collections.Generic;
using UnityEngine;

// =====================================================================================
// [LUCCA - ESCALA] ARQUIVO NOVO (versão 3: normalizada pela mesh + diagnóstico)
//
// Por que existe:
//   Os scripts IngredientScale e IngredientScaleZone foram REMOVIDOS do projeto.
//   Eles recalculavam a escala do ingrediente toda vez que ele mudava de pai
//   (OnTransformParentChanged), o que fazia o modelo aparecer num tamanho na mesa,
//   outro depois de cortado/moído e outro no caldeirão.
//
//   Agora o tamanho é controlado por sliders no Inspector de cada estação
//   (StationBase), do Caldeirão e do PlayerController (mão), usando este helper.
//
// O QUE O VALOR DO SLIDER SIGNIFICA:
//   É o tamanho da MAIOR DIMENSÃO do modelo, em unidades de MUNDO (1 = 1 metro na cena).
//   O script mede a mesh atual do objeto (mesh.bounds) e calcula a escala sozinho, então
//   não importa se a mesh foi importada gigante ou minúscula.
//
// QUANDO CHAMAR:
//   Sempre DEPOIS de trocar o pai (SetParent) E de trocar a mesh (ex.: MarcarComoProcessado,
//   PreencherComoPotion), porque o cálculo usa o pai e a mesh atuais.
//
// REQUISITO:
//   O objeto precisa ter MeshFilter (os ingredientes e o handSlot já têm). Se não tiver
//   MeshFilter ou mesh, cai no comportamento antigo (multiplica a escala).
//
// PAIS COM ESCALA NÃO-UNIFORME (v4):
//   Se o objeto tiver um pai com escala diferente em cada eixo (ex.: mesa com
//   (100, 118, 3.2)), o filho sai deformado e nenhuma escala local corrige isso.
//   Por isso StationBase e Caldeirao fazem SetParent(null) nos ingredientes. O log
//   [EscalaUtil] avisa (LogWarning) quando isso acontecer em algum outro objeto.
//
// DIAGNÓSTICO (v3):
//   Com 'Diagnostico' = true, escreve no Console linhas começando com [EscalaUtil] sempre
//   que muda a mesh ou o valor do slider de um objeto. DESLIGAR (false) antes do build final.
// =====================================================================================
public static class EscalaUtil
{
    /// <summary>[LUCCA - ESCALA] Liga/desliga as mensagens [EscalaUtil] no Console.</summary>
    public static bool Diagnostico = true;

    // Guarda a última combinação (mesh + valor do slider) logada por objeto, pra não
    // repetir a mesma linha todo frame quando o ajuste ao vivo está ligado.
    private static readonly Dictionary<int, string> ultimoLog = new Dictionary<int, string>();

    /// <summary>
    /// Faz o modelo ter 'tamanho' unidades de MUNDO na sua MAIOR dimensão (uniforme nos
    /// 3 eixos), independente da escala do pai e do tamanho original da mesh.
    /// Deve ser chamado DEPOIS do SetParent e depois de qualquer troca de mesh.
    /// </summary>
    public static void DefinirEscalaMundo(Transform t, float tamanho)
    {
        // Escala acumulada dos pais (mesa com scale 5, player escalado etc.).
        Vector3 pai = t.parent != null ? t.parent.lossyScale : Vector3.one;

        // [LUCCA - ESCALA] v4: valor absoluto, pra um pai com escala NEGATIVA (espelhado)
        // não gerar localScale negativo (que espelharia/inverteria o item).
        pai = new Vector3(Mathf.Abs(pai.x), Mathf.Abs(pai.y), Mathf.Abs(pai.z));

        // Fator base: se não houver mesh, usa o valor direto (comportamento da versão 1).
        float fator = tamanho;

        MeshFilter mf = t.GetComponent<MeshFilter>();
        Mesh mesh = mf != null ? mf.sharedMesh : null;

        if (mesh != null)
        {
            Vector3 tamanhoMesh = mesh.bounds.size; // tamanho da mesh SEM escala
            float maiorDimensao = Mathf.Max(tamanhoMesh.x, tamanhoMesh.y, tamanhoMesh.z);

            if (maiorDimensao > 0.0001f)
            {
                fator = tamanho / maiorDimensao;
            }
        }

        // Mathf.Max evita divisão por zero caso algum pai tenha escala 0.
        t.localScale = new Vector3(
            fator / Mathf.Max(pai.x, 0.0001f),
            fator / Mathf.Max(pai.y, 0.0001f),
            fator / Mathf.Max(pai.z, 0.0001f));

        if (Diagnostico)
        {
            LogarDiagnostico(t, mesh, tamanho, pai);
        }
    }

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO (v3): escreve no Console o que está acontecendo de verdade.
    //   - mesh.bounds.size   : tamanho ORIGINAL da mesh (se for enorme, é problema de importação)
    //   - mesh.bounds.center : onde está o centro da mesh em relação ao pivô do objeto
    //   - pai.lossyScale     : escala acumulada dos pais
    //   - tamanho final      : quanto o modelo mede de fato na cena (deveria ser = slider)
    //   - desvio do centro   : distância (em mundo) entre o pivô e o centro visual do modelo
    //                          (se for grande, o modelo "flutua" longe de onde foi posicionado)
    // ---------------------------------------------------------------------------------
    private static void LogarDiagnostico(Transform t, Mesh mesh, float tamanho, Vector3 pai)
    {
        int id = t.GetInstanceID();
        string chave = (mesh != null ? mesh.GetInstanceID().ToString() : "sem-mesh") + "|" + tamanho;

        if (ultimoLog.TryGetValue(id, out string anterior) && anterior == chave) return;
        ultimoLog[id] = chave;

        if (mesh == null)
        {
            Debug.Log($"[EscalaUtil] '{t.name}': SEM MeshFilter/mesh, usei escala direta {tamanho}.", t);
            return;
        }

        // [LUCCA - ESCALA] v4: avisa quando o pai tem escala NÃO-UNIFORME. Nesse caso o item
        // (filho) fica deformado e nenhuma escala local corrige. Solução: SetParent(null).
        float maiorPai = Mathf.Max(pai.x, pai.y, pai.z);
        float menorPai = Mathf.Min(pai.x, pai.y, pai.z);
        if (menorPai > 0f && maiorPai / menorPai > 1.05f)
        {
            Debug.LogWarning(
                $"[EscalaUtil] '{t.name}': o pai '{t.parent.name}' tem escala NÃO-UNIFORME {pai}. " +
                "O item fica deformado. Solução: não deixar o item como filho desse objeto (SetParent(null)).", t);
        }

        Vector3 centroMundo = t.TransformPoint(mesh.bounds.center);
        Vector3 tamanhoFinal = Vector3.Scale(mesh.bounds.size, t.lossyScale);
        Vector3 desvio = centroMundo - t.position;

        Debug.Log(
            $"[EscalaUtil] '{t.name}' (pai: {(t.parent != null ? t.parent.name : "nenhum")}) | " +
            $"mesh '{mesh.name}' bounds.size={mesh.bounds.size} bounds.center={mesh.bounds.center} | " +
            $"pai.lossyScale={pai} | slider={tamanho} | localScale={t.localScale} | " +
            $"tamanho final no mundo={tamanhoFinal} | desvio do centro (mundo)={desvio} ({desvio.magnitude:F2})",
            t);
    }
}