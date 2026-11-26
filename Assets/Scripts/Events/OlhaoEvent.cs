using System.Collections;
using UnityEngine;

// =====================================================================================
// [LUCCA - SOM] RESUMO DAS ALTERAÇÕES NESTE ARQUIVO (procure por "[LUCCA - SOM]")
//   1. Novo campo privado 'olhaoAudio' (referência ao componente OlhaoAudio, ARQUIVO NOVO).
//   2. Awake(): pega o OlhaoAudio que está no MESMO objeto deste OlhaoEvent.
//   3. Run(): toca OlhaoAudio.Aparecer() quando o olho aparece (som + liga o loop do scanner).
//   4. OnScanned(): toca OlhaoAudio.Detectar() quando a Bruna é pega pelo cone e paralisada.
//   5. ParalyzePlayer(): toca OlhaoAudio.Liberar() quando a paralisia ACABA SOZINHA
//      (se o evento for resolvido no meio da paralisia, esse som não toca).
//   6. OnResolving(): toca OlhaoAudio.Sumir() quando o evento é resolvido com a poção
//      (desliga o loop e toca o som de sumir, junto com o olho encolhendo).
//   7. OnForceEnded(): chama OlhaoAudio.Parar() (desliga o loop, sem tocar som).
//
// COMO FUNCIONA:
//   O OlhaoAudio fica no MESMO objeto do OlhaoEvent. Ele lê daqui o 'eye' e o 'player'
//   pra saber onde tocar cada som. Se não houver OlhaoAudio no objeto, o evento funciona
//   normalmente, só sem som (nenhum erro).
//
// ONDE CONFIGURAR:
//   No objeto do OlhaoEvent: Add Component > OlhaoAudio e escolher os eventos do FMOD
//   no Inspector. Nada mais precisa mudar (ScannerDetector e ScannerConeVisual continuam iguais).
//
// Todo o resto do arquivo está igual à versão anterior.
// =====================================================================================

/// <summary>
/// Evento Olhão: olho na área de entrega que varre a sala por partes com um cone de luz visível.
/// Quem o cone pega (a Bruna) fica paralisada por 2 a 3 segundos. Detecção em ScannerDetector,
/// separada do visual. Resolvido com a poção Invisibility aplicada no alvo (EventPotionTarget).
/// </summary>
public class OlhaoEvent : GameEventBase
{
    [Header("Referências")]
    public Transform eye;
    [Tooltip("Âncora da área de entrega. Vazio = usa a DeliveryStation da cena. O olho aparece em anchor + offset.")]
    public Transform deliveryAnchor;
    public Vector3 anchorOffset = new Vector3(0f, 2.5f, 0f);
    [Tooltip("Pontos da sala que o olho vai examinar, um de cada vez.")]
    public Transform[] scanPoints;
    public Light scannerLight;                 // Spot Light filha do olho, apontando para +Z
    public ScannerConeVisual coneVisual;       // cone translúcido filho do olho
    public ScannerDetector detector;           // opcional (criado sozinho se vazio)
    [Tooltip("Quem o cone paralisa. Vazio = encontra o PlayerController da cena no Awake.")]
    public Transform player;
    [Tooltip("Mostra no Console quando a Bruna entra no cone (para depurar).")]
    public bool debugLog = true;
    public GameObject appearVfx;               // opcional

    [Header("Cone")]
    [Range(5f, 120f)] public float coneAngle = 35f;
    public float range = 10f;
    public float lightIntensity = 3f;
    public Color lightColor = new Color(1f, 0.92f, 0.6f);

    [Header("Varredura")]
    [Tooltip("Graus por segundo.")] public float scanSpeed = 55f;
    public float easeAngle = 25f;
    public float pauseTime = 1.5f;
    public bool randomOrder = true;
    [Tooltip("Limites de giro relativos à direção da âncora.")]
    public float maxYaw = 80f;
    public float minPitch = -75f;
    public float maxPitch = 10f;

    [Header("Paralisia (quando a Bruna é pega pelo cone)")]
    [Tooltip("A cada vez, a paralisia dura um valor sorteado entre Min e Max (segundos).")]
    public float paralysisDurationMin = 2f;
    public float paralysisDurationMax = 3f;
    [Tooltip("Tempo após a paralisia em que ela não pode ser paralisada de novo (evita sequência infinita).")]
    public float paralysisCooldown = 3f;
    [Tooltip("Efeito na Bruna enquanto paralisada (estrelas, gelo, ícone). Opcional.")]
    public GameObject paralysisIndicator;

    [Header("Resolução (alvo da poção Invisibility)")]
    [Tooltip("Objeto com Collider (Is Trigger) + EventPotionTarget (Target Event = este OlhaoEvent). Ativado só durante o evento. Required Potion deve ser Invisibility.")]
    public GameObject potionTarget;

