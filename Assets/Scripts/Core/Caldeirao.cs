using System.Collections.Generic;
using UnityEngine;

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

    [Header("Efeitos")]
    [Tooltip("Partícula tocada enquanto o caldeirão está fervendo (bolhas/vapor)")]
    public ParticleSystem particulaFervura;

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

            if (particulaFervura != null)
            {
                particulaFervura.Play();
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

        potionPronta = false;
        potionProntaTipo = PotionType.None;
        Debug.Log("Garrafa engarrafada com a poção!");
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

        if (particulaFervura != null)
        {
            particulaFervura.Stop();
        }

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

        potionProntaTipo = resultado;
        potionPronta = true;

        Debug.Log($"Poção pronta: {resultado}! Segura uma garrafa vazia e aperta E pra encher");
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