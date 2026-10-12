using UnityEngine;
using ChronosEscape.Audio;
using ChronosEscape.Core;

namespace ChronosEscape.Player
{
    /// <summary>
    /// Controlador principal del jugador: físicas, desplazamiento 3D, salto, salud y efectos de sonido.
    /// Diseñado con CharacterController para máxima estabilidad y respuesta inmediata.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Parámetros de Movimiento")]
        [SerializeField] private float velocidadCaminar = 6.0f;
        [SerializeField] private float velocidadCorrer = 9.5f;
        [SerializeField] private float alturaSalto = 1.8f;
        [SerializeField] private float gravedad = -20f;

        [Header("Sistema de Salud")]
        [SerializeField] private float vidaMaxima = 100f;
        [SerializeField] private float vidaActual = 100f;
        [SerializeField] private float tiempoInvulnerabilidad = 0.8f;

        [Header("Audio de Pasos")]
        [SerializeField] private float intervaloPasosCaminar = 0.42f;
        [SerializeField] private float intervaloPasosCorrer = 0.28f;

        // Variables de físicas y estado
        private CharacterController controller;
        private Vector3 velocidad;
        private bool enSuelo;
        private float ultimoTiempoDanio = -10f;
        private float temporizadorPasos = 0f;
        private bool estaMuerto = false;

        // Propiedades públicas en español (con compatibilidad)
        public float VidaActual => vidaActual;
        public float VidaMaxima => vidaMaxima;
        public bool EstaMuerto => estaMuerto;

        public float CurrentHealth => vidaActual;
        public float MaxHealth => vidaMaxima;
        public bool IsDead => estaMuerto;

        // Eventos para actualizar la interfaz
        public delegate void DelegadoVida(float actual, float maximo);
        public event DelegadoVida OnHealthChanged;

        public delegate void DelegadoDanio();
        public event DelegadoDanio OnPlayerDamaged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            controller = GetComponent<CharacterController>();
            vidaActual = vidaMaxima;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(vidaActual, vidaMaxima);
        }

        private void Update()
        {
            if (estaMuerto || (GameManager.Instance != null && GameManager.Instance.IsGameEnded)) return;

            ManejarMovimiento();
            ManejarCaida();
            ManejarInteraccion();
        }

        private void ManejarInteraccion()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Detectar terminales cercanas en un radio de 4 metros
                Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, 4.0f);
                foreach (var objeto in objetosCercanos)
                {
                    var terminal = objeto.GetComponent<ChronosEscape.Environment.SecurityTerminal>();
                    if (terminal == null) terminal = objeto.GetComponentInParent<ChronosEscape.Environment.SecurityTerminal>();
                    if (terminal != null)
                    {
                        terminal.Interact();
                        break;
                    }
                }
            }
        }

        private void ManejarMovimiento()
        {
            enSuelo = controller.isGrounded;
            if (enSuelo && velocidad.y < 0)
            {
                velocidad.y = -2f;
            }

            // Entrada de ejes horizontal y vertical (estándar en Unity)
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direccionMovimiento = (transform.right * horizontal + transform.forward * vertical).normalized;

            bool estaCorriendo = Input.GetKey(KeyCode.LeftShift) && enSuelo && direccionMovimiento.magnitude > 0.1f;
            float velocidadActual = estaCorriendo ? velocidadCorrer : velocidadCaminar;

            controller.Move(direccionMovimiento * velocidadActual * Time.deltaTime);

            // Temporizador de sonido de pasos
            if (enSuelo && direccionMovimiento.magnitude > 0.1f)
            {
                temporizadorPasos += Time.deltaTime;
                float intervalo = estaCorriendo ? intervaloPasosCorrer : intervaloPasosCaminar;
                if (temporizadorPasos >= intervalo)
                {
                    temporizadorPasos = 0f;
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayFootstep();
                    }
                }
            }
            else
            {
                temporizadorPasos = 0f;
            }

            // Salto con barra espaciadora
            if (Input.GetButtonDown("Jump") && enSuelo)
            {
                velocidad.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            }

            // Aplicar gravedad continua
            velocidad.y += gravedad * Time.deltaTime;
            controller.Move(velocidad * Time.deltaTime);
        }

        private void ManejarCaida()
        {
            if (transform.position.y < -15f && !estaMuerto)
            {
                TakeDamage(vidaMaxima); // Caída mortal al vacío
            }
        }

        public void TakeDamage(float cantidad) => TomarDanio(cantidad);

        public void TomarDanio(float cantidad)
        {
            if (estaMuerto) return;
            if (Time.time - ultimoTiempoDanio < tiempoInvulnerabilidad) return;

            ultimoTiempoDanio = Time.time;
            vidaActual -= cantidad;
            vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

            OnHealthChanged?.Invoke(vidaActual, vidaMaxima);
            OnPlayerDamaged?.Invoke();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerHurt();
            }

            if (vidaActual <= 0f)
            {
                Morir();
            }
        }

        public void Heal(float cantidad) => Curar(cantidad);

        public void Curar(float cantidad)
        {
            if (estaMuerto) return;
            vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
            OnHealthChanged?.Invoke(vidaActual, vidaMaxima);
        }

        private void Morir()
        {
            estaMuerto = true;
            Debug.Log("[PlayerController] Jugador derrotado.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerDefeat();
            }
        }
    }
}
