using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

// =====================================================================================
// [LUCCA - ESCALA] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - ESCALA]")
//   1. Novos campos no Inspector: escalaCruNaMao, escalaProcessadoNaMao,
//      escalaGarrafaNaMao, escalaPocaoNaMao e ajusteAoVivo.
//   2. Novo método privado AplicarEscalaNaMao() (usa EscalaUtil no handSlot).
//   3. PickUpIngredient() e RefreshHeldVisual() agora chamam AplicarEscalaNaMao().
//   4. Update() reaplica a escala todo frame se ajusteAoVivo estiver ligado.
//
// POR QUE: o item na mão NÃO é o ingrediente em si (ele fica desativado), é só a mesh
// copiada pro handSlot. Então a escala precisa ser aplicada no handSlot, e como o mesmo
// handSlot mostra cru / processado / garrafa / poção (meshes de tamanhos diferentes),
// existe um slider pra cada caso.
//
// ATENÇÃO: a escala do handSlot no Inspector/prefab é SOBRESCRITA por este script
// enquanto o player segura algo. Ajuste pelos sliders abaixo, não pelo Transform do handSlot.
//
// =====================================================================================
// [LUCCA - SOM] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - SOM]")
//   1. using FMODUnity (pra usar EventReference e RuntimeManager).
//   2. Novos campos no Inspector: somPegarIngrediente e somPegarPocao.
//   3. PickUpIngredient() ganhou o parâmetro opcional 'tocarSom' (padrão = true) e toca
//      o som de pegar no final. As chamadas antigas (ex.: IngredientBox) continuam
//      funcionando sem mudar. A StationBase chama com 'false' porque o som de RETIRAR
//      da mesa é tocado por ela (evita dois sons ao mesmo tempo).
//   4. Novo método privado TocarSomDePegar() (poção usa o som de poção, o resto usa o
//      de ingrediente).
//
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 5f;
    public float rotationSpeed = 10f;

    [Header("Áudio de Passos")]
    [Tooltip("Componente PlayerWalkAudio (FMOD) no mesmo objeto do jogador")]
    public PlayerWalkAudio audioPassos;

    // ---------------------------------------------------------------------------------
    // [LUCCA - SOM] NOVO: eventos do FMOD tocados quando o player PEGA um item.
    // Campos vazios são ignorados (não dá erro).
    // ---------------------------------------------------------------------------------
    [Header("Áudio de pegar item (FMOD)")]
    [Tooltip("Som ao pegar um INGREDIENTE (ou garrafa vazia). Ex.: event:/pegar_ingrediente")]
    [SerializeField] EventReference somPegarIngrediente;

    [Tooltip("Som ao pegar uma POÇÃO (garrafa cheia). Ex.: event:/pegar_pocao")]
    [SerializeField] EventReference somPegarPocao;

    [Header("Mão (equivalente ao HeldItemMesh)")]
    public Transform handSlot;

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO: sliders de tamanho do item NA MÃO do player.
    // Valores em unidades de MUNDO (o tamanho que aparece na tela), não em escala local,
    // então não dependem da escala do player nem do handSlot.
    // ---------------------------------------------------------------------------------
    [Header("Escala do item na mão (unidades de mundo)")]
    [Tooltip("Ingrediente CRU (ainda não moído/cortado) na mão")]
    [Range(0.05f, 2f)] public float escalaCruNaMao = 0.3f;

    [Tooltip("Ingrediente PROCESSADO (moído/cortado) na mão")]
    [Range(0.05f, 2f)] public float escalaProcessadoNaMao = 0.3f;

    [Tooltip("Garrafa VAZIA na mão")]
    [Range(0.05f, 2f)] public float escalaGarrafaNaMao = 0.3f;

    [Tooltip("POÇÃO (garrafa cheia) na mão")]
    [Range(0.05f, 2f)] public float escalaPocaoNaMao = 0.3f;

    [Tooltip("Reaplica a escala todo frame, pra você mexer nos sliders com o jogo rodando. Pode desligar na versão final.")]
    public bool ajusteAoVivo = true;

    public IngredientBase HeldIngredient { get; private set; }
    public bool IsInteractKeyHeld => interactAction.IsPressed();

    private CharacterController controller;
    private MeshFilter handMeshFilter;
    private MeshRenderer handRenderer;
    private IInteractable currentInteractable;
    private Animator animator; // 1. Referência ao Animator

    private InputAction moveAction;
    private InputAction interactAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();


        animator = GetComponentInChildren<Animator>();

        handMeshFilter = handSlot.GetComponent<MeshFilter>();
        handRenderer = handSlot.GetComponent<MeshRenderer>();

        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        interactAction = new InputAction("Interact", InputActionType.Button, binding: "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void OnEnable()
    {
        moveAction.Enable();
        interactAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        interactAction.Disable();
    }

    void Update()
    {
        HandleMovement();

        // [LUCCA - ESCALA] NOVO: ajuste ao vivo dos sliders (só enquanto há item na mão).
        if (ajusteAoVivo && HeldIngredient != null)
        {
            AplicarEscalaNaMao();
        }

        if (interactAction.WasPressedThisFrame() && currentInteractable != null && !EventModifiers.IsParalyzed)
        {
            currentInteractable.Interact(this);
        }
    }

    void HandleMovement()
    {

        if (EventModifiers.IsParalyzed)
        {
            if (animator) { animator.SetFloat("Speed", 0f); animator.speed = 0f; }
            if (audioPassos != null) audioPassos.SetWalking(false);
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0f, input.y);
        float inputMagnitude = move.sqrMagnitude;


        if (animator != null)
        {
            animator.SetFloat("Speed", inputMagnitude);
            // Evento Iced: animação mais lenta também (1 = normal; sem evento nada muda)
            animator.speed = EventModifiers.MoveSpeedMultiplier;
        }

        if (inputMagnitude > 0.01f)
        {
            controller.SimpleMove(move.normalized * speed * EventModifiers.MoveSpeedMultiplier);
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            // Liga o som dos passos (FMOD)
            if (audioPassos != null) audioPassos.SetWalking(true);
        }
        else
        {
            // Desliga o som dos passos (FMOD)
            if (audioPassos != null) audioPassos.SetWalking(false);
        }
    }

    /// <summary>
    /// Coloca o item na mão do player.
    /// [LUCCA - SOM] 'tocarSom' (padrão true) toca o som de pegar. A StationBase passa false
    /// porque, ao retirar o item da mesa, quem toca o som é a própria mesa.
    /// </summary>
    public void PickUpIngredient(IngredientBase ingredient, bool tocarSom = true)
    {
        HeldIngredient = ingredient;
        handMeshFilter.mesh = ingredient.GetIngredientMesh();
        handRenderer.material = ingredient.GetIngredientMaterial();
        ingredient.gameObject.SetActive(false);

        // [LUCCA - ESCALA] NOVO: aplica o tamanho certo na mão logo ao pegar o item.
        AplicarEscalaNaMao();

        // [LUCCA - SOM] NOVO: toca o som de pegar (por último, depois do item já estar na mão).
        if (tocarSom)
        {
            TocarSomDePegar(ingredient);
        }
    }

    // ---------------------------------------------------------------------------------
    // [LUCCA - SOM] NOVO: escolhe o evento conforme o item pego. Só a garrafa CHEIA tem
    // potionType, então garrafa vazia e ingredientes usam o som de ingrediente.
    // ---------------------------------------------------------------------------------
    private void TocarSomDePegar(IngredientBase item)
    {
        EventReference evento = item.potionType != PotionType.None ? somPegarPocao : somPegarIngrediente;

        if (evento.IsNull) return;
        RuntimeManager.PlayOneShot(evento, transform.position);
    }

    public void RefreshHeldVisual()
    {
        if (HeldIngredient == null) return;
        handMeshFilter.mesh = HeldIngredient.GetIngredientMesh();
        handRenderer.material = HeldIngredient.GetIngredientMaterial();

        // [LUCCA - ESCALA] NOVO: o visual mudou (ex.: garrafa virou poção), então
        // o slider usado também pode ter mudado.
        AplicarEscalaNaMao();
    }

    // ---------------------------------------------------------------------------------
    // [LUCCA - ESCALA] NOVO: escolhe o slider conforme o que o player está segurando
    // e aplica no handSlot. A ORDEM dos ifs importa: uma garrafa cheia continua com
    // ehGarrafaVazia = true (só ganha potionType), então a poção é checada primeiro.
    // ---------------------------------------------------------------------------------
    private void AplicarEscalaNaMao()
    {
        if (HeldIngredient == null || handSlot == null) return;

        float escala;

        if (HeldIngredient.potionType != PotionType.None)
        {
            escala = escalaPocaoNaMao;          // garrafa já engarrafada com poção
        }
        else if (HeldIngredient.ehGarrafaVazia)
        {
            escala = escalaGarrafaNaMao;        // garrafa vazia
        }
        else if (HeldIngredient.EstaProcessado)
        {
            escala = escalaProcessadoNaMao;     // ingrediente moído/cortado
        }
        else
        {
            escala = escalaCruNaMao;            // ingrediente cru
        }

        EscalaUtil.DefinirEscalaMundo(handSlot, escala);
    }

    public IngredientBase ReleaseHand()
    {
        IngredientBase released = HeldIngredient;
        HeldIngredient = null;
        handMeshFilter.mesh = null;

        if (released != null)
            released.gameObject.SetActive(true);

        return released;
    }

    public void ClearHand()
    {
        if (HeldIngredient != null)
        {
            Destroy(HeldIngredient.gameObject);
        }
        HeldIngredient = null;
        handMeshFilter.mesh = null;
    }

    void OnTriggerStay(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null) currentInteractable = interactable;
    }

    void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == currentInteractable) currentInteractable = null;
    }
}