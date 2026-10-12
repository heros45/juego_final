using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChronosEscape.Core;
using ChronosEscape.Player;
using ChronosEscape.Optimization;

namespace ChronosEscape.UI
{
    /// <summary>
    /// Administrador de la Interfaz de Usuario (HUD, Menús, Pantalla de Victoria/Derrota, Pausa).
    /// Garantiza interactividad completa con mouse, botones y atajos de teclado (R para reintentar, ESC/Q para salir).
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        [Header("HUD del Jugador")]
        [SerializeField] private Slider barraVida;
        [SerializeField] private Image imagenRellenoVida;
        [SerializeField] private Text textoVida;
        [SerializeField] private Text textoContadorNucleos;
        [SerializeField] private Text textoObjetivo;
        [SerializeField] private Image pantallaFlashDanio;
        [SerializeField] private Text textoNotificacion;

        [Header("Paneles de Estado")]
        [SerializeField] private GameObject panelVictoria;
        [SerializeField] private Text textoEstadisticasVictoria;
        [SerializeField] private GameObject panelDerrota;
        [SerializeField] private GameObject panelPausa;

        [Header("Colores de Salud")]
        [SerializeField] private Color colorVidaLlena = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color colorVidaMedia = new Color(0.95f, 0.8f, 0.1f);
        [SerializeField] private Color colorVidaBaja = new Color(0.95f, 0.2f, 0.2f);

        private Coroutine corutinaFlash;
        private Coroutine corutinaNotificacion;

        public void ConfigurarReferenciasUI(
            Slider slider, Image relleno, Text txtVida, Text txtNucleos,
            Text txtObjetivo, Image flash, Text txtNotif,
            GameObject pnlVic, Text txtStatsVic, GameObject pnlDer, GameObject pnlPau)
        {
            barraVida = slider;
            imagenRellenoVida = relleno;
            textoVida = txtVida;
            textoContadorNucleos = txtNucleos;
            textoObjetivo = txtObjetivo;
            pantallaFlashDanio = flash;
            textoNotificacion = txtNotif;
            panelVictoria = pnlVic;
            textoEstadisticasVictoria = txtStatsVic;
            panelDerrota = pnlDer;
            panelPausa = pnlPau;
        }

        private void Awake()
        {
            ConfigurarBotonesListeners();
        }

