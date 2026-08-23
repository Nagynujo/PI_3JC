using UnityEngine;


public class StationBase : MonoBehaviour, IInteractable, IProgressAction
{
    [Header("Configuração")]
    [Tooltip("Qual tipo de ingrediente essa estação aceita")]
    public ProcessType tipoAceito;

    [Tooltip("Quanto tempo (segundos) precisa segurar E pra terminar de processar")]
    public float tempoDeProcesso = 2f;

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
            itemAtual.transform.SetParent(transform);
            estaOcupada = true;
            Debug.Log($"{gameObject.name} recebeu {itemAtual.name}");
        }
        else
        {
            Debug.Log("Ingrediente errado pra essa estação");
        }
    }

    void Update()
    {
        if (!estaProcessando) return;

        if (jogadorAtual != null && jogadorAtual.IsInteractKeyHeld)
        {
            tempoSegurando += Time.deltaTime;
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
        }
    }

    private void FinishProcessing()
    {
        estaProcessando = false;
        progresso = 0f;
        jogadorAtual = null;
        tempoSegurando = 0f;

        itemAtual.MarcarComoProcessado();
        Debug.Log($"{gameObject.name}: {itemAtual.name} processado!");
    }
}