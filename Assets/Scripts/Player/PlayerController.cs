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

        
        if (interactAction.WasPressedThisFrame() && currentInteractable != null)
        {
            Debug.Log("Apertei E. Interactable atual: " + currentInteractable);
            currentInteractable.Interact(this);
        }
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

    void OnTriggerStay(Collider other)
    {
        Debug.Log("Trigger com: " + other.gameObject.name);
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null) currentInteractable = interactable;
    }

    void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == currentInteractable) currentInteractable = null;
    }
}