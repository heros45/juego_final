using UnityEngine;
using ChronosEscape.Audio;
using ChronosEscape.Core;
using ChronosEscape.Player;

namespace ChronosEscape.Environment
{
    /// <summary>
    /// Mecanismo final de escape: Portal Cuántico que se activa al reunir los 5 núcleos de energía.
    /// Incorpora doble comprobación (Trigger de físicas + distancia en Update) para garantizar
    /// que la victoria se active siempre sin fallos.
    /// </summary>
    public class EscapePortal : MonoBehaviour
    {
        [Header("Referencias Visuales")]
        [SerializeField] private ParticleSystem particulasPortal;
        [SerializeField] private Light luzPortal;
        [SerializeField] private Renderer renderAnillo;
        [SerializeField] private GameObject hazDeLuz;

        [Header("Compuerta y Botón de Pared")]
        [SerializeField] private Renderer renderBotonPared;
        [SerializeField] private Light luzBotonPared;
        [SerializeField] private Transform panelPuertaIzq;
        [SerializeField] private Transform panelPuertaDer;

        [Header("Colores")]
        [SerializeField] private Color colorBloqueado = new Color(0.9f, 0.15f, 0.15f); // Rojo
        [SerializeField] private Color colorActivado = new Color(0.1f, 1f, 0.5f);     // Verde brillante

        [Header("Radio de Detección de Victoria")]
        [SerializeField] private float radioVictoria = 6.0f;

        [Header("Estado")]
        [SerializeField] private bool portalActivado = false;

        private Transform jugadorTransform;
        private bool haGanado = false;

        public bool IsActive => portalActivado;
        public bool PortalActivado => portalActivado;

        public void ConfigurarReferencias(ParticleSystem particulas, Light luz, Renderer anillo, GameObject haz)
        {
            particulasPortal = particulas;
            luzPortal = luz;
            renderAnillo = anillo;
            hazDeLuz = haz;
        }

        public void ConfigurarCompuertaYBoton(Renderer boton, Light luzBoton, Transform puertaIzq, Transform puertaDer)
        {
            renderBotonPared = boton;
            luzBotonPared = luzBoton;
            panelPuertaIzq = puertaIzq;
            panelPuertaDer = puertaDer;
        }

        private void Start()
        {
            BuscarReferenciaJugador();
            ConfigurarVisualBloqueado();
        }

        private void BuscarReferenciaJugador()
        {
            if (PlayerController.Instance != null)
            {
                jugadorTransform = PlayerController.Instance.transform;
            }
            else
            {
                GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
                if (jugadorObj != null) jugadorTransform = jugadorObj.transform;
            }
        }

        [Header("Radio de Interacción")]
        [SerializeField] private float radioInteraccion = 4.0f;

        private void Update()
        {
            if (haGanado) return;

            if (jugadorTransform == null)
            {
                BuscarReferenciaJugador();
                if (jugadorTransform == null) return;
            }

            float distancia = Vector3.Distance(transform.position, jugadorTransform.position);

            if (distancia <= radioInteraccion)
            {
                if (portalActivado)
                {
                    // Consola lista en verde: feedback claro para que el jugador presione E
                    if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                    {
                        GameManager.Instance.UIManager.ShowNotification("Presiona [E] para PULSAR EL BOTÓN DE EVACUACIÓN Y ESCAPAR", 0.3f);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        DispararEscape();
                    }
                }
                else
                {
                    int recolectados = GameManager.Instance != null ? GameManager.Instance.NucleosRecolectados : 0;
                    int requeridos = GameManager.Instance != null ? GameManager.Instance.TotalNucleosRequeridos : 5;
                    if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                    {
                        GameManager.Instance.UIManager.ShowNotification($"Consola de evacuación bloqueada [{recolectados}/{requeridos} Núcleos]. Consigue todos para desbloquear el botón.", 0.3f);
                    }
                }
            }
        }