    [Header("Aparição")]
    public float appearDuration = 0.8f;

    [Header("Encerramento")]
    [Tooltip("0 = só termina com a poção correta (Invisibility). Valor > 0 = termina sozinho após esse tempo (útil só para testes).")]
    public float autoEndAfter = 0f;

    private Vector3 eyeScale;
    private Coroutine main, scan, paralysis;
    private float nextParalysisAllowed;

    // [LUCCA - SOM] NOVO: sons do Olhão (FMOD). Opcional: fica null se não houver OlhaoAudio no objeto.
    private OlhaoAudio olhaoAudio;

    private void Awake()
    {
        // [LUCCA - SOM] NOVO: procura o OlhaoAudio no mesmo objeto (pode não existir).
        olhaoAudio = GetComponent<OlhaoAudio>();

        if (paralysisIndicator != null) paralysisIndicator.SetActive(false);
        if (potionTarget != null) potionTarget.SetActive(false);
        if (player == null)
        {
#if UNITY_2023_1_OR_NEWER
            var pc = FindFirstObjectByType<PlayerController>();
#else
            var pc = FindObjectOfType<PlayerController>();
#endif
            if (pc != null) player = pc.transform;
        }
        if (deliveryAnchor == null)
        {   // reaproveita a DeliveryStation existente como âncora
#if UNITY_2023_1_OR_NEWER
            var ds = FindFirstObjectByType<DeliveryStation>();
#else
            var ds = FindObjectOfType<DeliveryStation>();
#endif
            if (ds != null) deliveryAnchor = ds.transform;
        }
        if (eye != null)
        {
            eyeScale = eye.localScale;
            // O olho nunca deve bloquear a interação de entrega.
            foreach (var c in eye.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            eye.gameObject.SetActive(false);
        }
    }

    protected override void OnActivated()
    {
        if (eye == null || deliveryAnchor == null) { Debug.LogError("[OlhaoEvent] Configure eye e deliveryAnchor.", this); return; }
        if (potionTarget != null) potionTarget.SetActive(true);
        main = StartCoroutine(Run());
    }

    private Quaternion BaseRotation
    {
        get
        {
            Vector3 f = deliveryAnchor.forward; f.y = 0f;
            return f.sqrMagnitude > 0.001f ? Quaternion.LookRotation(f) : Quaternion.identity;
        }
    }

    private IEnumerator Run()
    {
        eye.position = deliveryAnchor.position + anchorOffset;
        eye.rotation = BaseRotation;
        eye.localScale = Vector3.zero;
        eye.gameObject.SetActive(true);
        if (appearVfx != null) Destroy(Instantiate(appearVfx, eye.position, Quaternion.identity), 3f);

        // [LUCCA - SOM] NOVO: som de o olho aparecer + liga o loop do scanner.
        // Fica DEPOIS de posicionar o olho (acima), pra o som sair no lugar certo.
        if (olhaoAudio != null) olhaoAudio.Aparecer();

        if (scannerLight != null)
        {
            scannerLight.type = LightType.Spot; scannerLight.spotAngle = coneAngle;
            scannerLight.range = range; scannerLight.color = lightColor; scannerLight.intensity = 0f;
        }
        if (coneVisual != null) { coneVisual.Rebuild(range, coneAngle); coneVisual.gameObject.SetActive(true); }

        if (detector == null)
        {   // detecção embutida: sem configuração extra no Inspector
            detector = eye.gameObject.AddComponent<ScannerDetector>();
        }
        detector.origin = eye;
        if (player != null) detector.trackedTargets = new[] { player };
        else Debug.LogWarning("[OlhaoEvent] Player não encontrado: a paralisia não vai funcionar.", this);
        detector.Configure(range, coneAngle);
        detector.TargetEntered -= OnScanned;
        detector.TargetEntered += OnScanned;
        nextParalysisAllowed = 0f;

        for (float e = 0f; e < appearDuration; e += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, e / appearDuration);
            eye.localScale = eyeScale * k;
            if (scannerLight != null) scannerLight.intensity = lightIntensity * k;
            yield return null;
        }
        eye.localScale = eyeScale;
        if (scannerLight != null) scannerLight.intensity = lightIntensity;

        scan = StartCoroutine(ScanLoop());
        detector.detectionEnabled = true; // detecção só começa com o olho totalmente aberto
        if (autoEndAfter > 0f) { yield return new WaitForSeconds(autoEndAfter); Resolve(); }
    }

