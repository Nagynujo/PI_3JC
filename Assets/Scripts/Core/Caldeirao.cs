using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

// =====================================================================================
// [LUCCA - ESCALA] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - ESCALA]")
//   1. Novos campos no Inspector: escalaNoCaldeirao e ajusteAoVivo.
//   2. PosicionarIngredienteNoCaldeirao() agora aplica a escala no final (usa EscalaUtil).
//   3. Update() reaplica a escala todo frame se ajusteAoVivo estiver ligado.
//   4. Removida a dependência de IngredientScale / IngredientScaleZone (não existem mais).
//   5. O ingrediente recebido agora faz SetParent(null) em vez de SetParent(transform), e o
//      fallback de posição passou a usar coordenadas de mundo (ver Interact() e
//      PosicionarIngredienteNoCaldeirao()).
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================
public class Caldeirao : MonoBehaviour, IInteractable, IProgressAction
{
    [System.Serializable]
    public class PotionPrefabEntry
    {
        public PotionType tipo;
        [Tooltip("Prefab usado só como referência de mesh/material (nunca é instanciado)")]
        public GameObject prefab;
    }

    [Header("Configuração")]
    public float tempoDeFervura = 5f;
    [Tooltip("Um prefab de poção pra cada PotionType (Fire, Acid, Poison, Invisibility, Cure, Fast) — usado só pra pegar mesh/material na hora de engarrafar")]
    public List<PotionPrefabEntry> potionPrefabs;

    [Tooltip("O mesmo asset de receitas usado pelo livro de receitas (RecipeBookUI)")]
    public LivroDeReceitasSO livroDeReceitas;

    [Tooltip("Pontos (objetos vazios filhos do Caldeirão) onde os 2 ingredientes ficam sentados visualmente. Se deixar vazio, usa deslocamentos padrão.")]
    public List<Transform> pontosDosIngredientes;

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO: slider de tamanho dos ingredientes dentro do caldeirão.
    // Valor em unidades de MUNDO (o tamanho que aparece na tela), não em escala local,
    // então não depende do scale do caldeirão.
    // ---------------------------------------------------------------------------------
    [Header("Escala dos ingredientes no caldeirão (unidades de mundo)")]
    [Tooltip("Tamanho dos ingredientes (já processados) enquanto estão dentro do caldeirão")]
    [Range(0.05f, 2f)] public float escalaNoCaldeirao = 0.3f;

    [Tooltip("Reaplica a escala todo frame, pra você mexer no slider com o jogo rodando. Pode desligar na versão final.")]
    public bool ajusteAoVivo = true;

    [Header("Efeitos")]
    [Tooltip("Partícula tocada enquanto o caldeirão está fervendo (bolhas/vapor)")]
    public ParticleSystem particulaFervura;

    [Header("Áudio")]
    [Tooltip("Componente StationAudio deste caldeirão (loop da fervura)")]
    public StationAudio audioFervura;

    [Tooltip("Som tocado uma vez quando a garrafa é enchida com a poção (gota)")]
    public EventReference somEngarrafar;

    [Header("Estado (só leitura, pra debug)")]
    public List<IngredientBase> ingredientesDentro = new List<IngredientBase>();
    public bool estaFervendo;
    public bool potionPronta;
    [Range(0f, 1f)] public float progresso;

    private float tempoDecorrido;
    private PotionType potionProntaTipo = PotionType.None;

    public bool EstaEmAndamento => estaFervendo;
    public float Progresso01 => progresso;

    public void Interact(PlayerController player)
    {
        if (potionPronta)
        {
            TentarEngarrafar(player);
            return;
        }

        if (estaFervendo) return;

        IngredientBase held = player.HeldIngredient;

        if (held != null)
        {
            if (held.ehGarrafaVazia)
            {
                Debug.Log("Isso é uma garrafa vazia, não é ingrediente");
                return;
            }

            if (held.potionType != PotionType.None)
            {
                Debug.Log("Isso já é uma poção, não dá pra colocar de volta no caldeirão");
                return;
            }

            if (!held.EstaProcessado)
            {
                Debug.Log("Esse ingrediente ainda não foi moído/cortado");
                return;
            }

            if (ingredientesDentro.Count >= 2)
            {
                Debug.Log("Caldeirão já tem os 2 ingredientes, pode começar a ferver");
                return;
            }

            IngredientBase item = player.ReleaseHand();

            // [LUCCA - ESCALA] ALTERADO: antes era SetParent(transform), pra o IngredientScale
            // (removido) recalcular o tamanho. Agora o item NÃO fica mais filho do caldeirão:
            // objetos com escala grande/não-uniforme deformam qualquer filho (mesmo problema
            // da mesa de moer). Sem pai, o tamanho é exatamente o do slider.
            // Os ingredientes continuam sendo destruídos em FinishFervura() pela lista
            // 'ingredientesDentro', então não ficam objetos soltos na cena.
            item.transform.SetParent(null);
            PosicionarIngredienteNoCaldeirao(item, ingredientesDentro.Count);
            ingredientesDentro.Add(item);
            Debug.Log($"Caldeirão: {ingredientesDentro.Count}/2 ingredientes");
            return;
        }

        if (ingredientesDentro.Count == 2)
        {
            estaFervendo = true;
            tempoDecorrido = 0f;
            progresso = 0f;

            if (particulaFervura != null)
            {
                particulaFervura.Play();
            }

            if (audioFervura != null)
            {
                audioFervura.Begin();
            }

            Debug.Log("Começou a ferver...");
        }
        else
        {
            Debug.Log($"Precisa de 2 ingredientes (tem {ingredientesDentro.Count})");
        }
    }

