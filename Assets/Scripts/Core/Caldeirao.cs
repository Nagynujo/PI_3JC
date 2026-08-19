using System.Collections.Generic;
using UnityEngine;

public class Caldeirao : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public class PotionPrefabEntry
    {
        public PotionType tipo;
        public GameObject prefab;
    }

    [Header("Configuração")]
    public float tempoDeFervura = 5f;
    [Tooltip("Um prefab de poção pra cada PotionType (Fire, Acid, Poison, Invisibility, Cure, Fast)")]
    public List<PotionPrefabEntry> potionPrefabs;

    [Header("Estado (só leitura, pra debug)")]
    public List<IngredientBase> ingredientesDentro = new List<IngredientBase>();
    public bool estaFervendo;
    public bool potionPronta;
    [Range(0f, 1f)] public float progresso;

    private float tempoDecorrido;
    private IngredientBase potionAtual;

    public void Interact(PlayerController player)
    {
        
        if (potionPronta)
        {
            if (player.HeldIngredient == null)
            {
                player.PickUpIngredient(potionAtual);
                potionPronta = false;
                potionAtual = null;
                Debug.Log("Poção retirada do caldeirão");
            }
            return;
        }

        if (estaFervendo) return;

        IngredientBase held = player.HeldIngredient;

        
        if (held != null)
        {
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
            item.transform.SetParent(transform);
            ingredientesDentro.Add(item);
            Debug.Log($"Caldeirão: {ingredientesDentro.Count}/2 ingredientes");
            return;
        }

        
        if (ingredientesDentro.Count == 2)
        {
            estaFervendo = true;
            tempoDecorrido = 0f;
            progresso = 0f;
            Debug.Log("Começou a ferver...");
        }
        else
        {
            Debug.Log($"Precisa de 2 ingredientes (tem {ingredientesDentro.Count})");
        }
    }

    void Update()
    {
        if (!estaFervendo) return;

        tempoDecorrido += Time.deltaTime;
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

        PotionType resultado = DeterminePotion();

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

        GameObject prefab = GetPrefabFor(resultado);
        if (prefab == null)
        {
            Debug.LogWarning($"Nenhum prefab configurado pra poção {resultado}");
            return;
        }

        GameObject spawned = Instantiate(prefab, transform.position, Quaternion.identity);
        potionAtual = spawned.GetComponent<IngredientBase>();
        potionAtual.gameObject.SetActive(false);
        potionPronta = true;

        Debug.Log($"Poção pronta: {resultado}! Aperta E pra pegar");
    }

    private PotionType DeterminePotion()
    {
        if (ingredientesDentro.Count != 2) return PotionType.None;

        IngredientKind a = ingredientesDentro[0].kind;
        IngredientKind b = ingredientesDentro[1].kind;

        if (Combo(a, b, IngredientKind.Camomila, IngredientKind.EscamaDeDragao)) return PotionType.Fire;
        if (Combo(a, b, IngredientKind.Camomila, IngredientKind.OlhoDeSalamandra)) return PotionType.Acid;
        if (Combo(a, b, IngredientKind.OlhoDeSalamandra, IngredientKind.EscamaDeDragao)) return PotionType.Poison;
        if (Combo(a, b, IngredientKind.MilhoDePipoca, IngredientKind.Camomila)) return PotionType.Invisibility;
        if (Combo(a, b, IngredientKind.MilhoDePipoca, IngredientKind.EscamaDeDragao)) return PotionType.Cure;
        if (Combo(a, b, IngredientKind.MilhoDePipoca, IngredientKind.OlhoDeSalamandra)) return PotionType.Fast;

        return PotionType.None;
    }

    private bool Combo(IngredientKind a, IngredientKind b, IngredientKind x, IngredientKind y)
    {
        return (a == x && b == y) || (a == y && b == x);
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