using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// Deixa a Bruna semitransparente por um tempo. Usa materiais URP/Lit.
public class InvisibilityEffect : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;      // todos os renderers do modelo
    [SerializeField, Range(0.05f, 1f)] private float alphaInvisivel = 0.35f;
    [SerializeField] private float fade = 0.4f;

    private Material[] mats;
    private Color[] original;
    private Coroutine rotina;

    private void Awake()
    {
        var lista = new System.Collections.Generic.List<Material>();
        foreach (var r in renderers) lista.AddRange(r.materials); // instâncias, não mexe nos assets
        mats = lista.ToArray();
        original = new Color[mats.Length];
        for (int i = 0; i < mats.Length; i++) original[i] = mats[i].GetColor("_BaseColor");
    }

    public void Ativar(float duracao)
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = StartCoroutine(Efeito(duracao));
    }

    private IEnumerator Efeito(float duracao)
    {
        foreach (var m in mats) SetTransparente(m, true);
        yield return Fade(1f, alphaInvisivel);
        yield return new WaitForSeconds(duracao);
        yield return Fade(alphaInvisivel, 1f);
        foreach (var m in mats) SetTransparente(m, false);
        rotina = null;
    }

    private IEnumerator Fade(float de, float para)
    {
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(de, para, t / fade));
            yield return null;
        }
        SetAlpha(para);
    }

    private void SetAlpha(float a)
    {
        for (int i = 0; i < mats.Length; i++)
        {
            Color c = original[i]; c.a = a;
            mats[i].SetColor("_BaseColor", c);
        }
    }

    private static void SetTransparente(Material m, bool on)
    {
        m.SetFloat("_Surface", on ? 1f : 0f);
        m.SetOverrideTag("RenderType", on ? "Transparent" : "Opaque");
        m.SetInt("_SrcBlend", (int)(on ? BlendMode.SrcAlpha : BlendMode.One));
        m.SetInt("_DstBlend", (int)(on ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
        m.SetInt("_ZWrite", on ? 0 : 1);
        if (on) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); else m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = on ? (int)RenderQueue.Transparent : -1;
    }
}