    private void TentarEngarrafar(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;

        if (held == null)
        {
            Debug.Log("Pega uma garrafa vazia pra encher a poção");
            return;
        }

        if (!held.ehGarrafaVazia || held.estaEngarrafada)
        {
            Debug.Log("Precisa estar segurando uma garrafa vazia pra encher");
            return;
        }

        GameObject prefab = GetPrefabFor(potionProntaTipo);
        if (prefab == null)
        {
            Debug.LogWarning($"Nenhum prefab configurado pra poção {potionProntaTipo}");
            return;
        }

        MeshFilter prefabMeshFilter = prefab.GetComponent<MeshFilter>();
        MeshRenderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
        Mesh mesh = prefabMeshFilter != null ? prefabMeshFilter.sharedMesh : null;
        Material mat = prefabRenderer != null ? prefabRenderer.sharedMaterial : null;

        held.PreencherComoPotion(potionProntaTipo, mesh, mat);
        player.RefreshHeldVisual();

        if (!somEngarrafar.IsNull)
        {
            RuntimeManager.PlayOneShot(somEngarrafar, transform.position);
        }

        potionPronta = false;
        potionProntaTipo = PotionType.None;
        Debug.Log("Garrafa engarrafada com a poção!");
    }

    void Update()
    {
        // [LUCCA - ESCALA] NOVO: ajuste ao vivo do slider (só enquanto há ingredientes dentro).
        // Fica ANTES do "return" abaixo pra funcionar mesmo sem estar fervendo.
        if (ajusteAoVivo)
        {
            foreach (IngredientBase ing in ingredientesDentro)
            {
                if (ing != null)
                {
                    EscalaUtil.DefinirEscalaMundo(ing.transform, escalaNoCaldeirao);
                }
            }
        }

        if (!estaFervendo) return;

        tempoDecorrido += Time.deltaTime * EventModifiers.ActionSpeedMultiplier;
        progresso = Mathf.Clamp01(tempoDecorrido / tempoDeFervura);

        if (tempoDecorrido >= tempoDeFervura)
        {
            FinishFervura();
        }
    }

    private void FinishFervura()
    {
        estaFervendo = false;
        progresso = 0f;

        if (particulaFervura != null)
        {
            particulaFervura.Stop();
        }

        PotionType resultado = DeterminePotion();

        if (audioFervura != null)
        {
            audioFervura.End(resultado != PotionType.None);
        }

        foreach (IngredientBase ing in ingredientesDentro)
        {
            Destroy(ing.gameObject);
        }
        ingredientesDentro.Clear();

        if (resultado == PotionType.None)
        {
            Debug.Log("Combinação inválida - poção falhou");
            return;
        }

        potionProntaTipo = resultado;
        potionPronta = true;

        Debug.Log($"Poção pronta: {resultado}! Segura uma garrafa vazia e aperta E pra encher");
    }

    private PotionType DeterminePotion()
    {
        if (ingredientesDentro.Count != 2) return PotionType.None;

        if (livroDeReceitas == null)
        {
            Debug.LogWarning("Caldeirao: nenhum 'Livro De Receitas' configurado. Arraste o asset LivroDeReceitasSO no Inspector.");
            return PotionType.None;
        }

        IngredientKind a = ingredientesDentro[0].kind;
        IngredientKind b = ingredientesDentro[1].kind;

        return livroDeReceitas.Combinar(a, b);
    }

    /// <summary>
    /// Coloca o ingrediente visualmente sentado num dos pontos do caldeirão, em vez de
    /// deixar ele onde a mão do player estava (o que fazia ele "reaparecer" na estação
    /// anterior). 'indice' é a posição na lista (0 = primeiro ingrediente, 1 = segundo).
    /// [LUCCA - ESCALA] Também aplica o tamanho do slider 'escalaNoCaldeirao' no final.
    /// </summary>
    private void PosicionarIngredienteNoCaldeirao(IngredientBase item, int indice)
    {
        if (pontosDosIngredientes != null && indice < pontosDosIngredientes.Count && pontosDosIngredientes[indice] != null)
        {
            item.transform.position = pontosDosIngredientes[indice].position;
            item.transform.rotation = pontosDosIngredientes[indice].rotation;
        }
        else
        {
            // Fallback: um deslocamento pequeno pra cada ingrediente não ficar
            // exatamente sobreposto ao outro.
            // [LUCCA - ESCALA] ALTERADO: como o item não é mais filho do caldeirão, o fallback
            // passou de localPosition para coordenadas de MUNDO (relativas à origem do caldeirão).
            float deslocamentoX = indice == 0 ? -0.2f : 0.2f;
            item.transform.position = transform.position + new Vector3(deslocamentoX, 0.5f, 0f);
            item.transform.rotation = Quaternion.identity;
        }

        // [LUCCA - ESCALA] NOVO: aplica o tamanho do slider.
        EscalaUtil.DefinirEscalaMundo(item.transform, escalaNoCaldeirao);
    }


    private GameObject GetPrefabFor(PotionType tipo)
    {
        foreach (var entry in potionPrefabs)
        {
            if (entry.tipo == tipo) return entry.prefab;
        }
        return null;
    }
}