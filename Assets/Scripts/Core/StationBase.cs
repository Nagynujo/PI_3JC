using UnityEngine;


public class StationBase : MonoBehaviour, IInteractable, IProgressAction
{
    [Header("Configuração")]
    [Tooltip("Qual tipo de ingrediente essa estação aceita")]
    public ProcessType tipoAceito;

    [Tooltip("Quanto tempo (segundos) precisa segurar E pra terminar de processar")]
    public float tempoDeProcesso = 2f;

    [Tooltip("Ponto (objeto vazio filho da estação) onde o ingrediente fica sentado visualmente em cima dela. Se deixar vazio, usa um deslocamento padrão pra cima.")]
    public Transform pontoDoItem;

    [Header("Áudio")]
    [Tooltip("Componente StationAudio desta estação (loop enquanto processa). Pode deixar vazio se a estação ainda não tem som.")]
    public StationAudio audioProcesso;

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
            player.PickUpIngredient(itemAtual);
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
            // O SetParent dispara OnTransformParentChanged no IngredientScale,
            // que recalcula a escala usando a IngredientScaleZone desta estação.
            itemAtual.transform.SetParent(transform);
            PosicionarItemNaBancada(itemAtual);
            estaOcupada = true;
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
            // [ALTERADO] Fallback agora usa coordenadas de MUNDO.
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
    }

    void Update()
    {
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
        PosicionarItemNaBancada(itemAtual);
        Debug.Log($"{gameObject.name}: {itemAtual.name} processado!");
    }
}