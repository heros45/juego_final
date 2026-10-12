using UnityEngine;
using ChronosEscape.Core;

namespace ChronosEscape.Environment
{
    /// <summary>
    /// Foco de Sirena Roja Giratoria de Evacuación.
    /// Emite un haz de luz Spotlight giratorio que barre el techo, suelo y paredes en rojo de emergencia.
    /// Al reunir los 5 núcleos, la sirena se detiene y cambia a una luz verde de estado operativo.
    /// </summary>
    public class RotatingEmergencySiren : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Transform cabezaGiro;
        [SerializeField] private Light luzSpotGiro;
        [SerializeField] private Light luzPuntoDomo;
        [SerializeField] private Renderer renderDomo;

        [Header("Configuración de Giro")]
        [SerializeField] private float velocidadGiro = 280f; // Grados por segundo
        [SerializeField] private Color colorAlerta = new Color(1f, 0.08f, 0.08f); // Rojo sirena
        [SerializeField] private Color colorOperativo = new Color(0.2f, 0.95f, 0.5f); // Verde resuelto

        private bool emergenciaActiva = true;

        public void Configurar(Transform cabeza, Light spot, Light punto, Renderer domo)
        {
            cabezaGiro = cabeza;
            luzSpotGiro = spot;
            luzPuntoDomo = punto;
            renderDomo = domo;
        }

        private void Start()
        {
            ActualizarColores(colorAlerta, 2.5f);
        }

        private void Update()
        {
            // Comprobar estado de victoria / núcleos reunidos
            if (emergenciaActiva && GameManager.Instance != null && GameManager.Instance.NucleosRecolectados >= GameManager.Instance.TotalNucleosRequeridos)
            {
                DesactivarSirenaEmergencia();
            }

            if (emergenciaActiva)
            {
                // Rotación continua del cabezal de la sirena
                if (cabezaGiro != null)
                {
                    cabezaGiro.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.Self);
                }
            }
        }

        public void DesactivarSirenaEmergencia()
        {
            if (!emergenciaActiva) return;
            emergenciaActiva = false;

            // Apagar el foco giratorio y dejar una luz verde calmada en el domo
            if (luzSpotGiro != null) luzSpotGiro.enabled = false;
            ActualizarColores(colorOperativo, 1.2f);
        }

        private void ActualizarColores(Color color, float intensidadPunto)
        {
            if (luzSpotGiro != null)
            {
                luzSpotGiro.color = color;
            }
            if (luzPuntoDomo != null)
            {
                luzPuntoDomo.color = color;
                luzPuntoDomo.intensity = intensidadPunto;
            }
            if (renderDomo != null && renderDomo.material != null)
            {
                renderDomo.material.color = color;
                if (renderDomo.material.HasProperty("_EmissionColor"))
                {
                    renderDomo.material.SetColor("_EmissionColor", color * 2.5f);
                }
            }
        }
    }
}
