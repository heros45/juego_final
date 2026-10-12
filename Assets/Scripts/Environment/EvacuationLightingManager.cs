using System.Collections.Generic;
using UnityEngine;
using ChronosEscape.Core;

namespace ChronosEscape.Environment
{
    /// <summary>
    /// Sistema de Iluminación Dinámica de Evacuación.
    /// Crea una atmósfera de alerta con luces rojas pulsantes/parpadeantes leves durante la fase de búsqueda,
    /// y cambia a verde/cian estable cuando se reúnen los 5 núcleos de energía y se desbloquea el botón de escape.
    /// </summary>
    public class EvacuationLightingManager : MonoBehaviour
    {
        public static EvacuationLightingManager Instance { get; private set; }

        [Header("Luces de Emergencia")]
        [SerializeField] private List<Light> lucesEmergencia = new List<Light>();

        [Header("Parámetros de Alerta (Rojo Evacuación)")]
        [SerializeField] private Color colorAlerta1 = new Color(0.95f, 0.12f, 0.12f); // Rojo intenso
        [SerializeField] private Color colorAlerta2 = new Color(0.55f, 0.05f, 0.05f); // Rojo tenue
        [SerializeField] private float velocidadParpadeo = 2.2f;
        [SerializeField] private float intensidadMin = 0.8f;
        [SerializeField] private float intensidadMax = 2.6f;

        [Header("Parámetros de Éxito (Energía Restaurada)")]
        [SerializeField] private Color colorEstable = new Color(0.2f, 0.95f, 0.6f); // Verde/Cian operativo
        [SerializeField] private float intensidadEstable = 2.0f;

        private bool energiaRestaurada = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Auto-detectar luces de pilares o pasillos si no fueron asignadas manualmente
            if (lucesEmergencia.Count == 0)
            {
                BuscarLucesEnEscena();
            }
        }

        public void RegistrarLuz(Light luz)
        {
            if (luz != null && !lucesEmergencia.Contains(luz))
            {
                lucesEmergencia.Add(luz);
            }
        }

        private void BuscarLucesEnEscena()
        {
            Light[] todasLasLuces = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in todasLasLuces)
            {
                // Incluir luces de pilares y pasillos excepto luz solar principal o luces del jugador
                if (l.type == LightType.Point && l.transform.parent != null)
                {
                    string parentName = l.transform.parent.name;
                    if (parentName.Contains("Pillar") || parentName.Contains("Corridor") || parentName.Contains("Light"))
                    {
                        lucesEmergencia.Add(l);
                    }
                }
            }
        }

        private void Update()
        {
            if (energiaRestaurada) return;

            // Comprobar si el jugador ya reunió los núcleos requeridos
            if (GameManager.Instance != null && GameManager.Instance.NucleosRecolectados >= GameManager.Instance.TotalNucleosRequeridos)
            {
                ActivarModoEnergiaRestaurada();
                return;
            }

            // Efecto de evacuación: respiración/parpadeo suave de alerta roja
            float ciclo = (Mathf.Sin(Time.time * velocidadParpadeo) + 1f) * 0.5f;
            Color colorActual = Color.Lerp(colorAlerta2, colorAlerta1, ciclo);
            float intensidadActual = Mathf.Lerp(intensidadMin, intensidadMax, ciclo);

            for (int i = 0; i < lucesEmergencia.Count; i++)
            {
                if (lucesEmergencia[i] != null)
                {
                    lucesEmergencia[i].color = colorActual;
                    lucesEmergencia[i].intensity = intensidadActual;
                }
            }
        }

        /// <summary>
        /// Llamado cuando los 5 núcleos han sido obtenidos. Cambia las luces a verde de evacuación completada.
        /// </summary>
        public void ActivarModoEnergiaRestaurada()
        {
            if (energiaRestaurada) return;
            energiaRestaurada = true;

            for (int i = 0; i < lucesEmergencia.Count; i++)
            {
                if (lucesEmergencia[i] != null)
                {
                    lucesEmergencia[i].color = colorEstable;
                    lucesEmergencia[i].intensity = intensidadEstable;
                }
            }

            if (ChronosEscape.Audio.AudioManager.Instance != null)
            {
                ChronosEscape.Audio.AudioManager.Instance.SilenciarAlarma();
            }

            Debug.Log("[EvacuationLighting] ¡Energía sincronizada! Alarma silenciada y luces cambiadas a verde operativo.");
        }
    }
}
