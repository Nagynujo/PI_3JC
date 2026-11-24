using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrunasBrew.Lighting
{
    /// <summary>
    /// Armazena somente um backup da iluminacao ANTERIOR, para o Editor poder restaurar.
    /// Nao executa Update nem qualquer outro trabalho durante o jogo.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class BrunasBrewLightingRigData : MonoBehaviour
    {
        [Serializable]
        public struct OriginalLight
        {
            public Light source;
            public bool wasEnabled;

        }

        [HideInInspector] public bool hasBackup;
        [HideInInspector] public AmbientMode originalAmbientMode;
        [HideInInspector] public Color originalSky;
        [HideInInspector] public Color originalEquator;
        [HideInInspector] public Color originalGround;
        [HideInInspector] public float originalAmbientIntensity;
        [HideInInspector] public float originalReflectionIntensity;
        [HideInInspector] public List<OriginalLight> originalLights = new List<OriginalLight>();
        public Light cauldronLight; // Caldeirao 1; mantido para compatibilidade.
        public Light cauldronLight2; // Caldeirao 2; referencia para futuros efeitos de pocoes.
    }
}
