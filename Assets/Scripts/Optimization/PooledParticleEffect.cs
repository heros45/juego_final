using System.Collections;
using UnityEngine;

namespace ChronosEscape.Optimization
{
    /// <summary>
    /// Componente para efectos visuales o partículas que retornan automáticamente al ObjectPooler al finalizar.
    /// </summary>
    public class PooledParticleEffect : MonoBehaviour, IPooledObject
    {
        [SerializeField] private string etiquetaPool = "PickupEffect";
        [SerializeField] private float tiempoVida = 1.5f;
        [SerializeField] private ParticleSystem sistemaParticulas;

        private Coroutine corutinaRetorno;

        private void Awake()
        {
            if (sistemaParticulas == null) sistemaParticulas = GetComponent<ParticleSystem>();
        }

        public void Configurar(string etiqueta, float duracion)
        {
            etiquetaPool = etiqueta;
            tiempoVida = duracion;
        }

        public void Setup(string tag, float duration) => Configurar(tag, duration);

        public void OnObjectSpawn()
        {
            if (sistemaParticulas != null)
            {
                sistemaParticulas.Clear();
                sistemaParticulas.Play();
            }

            if (corutinaRetorno != null) StopCoroutine(corutinaRetorno);
            corutinaRetorno = StartCoroutine(RutinaRetornoConRetraso());
        }

        private IEnumerator RutinaRetornoConRetraso()
        {
            yield return new WaitForSeconds(tiempoVida);
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.DevolverAlPool(etiquetaPool, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
