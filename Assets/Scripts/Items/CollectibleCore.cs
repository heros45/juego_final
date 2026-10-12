using UnityEngine;
using ChronosEscape.Audio;
using ChronosEscape.Core;
using ChronosEscape.Optimization;

namespace ChronosEscape.Items
{
    /// <summary>
    /// Mecánica principal: Núcleos de energía coleccionables con animación de rotación,
    /// flotación senoidal y generación de efectos mediante Object Pooling.
    /// </summary>
    public class CollectibleCore : MonoBehaviour
    {
        [Header("Animación")]
        [SerializeField] private float velocidadRotacion = 90f;
        [SerializeField] private float amplitudFlotacion = 0.25f;
        [SerializeField] private float frecuenciaFlotacion = 2f;

        [Header("Efectos")]
        [SerializeField] private string etiquetaPoolEfecto = "PickupEffect";
        [SerializeField] private Light luzNucleo;

        private Vector3 posicionInicial;
        private bool yaRecolectado = false;

        private void Start()
        {
            posicionInicial = transform.position;
        }

        private void Update()
        {
            if (yaRecolectado) return;

            // Rotación continua sobre dos ejes
            transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.World);
            transform.Rotate(Vector3.right * (velocidadRotacion * 0.4f) * Time.deltaTime, Space.Self);

            // Efecto de flotación suave
            float nuevaY = posicionInicial.y + Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
            transform.position = new Vector3(posicionInicial.x, nuevaY, posicionInicial.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (yaRecolectado) return;

            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController>() != null)
            {
                Recoger();
            }
        }

        public void Collect() => Recoger();

        public void Recoger()
        {
            if (yaRecolectado) return;
            yaRecolectado = true;

            // Desplegar efecto visual desde el Object Pooler para ahorrar memoria
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.SpawnFromPool(etiquetaPoolEfecto, transform.position, Quaternion.identity);
            }

            // Sonido de recolección
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPickup();
            }

            // Registrar en el administrador del juego
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterCoreCollected(this);
            }

            gameObject.SetActive(false);
        }
    }
}