        private void Start()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnHealthChanged += ActualizarUIVida;
                PlayerController.Instance.OnPlayerDamaged += DispararFlashDanio;
                ActualizarUIVida(PlayerController.Instance.VidaActual, PlayerController.Instance.VidaMaxima);
            }

            if (panelVictoria != null) panelVictoria.SetActive(false);
            if (panelDerrota != null) panelDerrota.SetActive(false);
            if (panelPausa != null) panelPausa.SetActive(false);
            if (pantallaFlashDanio != null)
            {
                pantallaFlashDanio.color = new Color(1, 0, 0, 0);
                pantallaFlashDanio.raycastTarget = false;
            }
            if (textoNotificacion != null)
            {
                textoNotificacion.text = "";
                textoNotificacion.raycastTarget = false;
            }
        }

        private void Update()
        {
            bool derrotaAbierta = panelDerrota != null && panelDerrota.activeSelf;
            bool victoriaAbierta = panelVictoria != null && panelVictoria.activeSelf;
            bool pausaAbierta = panelPausa != null && panelPausa.activeSelf;

            if (derrotaAbierta || victoriaAbierta || pausaAbierta)
            {
                // Asegurar cursor libre para hacer clic en botones
                if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                // Atajos de teclado universales
                if (derrotaAbierta || victoriaAbierta)
                {
                    // R, Espacio o Enter para reintentar
                    if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        AlHacerClicReiniciar();
                    }

                    // Escape o Q para salir
                    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
                    {
                        AlHacerClicSalir();
                    }
                }
                else if (pausaAbierta)
                {
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        AlHacerClicSalir();
                    }
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        AlHacerClicReiniciar();
                    }
                }
            }
        }

        private void ConfigurarBotonesListeners()
        {
            // Desactivar raycastTarget en todos los textos de la interfaz para que no bloqueen los clics
            Text[] todosLosTextos = GetComponentsInChildren<Text>(true);
            foreach (var txt in todosLosTextos)
            {
                txt.raycastTarget = false;
            }

            // Botones de panel de Derrota
            if (panelDerrota != null)
            {
                Button[] botonesDerrota = panelDerrota.GetComponentsInChildren<Button>(true);
                foreach (var btn in botonesDerrota)
                {
                    btn.onClick.RemoveAllListeners();
                    string nombre = btn.gameObject.name.ToLower();
                    if (nombre.Contains("retry") || nombre.Contains("restart") || nombre.Contains("reintentar"))
                    {
                        btn.onClick.AddListener(AlHacerClicReiniciar);
                    }
                    else if (nombre.Contains("quit") || nombre.Contains("salir") || nombre.Contains("exit"))
                    {
                        btn.onClick.AddListener(AlHacerClicSalir);
                    }
                }
            }

            // Botones de panel de Victoria
            if (panelVictoria != null)
            {
                Button[] botonesVictoria = panelVictoria.GetComponentsInChildren<Button>(true);
                foreach (var btn in botonesVictoria)
                {
                    btn.onClick.RemoveAllListeners();
                    string nombre = btn.gameObject.name.ToLower();
                    if (nombre.Contains("restart") || nombre.Contains("jugar") || nombre.Contains("retry"))
                    {
                        btn.onClick.AddListener(AlHacerClicReiniciar);
                    }
                    else if (nombre.Contains("quit") || nombre.Contains("salir") || nombre.Contains("exit"))
                    {
                        btn.onClick.AddListener(AlHacerClicSalir);
                    }
                }
            }

            // Botones de panel de Pausa
            if (panelPausa != null)
            {
                Button[] botonesPausa = panelPausa.GetComponentsInChildren<Button>(true);
                foreach (var btn in botonesPausa)
                {
                    btn.onClick.RemoveAllListeners();
                    string nombre = btn.gameObject.name.ToLower();
                    if (nombre.Contains("resume") || nombre.Contains("continuar"))
                    {
                        btn.onClick.AddListener(AlHacerClicContinuar);
                    }
                    else if (nombre.Contains("restart") || nombre.Contains("reiniciar"))
                    {
                        btn.onClick.AddListener(AlHacerClicReiniciar);
                    }
                    else if (nombre.Contains("quit") || nombre.Contains("salir"))
                    {
                        btn.onClick.AddListener(AlHacerClicSalir);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnHealthChanged -= ActualizarUIVida;
                PlayerController.Instance.OnPlayerDamaged -= DispararFlashDanio;
            }
        }

        public void ActualizarUIVida(float vidaActual, float vidaMaxima)
        {
            if (barraVida != null)
            {
                barraVida.maxValue = vidaMaxima;
                barraVida.value = vidaActual;
            }

            if (textoVida != null)
            {
                textoVida.text = $"{Mathf.CeilToInt(vidaActual)} / {Mathf.CeilToInt(vidaMaxima)} HP";
            }

            if (imagenRellenoVida != null)
            {
                float proporcion = vidaActual / vidaMaxima;
                if (proporcion > 0.5f)
                    imagenRellenoVida.color = Color.Lerp(colorVidaMedia, colorVidaLlena, (proporcion - 0.5f) * 2f);
                else
                    imagenRellenoVida.color = Color.Lerp(colorVidaBaja, colorVidaMedia, proporcion * 2f);
            }
        }

        public void UpdateHealthUI(float cur, float max) => ActualizarUIVida(cur, max);

        public void ActualizarUINucleos(int recolectados, int total)
        {
            if (textoContadorNucleos != null)
            {
                textoContadorNucleos.text = $"NÚCLEOS DE ENERGÍA: {recolectados} / {total}";
            }
        }

        public void UpdateCoresUI(int col, int tot) => ActualizarUINucleos(col, tot);

        public void ActualizarObjetivo(string texto)
        {
            if (textoObjetivo != null)
            {
                textoObjetivo.text = $"OBJETIVO: {texto}";
            }
        }

        public void UpdateObjective(string txt) => ActualizarObjetivo(txt);

        public void MostrarNotificacion(string mensaje, float duracion = 2.5f)
        {
            if (textoNotificacion == null) return;
            if (corutinaNotificacion != null) StopCoroutine(corutinaNotificacion);
            corutinaNotificacion = StartCoroutine(RutinaNotificacion(mensaje, duracion));
        }

        public void ShowNotification(string msg, float dur = 2.5f) => MostrarNotificacion(msg, dur);

        private IEnumerator RutinaNotificacion(string mensaje, float duracion)
        {
            textoNotificacion.text = mensaje;
            textoNotificacion.color = Color.white;
            yield return new WaitForSeconds(duracion);

            float desvanecimiento = 1f;
            while (desvanecimiento > 0f)
            {
                desvanecimiento -= Time.deltaTime * 2f;
                textoNotificacion.color = new Color(1f, 1f, 1f, desvanecimiento);
                yield return null;
            }
            textoNotificacion.text = "";
        }

        public void DispararFlashDanio()
        {
            if (pantallaFlashDanio == null) return;
            if (corutinaFlash != null) StopCoroutine(corutinaFlash);
            corutinaFlash = StartCoroutine(RutinaFlashDanio());
        }

        public void TriggerDamageFlash() => DispararFlashDanio();

        private IEnumerator RutinaFlashDanio()
        {
            pantallaFlashDanio.color = new Color(0.8f, 0.05f, 0.05f, 0.45f);
            float alfa = 0.45f;
            while (alfa > 0f)
            {
                alfa -= Time.deltaTime * 1.5f;
                pantallaFlashDanio.color = new Color(0.8f, 0.05f, 0.05f, Mathf.Clamp01(alfa));
                yield return null;
            }
        }

        public void MostrarPantallaVictoria(float tiempoTranscurrido, int nucleosRecolectados, int totalNucleos)
        {
            if (panelVictoria != null)
            {
                panelVictoria.SetActive(true);
                int minutos = Mathf.FloorToInt(tiempoTranscurrido / 60F);
                int segundos = Mathf.FloorToInt(tiempoTranscurrido % 60F);

                int llamadasEvitadas = ObjectPooler.Instance != null ? ObjectPooler.Instance.TotalInstanciacionesEvitadas : 0;

                if (textoEstadisticasVictoria != null)
                {
                    textoEstadisticasVictoria.text = $"¡Has escapado con éxito!\n\n" +
                                                     $"⏱ Tiempo total: {minutos:00}:{segundos:00}\n" +
                                                     $"⚡ Núcleos recolectados: {nucleosRecolectados}/{totalNucleos}\n" +
                                                     $"🚀 Optimización: {llamadasEvitadas} llamadas a Instantiate() evitadas con Object Pooling.\n\n" +
                                                     $"[R / Espacio] Jugar de nuevo  |  [ESC / Q] Salir";
                }
            }

            DesbloquearCursor();
        }

        public void ShowVictoryScreen(float t, int c, int tot) => MostrarPantallaVictoria(t, c, tot);

        public void MostrarPantallaDerrota()
        {
            if (panelDerrota != null)
            {
                panelDerrota.SetActive(true);
            }
            DesbloquearCursor();
        }

        public void ShowDefeatScreen() => MostrarPantallaDerrota();

        public void ConfigurarPantallaPausa(bool enPausa)
        {
            if (panelPausa != null)
            {
                panelPausa.SetActive(enPausa);
            }

            if (enPausa) DesbloquearCursor();
            else BloquearCursor();
        }

        public void SetPauseScreen(bool p) => ConfigurarPantallaPausa(p);

        public void DesbloquearCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void BloquearCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor() => DesbloquearCursor();
        public void LockCursor() => BloquearCursor();

        // Acciones de botones
        public void AlHacerClicReiniciar()
        {
            Debug.Log("[GameUIManager] Reiniciando nivel...");
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }

        public void OnClickRestart() => AlHacerClicReiniciar();

        public void AlHacerClicContinuar()
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        public void OnClickResume() => AlHacerClicContinuar();

        public void AlHacerClicSalir()
        {
            Debug.Log("[GameUIManager] Saliendo del juego...");
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }

        public void OnClickQuit() => AlHacerClicSalir();
    }
}
