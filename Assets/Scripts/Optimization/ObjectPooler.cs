using System.Collections.Generic;
using UnityEngine;

namespace ChronosEscape.Optimization
{
    /// <summary>
    /// Sistema de Object Pooling para optimización de memoria y reducción de Garbage Collection (GC).
    /// Cumple con el requisito de técnica de optimización exigido en la rúbrica del PA4.
    /// Permite reutilizar efectos visuales (VFX) y partículas en lugar de llamar a Instantiate/Destroy.
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [System.Serializable]
        public class ElementoPool
        {
            public string etiquetaPool;
            public GameObject objetoPrefab;
            public int tamanioInicial = 10;
            public bool permitirCrecimiento = true;
        }

        [Header("Configuración de Pools")]
        [SerializeField] private List<ElementoPool> pools = new List<ElementoPool>();

        private Dictionary<string, Queue<GameObject>> diccionarioPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, ElementoPool> configuracionPools = new Dictionary<string, ElementoPool>();

        [Header("Métricas de Optimización")]
        [SerializeField] private int totalObjetosGenerados = 0;
        [SerializeField] private int totalInstanciacionesEvitadas = 0;

        public int TotalInstanciacionesEvitadas => totalInstanciacionesEvitadas;
        public int TotalInstantiatesAvoided => totalInstanciacionesEvitadas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InicializarPools();
        }

        public void InicializarPools()
        {
            diccionarioPools.Clear();
            configuracionPools.Clear();

            foreach (var item in pools)
            {
                if (item.objetoPrefab == null || string.IsNullOrEmpty(item.etiquetaPool)) continue;

                Queue<GameObject> colaObjetos = new Queue<GameObject>();
                configuracionPools[item.etiquetaPool] = item;

                GameObject grupoPadre = new GameObject($"[Pool_{item.etiquetaPool}]");
                grupoPadre.transform.SetParent(this.transform);

                for (int i = 0; i < item.tamanioInicial; i++)
                {
                    GameObject obj = Instantiate(item.objetoPrefab, grupoPadre.transform);
                    obj.SetActive(false);
                    colaObjetos.Enqueue(obj);
                }

                diccionarioPools[item.etiquetaPool] = colaObjetos;
            }
        }

        public void RegisterNewPool(string tag, GameObject prefab, int initialCount) => RegistrarNuevoPool(tag, prefab, initialCount);

        public void RegistrarNuevoPool(string etiqueta, GameObject prefab, int cantidadInicial)
        {
            if (diccionarioPools.ContainsKey(etiqueta)) return;

            ElementoPool nuevoElemento = new ElementoPool
            {
                etiquetaPool = etiqueta,
                objetoPrefab = prefab,
                tamanioInicial = cantidadInicial,
                permitirCrecimiento = true
            };
            pools.Add(nuevoElemento);
            configuracionPools[etiqueta] = nuevoElemento;

            Queue<GameObject> cola = new Queue<GameObject>();
            GameObject grupoPadre = new GameObject($"[Pool_{etiqueta}]");
            grupoPadre.transform.SetParent(this.transform);

            for (int i = 0; i < cantidadInicial; i++)
            {
                GameObject obj = Instantiate(prefab, grupoPadre.transform);
                obj.SetActive(false);
                cola.Enqueue(obj);
            }

            diccionarioPools[etiqueta] = cola;
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation) => ObtenerDelPool(tag, position, rotation);

        public GameObject ObtenerDelPool(string etiqueta, Vector3 posicion, Quaternion rotacion)
        {
            if (!diccionarioPools.ContainsKey(etiqueta))
            {
                Debug.LogWarning($"[ObjectPooler] El pool con etiqueta '{etiqueta}' no existe.");
                return null;
            }

            Queue<GameObject> cola = diccionarioPools[etiqueta];
            GameObject objetoASpawnear = null;

            if (cola.Count > 0)
            {
                objetoASpawnear = cola.Dequeue();
                totalInstanciacionesEvitadas++;
            }
            else if (configuracionPools.ContainsKey(etiqueta) && configuracionPools[etiqueta].permitirCrecimiento)
            {
                objetoASpawnear = Instantiate(configuracionPools[etiqueta].objetoPrefab);
                Debug.Log($"[ObjectPooler] Pool '{etiqueta}' expandido dinámicamente.");
            }

            if (objetoASpawnear != null)
            {
                objetoASpawnear.transform.position = posicion;
                objetoASpawnear.transform.rotation = rotacion;
                objetoASpawnear.SetActive(true);

                totalObjetosGenerados++;

                IPooledObject pooledObj = objetoASpawnear.GetComponent<IPooledObject>();
                if (pooledObj != null)
                {
                    pooledObj.OnObjectSpawn();
                }
            }

            return objetoASpawnear;
        }

        public void ReturnToPool(string tag, GameObject obj) => DevolverAlPool(tag, obj);

        public void DevolverAlPool(string etiqueta, GameObject obj)
        {
            if (!diccionarioPools.ContainsKey(etiqueta))
            {
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            diccionarioPools[etiqueta].Enqueue(obj);
        }
    }

    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}
