using FMODUnity;
using UnityEngine;

// =====================================================================================
// [LUCCA - ESCALA] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - ESCALA]")
//   1. Novos campos no Inspector: escalaCru, escalaProcessado, ajusteAoVivo.
//   2. Novo método privado AplicarEscalaDoItem() (usa EscalaUtil).
//   3. PosicionarItemNaBancada() agora chama AplicarEscalaDoItem() no final.
//   4. Update() reaplica a escala todo frame se ajusteAoVivo estiver ligado.
//   5. Removida a dependência de IngredientScale / IngredientScaleZone (não existem mais).
//   6. O item recebido agora faz SetParent(null) em vez de SetParent(transform): a mesa tem
//      escala não-uniforme enorme e deformava qualquer filho. Ver comentário em TentarReceberItem().
//
// =====================================================================================
// [LUCCA - SOM] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - SOM]")
//   1. using FMODUnity (pra usar EventReference e RuntimeManager).
//   2. Novos campos no Inspector: somColocar e somRetirar (EventReference).
//   3. TentarReceberItem() toca somColocar quando a estação aceita o item.
//   4. Interact() toca somRetirar quando o jogador pega o item processado de volta, e chama
//      PickUpIngredient(item, false) pra o player NÃO tocar o som de pegar ao mesmo tempo.
//   5. Novo método privado TocarSom().
//
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================
public class StationBase : MonoBehaviour, IInteractable, IProgressAction
{
    [Header("Configuração")]
    [Tooltip("Qual tipo de ingrediente essa estação aceita")]
    public ProcessType tipoAceito;

    [Tooltip("Quanto tempo (segundos) precisa segurar E pra terminar de processar")]
    public float tempoDeProcesso = 2f;

    [Tooltip("Ponto (objeto vazio filho da estação) onde o ingrediente fica sentado visualmente em cima dela. Se deixar vazio, usa um deslocamento padrão pra cima.")]
    public Transform pontoDoItem;

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO: sliders de tamanho do ingrediente nesta estação.
    // Os valores são em unidades de MUNDO (o tamanho que aparece na tela), não em
    // escala local, então não dependem do scale da mesa.
    // ---------------------------------------------------------------------------------
    [Header("Escala do item (unidades de mundo)")]
    [Tooltip("Tamanho do ingrediente CRU (ainda não moído/cortado) em cima da estação")]
    [Range(0.05f, 2f)] public float escalaCru = 0.5f;

    [Tooltip("Tamanho do ingrediente DEPOIS de moído/cortado em cima da estação")]
    [Range(0.05f, 2f)] public float escalaProcessado = 0.5f;

    [Tooltip("Reaplica a escala todo frame, pra você mexer nos sliders com o jogo rodando. Pode desligar na versão final.")]
    public bool ajusteAoVivo = true;

    [Header("Áudio")]
    [Tooltip("Componente StationAudio desta estação (loop enquanto processa). Pode deixar vazio se a estação ainda não tem som.")]
    public StationAudio audioProcesso;

    // ---------------------------------------------------------------------------------
    // [LUCCA - SOM] NOVO: sons pontuais de colocar/retirar o item na estação (FMOD).
    // Campos vazios são ignorados (não dá erro). Cada estação tem os seus.
    // ---------------------------------------------------------------------------------
    [Tooltip("Som ao COLOCAR o ingrediente na estação. Ex.: event:/colocar_mesa")]
    public EventReference somColocar;

    [Tooltip("Som ao RETIRAR o item processado da estação. Ex.: event:/retirar_mesa")]
    public EventReference somRetirar;

    [Header("Estado (só leitura, pra debug)")]
    public IngredientBase itemAtual;
    public bool estaOcupada;
    public bool estaProcessando;
    [Range(0f, 1f)] public float progresso;

    private PlayerController jogadorAtual;
    private float tempoSegurando;

    public bool EstaEmAndamento => estaProcessando;
    public float Progresso01 => progresso;

    public virtual void Interact(PlayerController player)
    {

        if (!estaOcupada && player.HeldIngredient != null)
        {
            TentarReceberItem(player);
            return;
        }


        if (estaOcupada && !estaProcessando && itemAtual != null && !itemAtual.EstaProcessado
            && player.HeldIngredient == null)
        {
            estaProcessando = true;
            jogadorAtual = player;
            tempoSegurando = 0f;

            if (audioProcesso != null)
            {
                audioProcesso.Begin();
            }

            Debug.Log($"{gameObject.name}: segura E pra processar");
            return;
        }


        if (estaOcupada && itemAtual != null && itemAtual.EstaProcessado && player.HeldIngredient == null)
        {
            // [LUCCA - SOM] ALTERADO: 'false' = o player não toca o som de pegar,
            // porque aqui quem toca é o som de retirar da estação (abaixo).
            player.PickUpIngredient(itemAtual, false);
            TocarSom(somRetirar);   // [LUCCA - SOM] NOVO
            itemAtual = null;
            estaOcupada = false;
            Debug.Log($"{gameObject.name}: jogador pegou o item processado de volta");
        }
    }

