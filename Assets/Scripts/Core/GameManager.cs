using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChronosEscape.Audio;
using ChronosEscape.Environment;
using ChronosEscape.Items;
using ChronosEscape.UI;

namespace ChronosEscape.Core
{
    public enum EstadoJuego
    {
        Jugando,
        Pausado,
        Victoria,
        Derrota
    }

    public enum GameState
    {
        Playing,
        Paused,
        Victory,
        Defeat
    }

    /// <summary>
    /// Administrador principal del ciclo de vida del juego, objetivos, puntuación y condiciones de fin de partida.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuración de Misión")]
        [SerializeField] private int totalNucleosRequeridos = 5;
        [SerializeField] private int nucleosRecolectados = 0;

        [Header("Referencias")]
        [SerializeField] private EscapePortal portalEscape;
        [SerializeField] private GameUIManager gestorUI;

        private EstadoJuego estadoActual = EstadoJuego.Jugando;
        private float temporizadorJuego = 0f;

        // Propiedades públicas en español e inglés
        public EstadoJuego EstadoActual => estadoActual;
        public GameState CurrentState => (GameState)estadoActual;
        public bool TerminoElJuego => estadoActual == EstadoJuego.Victoria || estadoActual == EstadoJuego.Derrota;
        public bool IsGameEnded => TerminoElJuego;
        public GameUIManager UIManager => gestorUI;
        public int NucleosRecolectados => nucleosRecolectados;
        public int CoresCollected => nucleosRecolectados;
        public int TotalNucleosRequeridos => totalNucleosRequeridos;
        public int TotalCoresRequired => totalNucleosRequeridos;

        public void ConfigurarReferencias(EscapePortal portal, GameUIManager ui)
        {
            portalEscape = portal;
            gestorUI = ui;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Time.timeScale = 1.0f;
        }

        private void Start()
        {
            if (gestorUI == null)
            {
                gestorUI = FindFirstObjectByType<GameUIManager>();
            }

            if (portalEscape == null)
            {
                portalEscape = FindFirstObjectByType<EscapePortal>();
            }

            // Auto-detectar cantidad de núcleos en escena si no está configurada
            CollectibleCore[] nucleosEncontrados = FindObjectsByType<CollectibleCore>(FindObjectsSortMode.None);
            if (nucleosEncontrados.Length > 0)
            {
                totalNucleosRequeridos = nucleosEncontrados.Length;
            }

            ActualizarEstadoUI();
        }

        private void Update()
        {
            if (estadoActual == EstadoJuego.Jugando)
            {
                temporizadorJuego += Time.deltaTime;

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    AlternarPausa();
                }
            }
            else if (estadoActual == EstadoJuego.Pausado)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    AlternarPausa();
                }
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    SalirDelJuego();
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    ReiniciarJuego();
                }
            }
            else if (estadoActual == EstadoJuego.Derrota || estadoActual == EstadoJuego.Victoria)
            {
                // Atajos de teclado para reiniciar o cerrar
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    ReiniciarJuego();
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
                {
                    SalirDelJuego();
                }
            }
        }

        public void RegisterCoreCollected(CollectibleCore core) => RegistrarNucleoRecolectado(core);

        public void RegistrarNucleoRecolectado(CollectibleCore core)
        {
            if (estadoActual != EstadoJuego.Jugando) return;

            nucleosRecolectados++;
            Debug.Log($"[GameManager] Núcleo recolectado: {nucleosRecolectados}/{totalNucleosRequeridos}");

            if (gestorUI != null)
            {
                gestorUI.ActualizarUINucleos(nucleosRecolectados, totalNucleosRequeridos);
                gestorUI.MostrarNotificacion($"¡Núcleo de Fusión obtenido! ({nucleosRecolectados}/{totalNucleosRequeridos})", 2.0f);
            }

            if (nucleosRecolectados >= totalNucleosRequeridos)
            {
                DesbloquearPortalEscape();
            }
        }

        private void DesbloquearPortalEscape()
        {
            if (portalEscape != null)
            {
                portalEscape.ActivarPortal();
            }

            if (gestorUI != null)
            {
                gestorUI.ActualizarObjetivo("¡PORTAL ACTIVADO! Ve a la CÁMARA SUR (sigue la columna de luz verde).");
                gestorUI.MostrarNotificacion("¡Todos los núcleos sincronizados! El portal se ha abierto en la CÁMARA SUR.", 6.0f);
            }
        }

        public void TriggerVictory() => DispararVictoria();

        public void DispararVictoria()
        {
            if (estadoActual != EstadoJuego.Jugando) return;
            estadoActual = EstadoJuego.Victoria;

            Debug.Log("[GameManager] ¡VICTORIA! Misión completada.");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ReproducirVictoria();
            }

            if (gestorUI != null)
            {
                gestorUI.MostrarPantallaVictoria(temporizadorJuego, nucleosRecolectados, totalNucleosRequeridos);
            }
        }

        public void TriggerDefeat() => DispararDerrota();

        public void DispararDerrota()
        {
            if (estadoActual != EstadoJuego.Jugando) return;
            estadoActual = EstadoJuego.Derrota;

            Debug.Log("[GameManager] DERROTA. El explorador ha caído.");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ReproducirDerrota();
            }

            if (gestorUI != null)
            {
                gestorUI.MostrarPantallaDerrota();
            }
        }

        public void TogglePause() => AlternarPausa();

        public void AlternarPausa()
        {
            if (estadoActual == EstadoJuego.Victoria || estadoActual == EstadoJuego.Derrota) return;

            if (estadoActual == EstadoJuego.Jugando)
            {
                estadoActual = EstadoJuego.Pausado;
                Time.timeScale = 0f;
                if (gestorUI != null) gestorUI.ConfigurarPantallaPausa(true);
            }
            else if (estadoActual == EstadoJuego.Pausado)
            {
                estadoActual = EstadoJuego.Jugando;
                Time.timeScale = 1f;
                if (gestorUI != null) gestorUI.ConfigurarPantallaPausa(false);
            }
        }

        public void RestartGame() => ReiniciarJuego();

        public void ReiniciarJuego()
        {
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame() => SalirDelJuego();

        public void SalirDelJuego()
        {
            Debug.Log("[GameManager] Saliendo del juego...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
            try
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            catch {}
#endif
        }

        private void ActualizarEstadoUI()
        {
            if (gestorUI != null)
            {
                gestorUI.ActualizarUINucleos(nucleosRecolectados, totalNucleosRequeridos);
                gestorUI.ActualizarObjetivo("Explora el complejo y recolecta los 5 núcleos de energía.");
            }
        }
    }
}
