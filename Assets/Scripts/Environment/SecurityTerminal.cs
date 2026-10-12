using UnityEngine;
using ChronosEscape.Audio;
using ChronosEscape.Core;
using ChronosEscape.Player;

namespace ChronosEscape.Environment
{
    /// <summary>
    /// Terminal interactiva de seguridad y energía.
    /// Permite al jugador interactuar con la tecla [E] para restaurar salud (+30 HP).
    /// </summary>
    public class SecurityTerminal : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private string nombreTerminal = "Consola de Seguridad Central";
        [SerializeField] private float radioInteraccion = 3.5f;
        [SerializeField] private Light luzTerminal;
        [SerializeField] private Renderer renderPantalla;

        [Header("Colores")]
        [SerializeField] private Color colorEspera = new Color(0f, 0.8f, 1f);   // Cian
        [SerializeField] private Color colorActivado = new Color(0.2f, 1f, 0.4f); // Verde

        private bool estaActivada = false;
        private Transform jugadorTransform;

        public void ConfigurarReferencias(Light luz, Renderer pantalla)
        {
            luzTerminal = luz;
            renderPantalla = pantalla;
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                jugadorTransform = PlayerController.Instance.transform;
            }
            ActualizarAspectoVisual(false);
        }

        private void Update()
        {
            if (jugadorTransform == null)
            {
                if (PlayerController.Instance != null) jugadorTransform = PlayerController.Instance.transform;
                return;
            }

            float distancia = Vector3.Distance(transform.position, jugadorTransform.position);
            if (distancia <= radioInteraccion)
            {
                if (!estaActivada)
                {
                    if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                    {
                        GameManager.Instance.UIManager.ShowNotification("Presiona [E] para activar la terminal y recuperar salud", 0.5f);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        Interactuar();
                    }
                }
            }
        }

        public void Interact() => Interactuar();

        public void Interactuar()
        {
            if (estaActivada) return;
            estaActivada = true;

            ActualizarAspectoVisual(true);

            // Audio de confirmación
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickup();
            }

            // Restaurar 30 puntos de salud al jugador
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.Curar(30f);
            }

            if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
            {
                GameManager.Instance.UIManager.ShowNotification("¡Terminal activada! Sistemas sincronizados y +30 HP restaurados.", 3.5f);
            }

            Debug.Log("[SecurityTerminal] Terminal activada con la tecla E.");
        }

        private void ActualizarAspectoVisual(bool activa)
        {
            Color colorFinal = activa ? colorActivado : colorEspera;
            if (luzTerminal != null)
            {
                luzTerminal.color = colorFinal;
                luzTerminal.intensity = activa ? 3.0f : 1.5f;
            }
            if (renderPantalla != null && renderPantalla.material != null)
            {
                renderPantalla.material.color = colorFinal;
                if (renderPantalla.material.HasProperty("_EmissionColor"))
                {
                    renderPantalla.material.SetColor("_EmissionColor", colorFinal * 1.5f);
                }
            }
        }
    }
}
