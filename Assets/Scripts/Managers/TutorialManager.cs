using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialStep
{
    [Tooltip("Identificador único. Ex.: intro_mover, cortar_moer, caldeirao, contratempo_ratos")]
    public string id;
    public string title;
    [TextArea(2, 6)] public string description;
    [Tooltip("Imagem opcional para ilustrar o tutorial.")]
    public Sprite image;
    [Tooltip("Se marcado, o jogo pausa (e o cronômetro para) enquanto o tutorial está aberto.")]
    public bool pauseGame = true;
}

/// <summary>
/// Sistema de tutoriais:
///  - INTRO: lista de ids que aparecem em sequência assim que o jogo começa, sem nenhum trigger.
///  - EVENTOS: qualquer outro tutorial só aparece quando algo chama Trigger("id"), e só na primeira vez.
/// Se vários forem disparados juntos, entram numa fila.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Tutorial inicial (aparece sozinho ao começar)")]
    [Tooltip("Ids na ordem em que devem aparecer ao iniciar. Ex.: intro_mover, intro_interagir, intro_objetivo")]
    [SerializeField] private List<string> introIds = new List<string>();
    [Tooltip("Se marcado, o tutorial inicial aparece toda vez que a cena começa.")]
    [SerializeField] private bool showIntroEveryTime = true;

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image illustration;
    [SerializeField] private Button closeButton;
    [Tooltip("Opcional: texto do botão. Mostra 'Próximo' enquanto há mais tutoriais na fila e 'Entendi' no último.")]
    [SerializeField] private TMP_Text closeButtonText;

    [Header("Opções")]
    [Tooltip("Se marcado, tutoriais de evento já vistos não aparecem de novo nem ao fechar e abrir o jogo.")]
    [SerializeField] private bool rememberBetweenSessions = false;

    private const string PrefsPrefix = "Tutorial_";

    private readonly HashSet<string> shown = new HashSet<string>();
    private readonly Queue<TutorialStep> queue = new Queue<TutorialStep>();
    private TutorialStep current;
    private float timeScaleBeforeTutorial = 1f;

    /// <summary>True enquanto um tutorial está na tela. Use para bloquear andar/interagir.</summary>
    public bool IsOpen => current != null;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        closeButton.onClick.AddListener(CloseCurrent);

        // Awake roda antes de todos os Start da cena, então o tutorial inicial sempre vem primeiro na fila.
        foreach (string id in introIds)
            Trigger(id, force: showIntroEveryTime);
    }

    private void OnDestroy()
    {
        // garante que o jogo não fique pausado ao trocar de cena com um tutorial aberto
        if (current != null) Time.timeScale = timeScaleBeforeTutorial;
        if (Instance == this) Instance = null;
    }

    /// <summary>Chame quando o evento acontecer. Só mostra se ainda não foi visto.</summary>
    public void Trigger(string id, bool force = false)
    {
        if (!force && HasBeenShown(id)) return;

        TutorialStep step = steps.Find(s => s.id == id);
        if (step == null)
        {
            Debug.LogWarning($"[Tutorial] Nenhum tutorial com o id '{id}'.");
            return;
        }

        MarkAsShown(id);
        queue.Enqueue(step);

        if (current == null) ShowNext();
    }

    /// <summary>Reabre um tutorial mesmo já visto (ex.: botão "?" ou "Ajuda").</summary>
    public void Show(string id) => Trigger(id, force: true);

    private void ShowNext()
    {
        if (queue.Count == 0)
        {
            current = null;
            panel.SetActive(false);
            Time.timeScale = timeScaleBeforeTutorial;
            return;
        }

        if (!panel.activeSelf) timeScaleBeforeTutorial = Time.timeScale;

        current = queue.Dequeue();
        titleText.text = current.title;
        descriptionText.text = current.description;

        illustration.gameObject.SetActive(current.image != null);
        illustration.sprite = current.image;

        if (closeButtonText != null)
            closeButtonText.text = queue.Count > 0 ? "Próximo" : "Entendi";

        panel.SetActive(true);
        Time.timeScale = current.pauseGame ? 0f : timeScaleBeforeTutorial;
    }

    private void CloseCurrent()
    {
        ShowNext();
    }

    private bool HasBeenShown(string id)
    {
        if (shown.Contains(id)) return true;
        return rememberBetweenSessions && PlayerPrefs.GetInt(PrefsPrefix + id, 0) == 1;
    }

    private void MarkAsShown(string id)
    {
        shown.Add(id);
        if (rememberBetweenSessions)
        {
            PlayerPrefs.SetInt(PrefsPrefix + id, 1);
            PlayerPrefs.Save();
        }
    }

    [ContextMenu("Resetar tutoriais vistos")]
    private void ResetSeenTutorials()
    {
        shown.Clear();
        foreach (var s in steps) PlayerPrefs.DeleteKey(PrefsPrefix + s.id);
        PlayerPrefs.Save();
    }
}
