using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 5f;
    public float rotationSpeed = 10f;

    [Header("Mão (equivalente ao HeldItemMesh)")]
    [Tooltip("Um GameObject filho vazio, posicionado tipo 'na mão', com MeshFilter + MeshRenderer")]
    public Transform handSlot;

    public IngredientBase HeldIngredient { get; private set; }

    private CharacterController controller;
    private MeshFilter handMeshFilter;
    private MeshRenderer handRenderer;
    private IInteractable currentInteractable;

    private InputAction moveAction;
    private InputAction interactAction;

    // --- Estado da interação segurada (hold) ---
    private bool isHolding;
    private IInteractable holdingTarget;
    private float holdTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
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
        HandleInteraction();
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0f, input.y);

        if (move.sqrMagnitude > 0.01f)
        {
            controller.SimpleMove(move.normalized * speed);

            
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleInteraction()
    {
        // Se saiu do alcance (ou o interactable sumiu) enquanto segurava E, cancela
        if (isHolding && currentInteractable != holdingTarget)
        {
            CancelHold();
        }

        if (!isHolding)
        {
            if (interactAction.WasPressedThisFrame() && currentInteractable != null)
            {
                if (currentInteractable.RequiresHold)
                {
                    if (currentInteractable.CanStartHold(this))
                    {
                        StartHold(currentInteractable);
                    }
                }
                else
                {
                    currentInteractable.Interact(this);
                }
            }

            return;
        }

        // Player está segurando E numa interação em andamento
        if (!interactAction.IsPressed())
        {
            CancelHold();
            return;
        }

        holdTimer += Time.deltaTime;
        float progress = holdingTarget.HoldDuration > 0f
            ? Mathf.Clamp01(holdTimer / holdingTarget.HoldDuration)
            : 1f;

        holdingTarget.OnHoldProgress(this, progress);

        if (progress >= 1f)
        {
            holdingTarget.OnHoldComplete(this);
            StopHold();
        }
    }

    void StartHold(IInteractable target)
    {
        isHolding = true;
        holdingTarget = target;
        holdTimer = 0f;
        target.OnHoldStart(this);
    }

    void StopHold()
    {
        isHolding = false;
        holdingTarget = null;
        holdTimer = 0f;
    }

    void CancelHold()
    {
        if (!isHolding) return;
        holdingTarget.OnHoldCancelled(this);
        StopHold();
    }

  
    public void PickUpIngredient(IngredientBase ingredient)
    {
        HeldIngredient = ingredient;
        handMeshFilter.mesh = ingredient.GetIngredientMesh();
        handRenderer.material = ingredient.GetIngredientMaterial();

        
        ingredient.gameObject.SetActive(false);
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

    // Tira o ingrediente da mão do player SEM destruir o gameObject. Use isso (em vez de
    // ClearHand) quando uma estação vai guardar o ingrediente pra processar e pode
    // precisar devolvê-lo depois (ex: se o hold for cancelado).
    public IngredientBase ReleaseHeldIngredient()
    {
        IngredientBase ingredient = HeldIngredient;
        HeldIngredient = null;
        handMeshFilter.mesh = null;
        return ingredient;
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