    private IEnumerator ScanLoop()
    {
        int last = -1;
        bool hasPoints = scanPoints != null && scanPoints.Length > 0;
        float side = 1f;
        while (true)
        {
            Quaternion target;
            if (hasPoints)
            {
                int idx;
                if (randomOrder && scanPoints.Length > 1)
                { do { idx = Random.Range(0, scanPoints.Length); } while (idx == last); }
                else idx = (last + 1) % scanPoints.Length;
                last = idx;
                target = RotationToward(scanPoints[idx].position);
            }
            else
            {   // sem pontos: alterna esquerda/direita dentro dos limites
                side = -side;
                target = BaseRotation * Quaternion.Euler(0f, side * Random.Range(0.4f, 1f) * maxYaw, 0f);
            }

            while (Quaternion.Angle(eye.rotation, target) > 0.5f)
            {
                float ang = Quaternion.Angle(eye.rotation, target);
                float step = scanSpeed * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(ang / Mathf.Max(1f, easeAngle))) * Time.deltaTime;
                eye.rotation = Quaternion.RotateTowards(eye.rotation, target, step); // transição suave
                yield return null;
            }
            yield return new WaitForSeconds(pauseTime);
        }
    }

    // Converte a direção até o ponto em yaw/pitch relativos à âncora e aplica os limites.
    private Quaternion RotationToward(Vector3 point)
    {
        Quaternion baseRot = BaseRotation;
        Vector3 local = Quaternion.Inverse(baseRot) * (point - eye.position).normalized;
        float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg; // + = para cima
        yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        return baseRot * Quaternion.Euler(-pitch, yaw, 0f);
    }

    // Gameplay do scanner: separado da apresentação visual.
    private void OnScanned(Transform other)
    {
        if (!IsActive || Time.time < nextParalysisAllowed) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (paralysis != null) return;
        if (debugLog) Debug.Log("[Olhão] Bruna no cone: paralisada!");

        // [LUCCA - SOM] NOVO: som de a Bruna ser pega pelo cone. Fica DEPOIS de todas as
        // checagens acima, então só toca quando a paralisia realmente vai começar
        // (o cooldown de paralisia também evita o som se repetir em sequência).
        if (olhaoAudio != null) olhaoAudio.Detectar();

        paralysis = StartCoroutine(ParalyzePlayer());
    }

    private IEnumerator ParalyzePlayer()
    {
        EventModifiers.SetParalyzed(this, true);
        if (paralysisIndicator != null) paralysisIndicator.SetActive(true);
        yield return new WaitForSeconds(Random.Range(paralysisDurationMin, Mathf.Max(paralysisDurationMin, paralysisDurationMax)));

        // [LUCCA - SOM] NOVO: som de a paralisia acabar. Só chega aqui quando a paralisia termina
        // sozinha: se o evento for resolvido/encerrado antes, ReleasePlayer() para esta
        // coroutine e esta linha não roda.
        if (olhaoAudio != null) olhaoAudio.Liberar();

        ReleasePlayer();
    }

    private void ReleasePlayer()
    {
        if (paralysis != null) StopCoroutine(paralysis);
        paralysis = null;
        EventModifiers.SetParalyzed(this, false);
        if (paralysisIndicator != null) paralysisIndicator.SetActive(false);
        nextParalysisAllowed = Time.time + paralysisCooldown;
    }

    protected override IEnumerator OnResolving()
    {
        ReleasePlayer(); // nenhum efeito persiste após o evento

        // [LUCCA - SOM] NOVO: desliga o loop do scanner e toca o som de sumir, junto com o
        // olho começando a encolher (animação logo abaixo).
        if (olhaoAudio != null) olhaoAudio.Sumir();

        if (potionTarget != null) potionTarget.SetActive(false);
        StopRoutines();
        if (detector != null) detector.detectionEnabled = false;
        if (eye != null)
        {
            float startI = scannerLight != null ? scannerLight.intensity : 0f;
            for (float e = 0f; e < appearDuration; e += Time.deltaTime)
            {
                float k = 1f - Mathf.SmoothStep(0f, 1f, e / appearDuration);
                eye.localScale = eyeScale * k;
                if (scannerLight != null) scannerLight.intensity = startI * k;
                yield return null;
            }
        }
        Hide();
    }

    // [LUCCA - SOM] ALTERADO: antes era uma linha só ({ ReleasePlayer(); StopRoutines(); Hide(); }).
    // Agora desliga também o loop do scanner, sem tocar nenhum som (fim forçado/reinício).
    protected override void OnForceEnded()
    {
        ReleasePlayer();
        StopRoutines();
        Hide();
        if (olhaoAudio != null) olhaoAudio.Parar();
    }

    private void StopRoutines()
    {
        if (main != null) StopCoroutine(main);
        if (scan != null) StopCoroutine(scan);
        main = scan = null;
    }

    private void Hide()
    {
        if (potionTarget != null) potionTarget.SetActive(false);
        if (detector != null) { detector.detectionEnabled = false; detector.TargetEntered -= OnScanned; }
        if (coneVisual != null) coneVisual.gameObject.SetActive(false);
        if (scannerLight != null) scannerLight.intensity = 0f;
        if (eye != null) { eye.localScale = eyeScale; eye.gameObject.SetActive(false); }
    }
}