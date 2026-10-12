using UnityEngine;
using ChronosEscape.Core;

namespace ChronosEscape.Player
{
    /// <summary>
    /// Control de cámara en primera persona con rotación vertical limitada y bloqueo del cursor.
    /// </summary>
    public class MouseLook : MonoBehaviour
    {
        [Header("Sensibilidad y Suavizado")]
        [SerializeField] private float sensibilidadRaton = 180f;
        [SerializeField] private float tiempoSuavizado = 0.03f;
        [SerializeField] private Transform cuerpoJugador;

        private float rotacionX = 0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cuerpoJugador == null && transform.parent != null)
            {
                cuerpoJugador = transform.parent;
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f || (GameManager.Instance != null && GameManager.Instance.IsGameEnded))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Movimiento del ratón (ejes estándar de tutoriales de Unity)
            float mouseX = Input.GetAxisRaw("Mouse X") * sensibilidadRaton * Time.unscaledDeltaTime;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensibilidadRaton * Time.unscaledDeltaTime;

            rotacionX -= mouseY;
            rotacionX = Mathf.Clamp(rotacionX, -85f, 85f);

            transform.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

            if (cuerpoJugador != null)
            {
                cuerpoJugador.Rotate(Vector3.up * mouseX);
            }
        }

        public void SetCursorLock(bool bloqueado)
        {
            Cursor.lockState = bloqueado ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloqueado;
        }
    }
}
