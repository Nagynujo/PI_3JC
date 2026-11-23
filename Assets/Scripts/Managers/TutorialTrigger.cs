using UnityEngine;

/// <summary>
/// Dispara um tutorial sem precisar escrever código.
///  - Coloque no prefab do rato com "Fire On Start" marcado: o tutorial dos ratos aparece quando o primeiro rato nasce.
///  - Ou use uma área (Collider com Is Trigger) e desmarque "Fire On Start": aparece quando o jogador entra nela.
///  - O método Fire() também pode ser ligado em UnityEvents (botões, etc.).
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField] private string tutorialId;
    [SerializeField] private bool fireOnStart = true;
    [SerializeField] private string triggerTag = "Player";

    private void Start()
    {
        if (fireOnStart) Fire();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!fireOnStart && other.CompareTag(triggerTag)) Fire();
    }

    public void Fire()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.Trigger(tutorialId);
    }
}
