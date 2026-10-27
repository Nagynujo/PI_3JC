using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configurações simples. Hoje só tem volume geral; dá pra adicionar mais
/// sliders/toggles seguindo o mesmo padrão (ler valor salvo no Start, escrever
/// no onValueChanged).
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Referências (arrastar na cena)")]
    public Slider sliderVolume;

    private const string ChaveVolume = "VolumeGeral";

    void Start()
    {
        float volumeSalvo = PlayerPrefs.GetFloat(ChaveVolume, 1f);
        AudioListener.volume = volumeSalvo;

        if (sliderVolume != null)
        {
            sliderVolume.value = volumeSalvo;
            sliderVolume.onValueChanged.AddListener(DefinirVolume);
        }
    }

    public void DefinirVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ChaveVolume, valor);
    }
}