        private void ConfigurarVisualBloqueado()
        {
            if (particulasPortal != null) particulasPortal.Stop();
            if (hazDeLuz != null) hazDeLuz.SetActive(false);

            if (luzPortal != null)
            {
                luzPortal.color = colorBloqueado;
                luzPortal.intensity = 1.5f;
                luzPortal.range = 8f;
            }
            if (renderAnillo != null && renderAnillo.material != null)
            {
                renderAnillo.material.color = colorBloqueado;
                if (renderAnillo.material.HasProperty("_EmissionColor"))
                {
                    renderAnillo.material.SetColor("_EmissionColor", colorBloqueado * 1.0f);
                }
            }

            // Botón de pared en rojo bloqueado
            if (renderBotonPared != null && renderBotonPared.material != null)
            {
                renderBotonPared.material.color = colorBloqueado;
                if (renderBotonPared.material.HasProperty("_EmissionColor"))
                {
                    renderBotonPared.material.SetColor("_EmissionColor", colorBloqueado * 1.5f);
                }
            }
            if (luzBotonPared != null)
            {
                luzBotonPared.color = colorBloqueado;
                luzBotonPared.intensity = 1.5f;
            }
        }

        public void ActivatePortal() => ActivarPortal();

        public void ActivarPortal()
        {
            if (portalActivado) return;
            portalActivado = true;

            if (particulasPortal != null) particulasPortal.Play();

            if (hazDeLuz != null)
            {
                hazDeLuz.SetActive(true);
                Renderer hazRenderer = hazDeLuz.GetComponent<Renderer>();
                if (hazRenderer != null && hazRenderer.material != null)
                {
                    hazRenderer.material.color = colorActivado;
                    if (hazRenderer.material.HasProperty("_EmissionColor"))
                    {
                        hazRenderer.material.SetColor("_EmissionColor", colorActivado * 2.5f);
                    }
                }
            }

            if (luzPortal != null)
            {
                luzPortal.color = colorActivado;
                luzPortal.intensity = 5.0f;
                luzPortal.range = 16f;
            }

            if (renderAnillo != null && renderAnillo.material != null)
            {
                renderAnillo.material.color = colorActivado;
                if (renderAnillo.material.HasProperty("_EmissionColor"))
                {
                    renderAnillo.material.SetColor("_EmissionColor", colorActivado * 3.0f);
                }
            }

            // Botón de pared cambia a VERDE BRILLANTE de evacuación
            if (renderBotonPared != null && renderBotonPared.material != null)
            {
                renderBotonPared.material.color = colorActivado;
                if (renderBotonPared.material.HasProperty("_EmissionColor"))
                {
                    renderBotonPared.material.SetColor("_EmissionColor", colorActivado * 3.5f);
                }
            }
            if (luzBotonPared != null)
            {
                luzBotonPared.color = colorActivado;
                luzBotonPared.intensity = 4.0f;
                luzBotonPared.range = 7f;
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPortalOpen();
            }

            Debug.Log("[EscapePortal] ¡Botón y compuerta desbloqueados en verde! Presiona [E] en la pared sur para escapar.");
        }

        private void OnTriggerStay(Collider other)
        {
            if (haGanado) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null)
            {
                if (portalActivado)
                {
                    if (GameManager.Instance != null && GameManager.Instance.UIManager != null)
                    {
                        GameManager.Instance.UIManager.ShowNotification("Presiona [E] para PULSAR EL BOTÓN Y ABRIR LA COMPUERTA", 0.3f);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        DispararEscape();
                    }
                }
            }
        }

        private void DispararEscape()
        {
            if (haGanado) return;
            haGanado = true;

            StartCoroutine(RutinaAbrirCompuertaYVictoria());
        }

        private System.Collections.IEnumerator RutinaAbrirCompuertaYVictoria()
        {
            Debug.Log("[EscapePortal] ¡Botón presionado! Abriendo compuerta de evacuación...");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPortalOpen();
            }

            // Animación de deslizamiento de las hojas de la compuerta
            float tiempo = 0f;
            float duracion = 0.8f;
            Vector3 posIzqIni = panelPuertaIzq != null ? panelPuertaIzq.localPosition : Vector3.zero;
            Vector3 posDerIni = panelPuertaDer != null ? panelPuertaDer.localPosition : Vector3.zero;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float progreso = Mathf.Clamp01(tiempo / duracion);

                if (panelPuertaIzq != null)
                {
                    panelPuertaIzq.localPosition = posIzqIni + new Vector3(0, 0, 2.0f * progreso);
                }
                if (panelPuertaDer != null)
                {
                    panelPuertaDer.localPosition = posDerIni + new Vector3(0, 0, -2.0f * progreso);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            Debug.Log("[EscapePortal] ¡Compuerta abierta! Victoria confirmada.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = portalActivado ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, radioVictoria);
        }
    }
}
