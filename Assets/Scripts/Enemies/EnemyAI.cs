using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChronosEscape.Audio;
using ChronosEscape.Player;

namespace ChronosEscape.Enemies
{
    /// <summary>
    /// IA para centinelas con Máquina de Estados Finita (Patrulla, Persecución, Ataque, Búsqueda).
    /// Emplea detección por distancia, ángulo de visión (FOV), Raycast de línea de visión y waypoints.
    /// Incorpora optimización de reducción de cálculos (raycasts espaciados a intervalos fijos).
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        public enum EstadoEnemigo
        {
            Patrullando,
            Persiguiendo,
            Atacando,
            Buscando
        }

        // Compatibilidad con código previo
        public enum AIState
        {
            Patrolling,
            Chasing,
            Attacking,
            Searching
        }

        [Header("Estado Actual de la IA")]
        [SerializeField] private EstadoEnemigo estadoActual = EstadoEnemigo.Patrullando;

        [Header("Waypoints de Patrulla")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float velocidadPatrulla = 3.2f;
        [SerializeField] private float tiempoEsperaPunto = 1.2f;
        private int indicePuntoActual = 0;
        private bool esperandoEnPunto = false;

        [Header("Parámetros de Detección")]
        [SerializeField] private float radioDeteccion = 12f;
        [SerializeField] private float radioDeteccionCercana = 4f; // Detección por proximidad
        [SerializeField] private float anguloVision = 110f;
        [SerializeField] private LayerMask capaObstaculos;
        [SerializeField] private float tiempoPerderJugador = 3.5f;

        [Header("Parámetros de Persecución y Ataque")]
        [SerializeField] private float velocidadPersecucion = 5.2f;
        [SerializeField] private float distanciaAtaque = 1.8f;
        [SerializeField] private float danioAtaque = 20f;
        [SerializeField] private float tiempoRecargaAtaque = 1.2f;

        [Header("Feedback Visual y Luces")]
        [SerializeField] private Light luzOjo;
        [SerializeField] private Renderer renderModelo;
        [SerializeField] private Color colorPatrulla = new Color(0f, 0.8f, 1f);  // Cian
        [SerializeField] private Color colorAlerta = new Color(1f, 0.1f, 0.1f);   // Rojo alerta

        [Header("Optimización")]
        [SerializeField] private float intervaloDeteccion = 0.12f; // Reducción de raycasts
        private float temporizadorDeteccion = 0f;

        private Transform jugadorObjetivo;
        private float ultimoTiempoAtaque = -10f;
        private float tiempoJugadorPerdido = 0f;
        private Vector3 ultimaPosicionJugador;
        private CharacterController characterController;
        private bool estaAtacandoAnim = false;

        private static readonly List<EnemyAI> enemigosActivos = new List<EnemyAI>();
        private Vector3 ultimaPosicionAtasco;
        private float temporizadorAtasco = 0f;

        public EstadoEnemigo EstadoActual => estadoActual;

        private void OnEnable()
        {
            if (!enemigosActivos.Contains(this)) enemigosActivos.Add(this);
            ultimaPosicionAtasco = transform.position;
        }

        private void OnDisable()
        {
            enemigosActivos.Remove(this);
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
            }
            characterController.height = 1.2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 0, 0);
            characterController.stepOffset = 0.3f;
        }

        public void ConfigurarReferencias(Light luz, Renderer modelo)
        {
            luzOjo = luz;
            renderModelo = modelo;
        }

        public void AsignarWaypoints(Transform[] puntos)
        {
            waypoints = puntos;
        }

        public void SetWaypoints(Transform[] wps) => AsignarWaypoints(wps);

        private void Start()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            BuscarReferenciaJugador();
            CambiarEstado(EstadoEnemigo.Patrullando);
            ActualizarColorVisual(colorPatrulla);
        }

        private void BuscarReferenciaJugador()
        {
            if (PlayerController.Instance != null)
            {
                jugadorObjetivo = PlayerController.Instance.transform;
            }
            else
            {
                GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
                if (jugadorObj != null) jugadorObjetivo = jugadorObj.transform;
            }
        }

        private void Update()
        {
            if (jugadorObjetivo == null)
            {
                BuscarReferenciaJugador();
                return;
            }

            // Optimización: comprobación de visión con intervalo fijo en lugar de cada frame
            temporizadorDeteccion += Time.deltaTime;
            if (temporizadorDeteccion >= intervaloDeteccion)
            {
                temporizadorDeteccion = 0f;
                ComprobarVisionYTransiciones();
            }

            // Ejecución del comportamiento según el estado activo
            switch (estadoActual)
            {
                case EstadoEnemigo.Patrullando:
                    EjecutarPatrulla();
                    break;
                case EstadoEnemigo.Persiguiendo:
                    EjecutarPersecucion();
                    break;
                case EstadoEnemigo.Atacando:
                    EjecutarAtaque();
                    break;
                case EstadoEnemigo.Buscando:
                    EjecutarBusqueda();
                    break;
            }
        }

        private void ComprobarVisionYTransiciones()
        {
            if (PlayerController.Instance != null && PlayerController.Instance.IsDead)
            {
                if (estadoActual != EstadoEnemigo.Patrullando) CambiarEstado(EstadoEnemigo.Patrullando);
                return;
            }

            float distanciaAlJugador = Vector3.Distance(transform.position, jugadorObjetivo.position);
            bool puedeVerAlJugador = PuedeVerObjetivo(distanciaAlJugador);

            if (puedeVerAlJugador)
            {
                ultimaPosicionJugador = jugadorObjetivo.position;
                tiempoJugadorPerdido = 0f;

                if (distanciaAlJugador <= distanciaAtaque)
                {
                    if (estadoActual != EstadoEnemigo.Atacando) CambiarEstado(EstadoEnemigo.Atacando);
                }
                else
                {
                    if (estadoActual != EstadoEnemigo.Persiguiendo)
                    {
                        CambiarEstado(EstadoEnemigo.Persiguiendo);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayEnemyAlert();
                    }
                }
            }
            else
            {
                // Si estaba persiguiendo o atacando y pierde de vista al jugador
                if (estadoActual == EstadoEnemigo.Persiguiendo || estadoActual == EstadoEnemigo.Atacando)
                {
                    tiempoJugadorPerdido += intervaloDeteccion;
                    if (tiempoJugadorPerdido >= tiempoPerderJugador)
                    {
                        CambiarEstado(EstadoEnemigo.Buscando);
                    }
                }
            }
        }

        private bool PuedeVerObjetivo(float distanciaAlJugador)
        {
            if (distanciaAlJugador > radioDeteccion) return false;

            // Detección por proximidad cercana
            if (distanciaAlJugador <= radioDeteccionCercana)
            {
                return TieneLineaDeVision();
            }

            // Comprobación de campo de visión (FOV)
            Vector3 direccionHaciaJugador = (jugadorObjetivo.position - transform.position).normalized;
            float angulo = Vector3.Angle(transform.forward, direccionHaciaJugador);

            if (angulo <= anguloVision * 0.5f)
            {
                return TieneLineaDeVision();
            }

            return false;
        }

        private bool TieneLineaDeVision()
        {
            Vector3 origen = transform.position + Vector3.up * 0.6f;
            Vector3 destino = jugadorObjetivo.position + Vector3.up * 0.6f;
            Vector3 direccion = (destino - origen).normalized;
            float distancia = Vector3.Distance(origen, destino);

            RaycastHit hit;
            if (Physics.Raycast(origen, direccion, out hit, distancia, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == jugadorObjetivo || hit.transform.IsChildOf(jugadorObjetivo))
                {
                    return true;
                }
            }
            return false;
        }

        private void EjecutarPatrulla()
        {
            if (waypoints == null || waypoints.Length == 0 || esperandoEnPunto) return;

            Transform puntoDestino = waypoints[indicePuntoActual];
            if (puntoDestino == null) return;

            MoverHacia(puntoDestino.position, velocidadPatrulla);

            Vector3 posPlana = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 destinoPlano = new Vector3(puntoDestino.position.x, 0, puntoDestino.position.z);

            if (Vector3.Distance(posPlana, destinoPlano) < 0.6f)
            {
                StartCoroutine(RutinaEsperaEnPunto());
            }
        }

        private IEnumerator RutinaEsperaEnPunto()
        {
            esperandoEnPunto = true;
            yield return new WaitForSeconds(tiempoEsperaPunto);
            indicePuntoActual = (indicePuntoActual + 1) % waypoints.Length;
            esperandoEnPunto = false;
        }

        private void EjecutarChase() => EjecutarPersecucion();

        private void EjecutarPersecucion()
        {
            if (jugadorObjetivo == null) return;

            MoverHacia(jugadorObjetivo.position, velocidadPersecucion);
            MirarHacia(jugadorObjetivo.position);
        }

        private void EjecutarAtaque()
        {
            if (jugadorObjetivo == null) return;

            MirarHacia(jugadorObjetivo.position);

            float distancia = Vector3.Distance(transform.position, jugadorObjetivo.position);
            if (distancia > distanciaAtaque + 0.4f)
            {
                CambiarEstado(EstadoEnemigo.Persiguiendo);
                return;
            }

            if (Time.time - ultimoTiempoAtaque >= tiempoRecargaAtaque)
            {
                ultimoTiempoAtaque = Time.time;
                StartCoroutine(EfectoAtaqueVisual());

                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.TakeDamage(danioAtaque);
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPlayerHurt();
                }
            }
        }

        private IEnumerator EfectoAtaqueVisual()
        {
            estaAtacandoAnim = true;
            float intensidadOriginal = luzOjo != null ? luzOjo.intensity : 3.5f;
            Color colorOriginal = luzOjo != null ? luzOjo.color : colorAlerta;

            // Destello de impacto ultra intenso
            if (luzOjo != null)
            {
                luzOjo.color = Color.white;
                luzOjo.intensity = 12f;
                luzOjo.range = 20f;
            }

            if (renderModelo != null && renderModelo.material != null)
            {
                if (renderModelo.material.HasProperty("_EmissionColor"))
                {
                    renderModelo.material.SetColor("_EmissionColor", Color.white * 3.5f);
                }
            }

            yield return new WaitForSeconds(0.18f);

            // Resplandor residual rojo alerta
            if (luzOjo != null)
            {
                luzOjo.color = colorAlerta;
                luzOjo.intensity = 6.5f;
                luzOjo.range = 14f;
            }

            yield return new WaitForSeconds(0.2f);

            // Retorno al estado normal
            estaAtacandoAnim = false;
            if (luzOjo != null)
            {
                luzOjo.color = colorOriginal;
                luzOjo.intensity = intensidadOriginal;
                luzOjo.range = 14f;
            }
            ActualizarColorVisual(estadoActual == EstadoEnemigo.Patrullando ? colorPatrulla : colorAlerta);
        }

        private void EjecutarBusqueda()
        {
            MoverHacia(ultimaPosicionJugador, velocidadPatrulla);

            Vector3 posPlana = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 destinoPlano = new Vector3(ultimaPosicionJugador.x, 0, ultimaPosicionJugador.z);

            if (Vector3.Distance(posPlana, destinoPlano) < 0.8f)
            {
                CambiarEstado(EstadoEnemigo.Patrullando);
            }
        }

        private void MoverHacia(Vector3 destino, float velocidad)
        {
            Vector3 direccionDestino = (destino - transform.position);
            direccionDestino.y = 0f; // Mantener movimiento estrictamente sobre el plano horizontal

            // Sistema de detección y desatasco autónomo
            temporizadorAtasco += Time.deltaTime;
            if (Vector3.Distance(transform.position, ultimaPosicionAtasco) > 0.25f)
            {
                temporizadorAtasco = 0f;
                ultimaPosicionAtasco = transform.position;
            }
            else if (temporizadorAtasco >= 1.2f)
            {
                // El NPC ha permanecido atrapado por más de 1.2s: saltar inmediatamente al siguiente objetivo
                temporizadorAtasco = 0f;
                ultimaPosicionAtasco = transform.position;
                if (estadoActual == EstadoEnemigo.Patrullando && waypoints != null && waypoints.Length > 0)
                {
                    indicePuntoActual = (indicePuntoActual + 1) % waypoints.Length;
                }
                else if (estadoActual == EstadoEnemigo.Buscando)
                {
                    CambiarEstado(EstadoEnemigo.Patrullando);
                }
            }

            if (direccionDestino.magnitude > 0.05f)
            {
                Vector3 direccionDeseada = direccionDestino.normalized;
                Vector3 direccionFinal = direccionDeseada;

                // Detección frontal de obstáculos en el plano horizontal
                Vector3 origenRayo = transform.position + Vector3.up * 0.4f;
                RaycastHit hitObstaculo;

                if (Physics.SphereCast(origenRayo, 0.35f, direccionDeseada, out hitObstaculo, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    // No esquivar al propio jugador si lo está persiguiendo
                    bool esJugador = hitObstaculo.transform == jugadorObjetivo || (jugadorObjetivo != null && hitObstaculo.transform.IsChildOf(jugadorObjetivo));
                    if (!esJugador)
                    {
                        Vector3 normalHorizontal = hitObstaculo.normal;
                        normalHorizontal.y = 0f;
                        if (normalHorizontal.sqrMagnitude > 0.01f)
                        {
                            normalHorizontal.Normalize();
                            Vector3 direccionDeslizamiento = Vector3.ProjectOnPlane(direccionDeseada, normalHorizontal);
                            direccionDeslizamiento.y = 0f;
                            if (direccionDeslizamiento.sqrMagnitude > 0.01f)
                            {
                                direccionFinal = direccionDeslizamiento.normalized;
                            }
                            else
                            {
                                Vector3 derecha = Vector3.Cross(Vector3.up, normalHorizontal);
                                bool hayMuroDer = Physics.Raycast(origenRayo, derecha, 1.2f, ~0, QueryTriggerInteraction.Ignore);
                                direccionFinal = (hayMuroDer ? -derecha : derecha).normalized;
                            }
                        }
                    }
                }

                // Repulsión mutua entre centinelas para evitar que colisionen o se encimen entre sí
                for (int i = 0; i < enemigosActivos.Count; i++)
                {
                    EnemyAI otro = enemigosActivos[i];
                    if (otro != null && otro != this)
                    {
                        Vector3 diff = transform.position - otro.transform.position;
                        diff.y = 0f;
                        float distSqr = diff.sqrMagnitude;
                        if (distSqr < 4.0f && distSqr > 0.0001f)
                        {
                            float dist = Mathf.Sqrt(distSqr);
                            Vector3 rep = (diff / dist) * ((2.0f - dist) * 1.8f);
                            direccionFinal += rep;
                        }
                    }
                }

                // Forzar estrictamente el vector de movimiento al plano horizontal (Y = 0)
                direccionFinal.y = 0f;

                if (direccionFinal.sqrMagnitude > 0.001f)
                {
                    direccionFinal.Normalize();
                    Quaternion rotacionDestino = Quaternion.LookRotation(direccionFinal, Vector3.up);
                    Quaternion rotSlerp = Quaternion.Slerp(transform.rotation, rotacionDestino, Time.deltaTime * 7f);
                    // Bloqueo riguroso: solo rotación en Yaw (Y), pitch y roll permanecen en 0
                    transform.rotation = Quaternion.Euler(0f, rotSlerp.eulerAngles.y, 0f);

                    // Movimiento físico mediante CharacterController respetando colisiones
                    if (characterController != null && characterController.enabled)
                    {
                        Vector3 movimiento = direccionFinal * velocidad;
                        movimiento.y = -5f; // Fuerza suave hacia abajo para mantener estabilidad
                        characterController.Move(movimiento * Time.deltaTime);
                    }
                    else
                    {
                        transform.position += direccionFinal * velocidad * Time.deltaTime;
                    }
                }
            }
        }

        private void MirarHacia(Vector3 posicionObjetivo)
        {
            Vector3 direccionMirada = (posicionObjetivo - transform.position);
            direccionMirada.y = 0f;
            if (direccionMirada.sqrMagnitude > 0.01f)
            {
                Quaternion rotacionMirada = Quaternion.LookRotation(direccionMirada.normalized, Vector3.up);
                Quaternion rotSlerp = Quaternion.Slerp(transform.rotation, rotacionMirada, Time.deltaTime * 8f);
                transform.rotation = Quaternion.Euler(0f, rotSlerp.eulerAngles.y, 0f);
            }
        }

        private void LateUpdate()
        {
            // Seguro final: asegura que el dron permanezca 100% horizontal sin inclinaciones en X ni Z
            Vector3 rot = transform.rotation.eulerAngles;
            if (Mathf.Abs(rot.x) > 0.01f || Mathf.Abs(rot.z) > 0.01f)
            {
                transform.rotation = Quaternion.Euler(0f, rot.y, 0f);
            }
        }

        private void CambiarEstado(EstadoEnemigo nuevoEstado)
        {
            estadoActual = nuevoEstado;
            switch (nuevoEstado)
            {
                case EstadoEnemigo.Patrullando:
                case EstadoEnemigo.Buscando:
                    ActualizarColorVisual(colorPatrulla);
                    break;
                case EstadoEnemigo.Persiguiendo:
                case EstadoEnemigo.Atacando:
                    ActualizarColorVisual(colorAlerta);
                    break;
            }
        }

        private void ActualizarColorVisual(Color color)
        {
            if (luzOjo != null) luzOjo.color = color;
            if (renderModelo != null && renderModelo.material != null)
            {
                renderModelo.material.color = color;
                if (renderModelo.material.HasProperty("_EmissionColor"))
                {
                    renderModelo.material.SetColor("_EmissionColor", color * 1.5f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Gizmos de depuración visual de IA para Unity Editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, distanciaAtaque);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radioDeteccionCercana);

            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                        int siguiente = (i + 1) % waypoints.Length;
                        if (waypoints[siguiente] != null)
                        {
                            Gizmos.DrawLine(waypoints[i].position, waypoints[siguiente].position);
                        }
                    }
                }
            }
        }
    }
}