    private void TentarReceberItem(PlayerController player)
    {
        IngredientBase held = player.HeldIngredient;

        if (held.ehGarrafaVazia)
        {
            Debug.Log("Não dá pra processar uma garrafa vazia");
            return;
        }


        if (held.potionType != PotionType.None)
        {
            Debug.Log("Isso é uma poção, não dá pra moer/cortar");
            return;
        }

        if (held.tipoDeProcesso == tipoAceito)
        {
            itemAtual = player.ReleaseHand();

            // [LUCCA - ESCALA] ALTERADO: antes era SetParent(transform), pra o IngredientScale
            // (removido) recalcular o tamanho. Agora o item NÃO fica mais filho da mesa:
            // a mesa tem escala enorme e NÃO-UNIFORME (ex.: -100, -118, 3.2), e um filho
            // herda essa deformação (item esmagado/gigante, impossível de corrigir só
            // com escala local). Sem pai, a escala do item é exatamente a do slider.
            // A posição continua correta, pois PosicionarItemNaBancada() usa coordenadas de mundo.
            itemAtual.transform.SetParent(null);
            PosicionarItemNaBancada(itemAtual);
            estaOcupada = true;
            TocarSom(somColocar);   // [LUCCA - SOM] NOVO: som de colocar o ingrediente
            Debug.Log($"{gameObject.name} recebeu {itemAtual.name}");
        }
        else
        {
            Debug.Log("Ingrediente errado pra essa estação");
        }
    }

    /// <summary>
    /// Coloca o item visualmente sentado em cima da bancada, em vez de deixar
    /// ele onde a mão do player estava. Usa 'pontoDoItem' se estiver configurado.
    /// [LUCCA - ESCALA] Também aplica o tamanho do slider (cru ou processado) no final.
    /// </summary>
    private void PosicionarItemNaBancada(IngredientBase item)
    {
        if (pontoDoItem != null)
        {
            item.transform.position = pontoDoItem.position;
            item.transform.rotation = pontoDoItem.rotation;
        }
        else
        {
            // -----------------------------------------------------------------------
            // [ALTERADO - alteração anterior] Fallback usa coordenadas de MUNDO.
            //
            // Antes: item.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            //   -> o 0.5 era LOCAL, então ele era multiplicado pelo scale da mesa.
            //      Mesa com scale 5 => item a 2.5 unidades de altura ("no céu").
            //
            // Agora: 0.5 unidade de mundo acima da origem da estação,
            //   independente do scale da mesa.
            // -----------------------------------------------------------------------
            item.transform.position = transform.position + Vector3.up * 0.5f;
            item.transform.rotation = Quaternion.identity;
        }

        // [LUCCA - ESCALA] NOVO: aplica o tamanho do slider.
        AplicarEscalaDoItem(item);
    }

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO: escolhe o slider certo (cru ou processado) e aplica.
    // É chamado em PosicionarItemNaBancada() e, se ajusteAoVivo estiver ligado, no Update().
    // ---------------------------------------------------------------------------------
    private void AplicarEscalaDoItem(IngredientBase item)
    {
        if (item == null) return;

        float escala = item.EstaProcessado ? escalaProcessado : escalaCru;
        EscalaUtil.DefinirEscalaMundo(item.transform, escala);
    }

    // ---------------------------------------------------------------------------------
    // [LUCCA - SOM] NOVO: toca um evento pontual (one-shot) na posição da estação.
    // Se o campo estiver vazio no Inspector, não faz nada.
    // ---------------------------------------------------------------------------------
    private void TocarSom(EventReference evento)
    {
        if (evento.IsNull) return;
        RuntimeManager.PlayOneShot(evento, transform.position);
    }

    void Update()
    {
        // [LUCCA - ESCALA] NOVO: ajuste ao vivo dos sliders (só enquanto há item na estação).
        // Fica ANTES do "return" abaixo pra funcionar mesmo sem estar processando.
        if (ajusteAoVivo && itemAtual != null)
        {
            AplicarEscalaDoItem(itemAtual);
        }

        if (!estaProcessando) return;

        if (jogadorAtual != null && jogadorAtual.IsInteractKeyHeld)
        {
            tempoSegurando += Time.deltaTime * EventModifiers.ActionSpeedMultiplier;
            progresso = Mathf.Clamp01(tempoSegurando / tempoDeProcesso);

            if (tempoSegurando >= tempoDeProcesso)
            {
                FinishProcessing();
            }
        }
        else
        {

            Debug.Log($"{gameObject.name}: soltou cedo demais, tenta de novo");
            estaProcessando = false;
            progresso = 0f;
            jogadorAtual = null;
            tempoSegurando = 0f;

            if (audioProcesso != null)
            {
                audioProcesso.End(false);
            }
        }
    }

    private void FinishProcessing()
    {
        estaProcessando = false;
        progresso = 0f;
        jogadorAtual = null;
        tempoSegurando = 0f;

        if (audioProcesso != null)
        {
            audioProcesso.End(true);
        }

        itemAtual.MarcarComoProcessado();

        // [LUCCA - ESCALA] Como o item agora está marcado como processado, o
        // PosicionarItemNaBancada() já aplica o slider 'escalaProcessado'.
        PosicionarItemNaBancada(itemAtual);
        Debug.Log($"{gameObject.name}: {itemAtual.name} processado!");
    }
}