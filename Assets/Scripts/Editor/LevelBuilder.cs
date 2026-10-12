using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using ChronosEscape.Core;
using ChronosEscape.Player;
using ChronosEscape.Enemies;
using ChronosEscape.Items;
using ChronosEscape.Environment;
using ChronosEscape.UI;
using ChronosEscape.Audio;
using ChronosEscape.Optimization;

namespace ChronosEscape.EditorTools
{
    public static class LevelBuilder
    {
        [MenuItem("PA4 Game/Generar Escenario Completo", false, 1)]
        public static void GenerateCompleteLevel()
        {
            Debug.Log("[LevelBuilder] Iniciando generación procedural del nivel 3D...");

            // 1. Crear nueva escena
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Asegurar carpetas
            EnsureFolder("Assets/Materials");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");

            // Convertir materiales de Minimal Scifi Pack a Standard Built-in Shader (eliminar shaders HDRP rotos)
            EnsureMinimalScifiMaterialsConverted();

            // 2. Crear Materiales PBR
            Material matFloor = CreateMaterial("Assets/Materials/Mat_Floor.mat", new Color(0.12f, 0.14f, 0.18f), 0.2f, 0.4f);
            Material matWall = CreateMaterial("Assets/Materials/Mat_Wall.mat", new Color(0.18f, 0.22f, 0.28f), 0.3f, 0.3f);
            Material matPillar = CreateMaterial("Assets/Materials/Mat_Pillar.mat", new Color(0.08f, 0.10f, 0.14f), 0.5f, 0.5f);
            Material matAccent = CreateMaterial("Assets/Materials/Mat_Accent.mat", new Color(0.0f, 0.8f, 0.9f), 0.8f, 0.8f, new Color(0.0f, 0.7f, 0.85f) * 1.5f);
            Material matCore = CreateMaterial("Assets/Materials/Mat_Core.mat", new Color(1.0f, 0.8f, 0.1f), 0.8f, 0.9f, new Color(1.0f, 0.75f, 0.05f) * 2.5f);
            Material matEnemy = CreateMaterial("Assets/Materials/Mat_Enemy.mat", new Color(0.85f, 0.15f, 0.15f), 0.5f, 0.6f, new Color(1.0f, 0.1f, 0.1f) * 1.8f);
            Material matPortalLocked = CreateMaterial("Assets/Materials/Mat_PortalLocked.mat", new Color(0.8f, 0.2f, 0.2f), 0.4f, 0.7f, new Color(0.8f, 0.1f, 0.1f) * 0.8f);
            Material matRamp = CreateMaterial("Assets/Materials/Mat_Ramp.mat", new Color(0.15f, 0.17f, 0.22f), 0.3f, 0.3f);
            Material matCeiling = CreateMaterial("Assets/Materials/Mat_Ceiling.mat", new Color(0.14f, 0.16f, 0.20f), 0.6f, 0.35f);

            // 3. Crear Sistema de Partículas para Object Pooling
            GameObject pooledVfxPrefab = CreatePickupVfxPrefab(matCore);

            // 4. Configurar Iluminación Ambiental y Cielo
            SetupLighting();

            // 5. Contenedor Raíz de la Estación
            GameObject environmentRoot = new GameObject("=== ENVIRONMENT ===");

            // 1. Suelos y Salas Interconectadas (Todas al mismo nivel base transitables)
            // Sala Central (30 x 30)
            CreateFloor(environmentRoot.transform, new Vector3(0, 0, 0), new Vector3(30, 1, 30), matFloor, "CentralHub_Floor");
            
            // Pasillo Norte y Sala Norte (Reactores)
            CreateFloor(environmentRoot.transform, new Vector3(0, 0, 25), new Vector3(10, 1, 20), matFloor, "NorthHall_Floor");
            CreateFloor(environmentRoot.transform, new Vector3(0, 0, 44), new Vector3(28, 1, 18), matFloor, "NorthChamber_Floor");
            
            // Pasillo Sur y Sala Sur (Portal de Escape)
            CreateFloor(environmentRoot.transform, new Vector3(0, 0, -25), new Vector3(10, 1, 20), matFloor, "SouthHall_Floor");
            CreateFloor(environmentRoot.transform, new Vector3(0, 0, -44), new Vector3(28, 1, 18), matFloor, "PortalChamber_Floor");

            // Pasillo Oeste y Sala Oeste (Generadores) - 100% ABIERTA Y TRANSITABLE
            CreateFloor(environmentRoot.transform, new Vector3(-25, 0, 0), new Vector3(20, 1, 10), matFloor, "WestHall_Floor");
            CreateFloor(environmentRoot.transform, new Vector3(-45, 0, 0), new Vector3(20, 1, 28), matFloor, "WestChamber_Floor");

            // Pasillo Este y Sala Este (Observatorio y Mirador)
            CreateFloor(environmentRoot.transform, new Vector3(25, 0, 0), new Vector3(20, 1, 10), matFloor, "EastHall_Floor");
            CreateFloor(environmentRoot.transform, new Vector3(45, 0, 0), new Vector3(20, 1, 28), matFloor, "EastChamber_Floor");
            
            // Plataforma elevada del Mirador Este sin superposición (Z-Fighting 100% eliminado)
            CreateFloor(environmentRoot.transform, new Vector3(45f, 0.75f, 0), new Vector3(12f, 0.9f, 16f), matFloor, "EastHighPlatform_Base");
            CreateFloor(environmentRoot.transform, new Vector3(45f, 1.25f, 0), new Vector3(12f, 0.1f, 16f), matAccent, "EastHighPlatform_Deck");
            CreateFloor(environmentRoot.transform, new Vector3(36f, 0.45f, 0), new Vector3(2f, 0.5f, 8f), matRamp, "East_Step_1");
            CreateFloor(environmentRoot.transform, new Vector3(38f, 0.75f, 0), new Vector3(2f, 0.7f, 8f), matRamp, "East_Step_2");

            // Muros perimetrales con 4 accesos abiertos de 10 metros
            BuildWallsAndCovers(environmentRoot.transform, matWall, matPillar, matAccent);

            // Techo completo de la instalación a 6.25m de altura (ambiente cerrado e inmersivo)
            BuildCeilings(environmentRoot.transform, matCeiling);

            // Focos Sirenas de Emergencia giratorias en el techo (Haz rojo barredor de alerta)
            CreateEmergencySiren(environmentRoot.transform, new Vector3(-7, 5.9f, -7), matAccent);
            CreateEmergencySiren(environmentRoot.transform, new Vector3(7, 5.9f, 7), matAccent);
            CreateEmergencySiren(environmentRoot.transform, new Vector3(0, 5.9f, 44), matAccent);  // Norte
            CreateEmergencySiren(environmentRoot.transform, new Vector3(0, 5.9f, -44), matAccent); // Sur (sobre la compuerta de escape)
            CreateEmergencySiren(environmentRoot.transform, new Vector3(-45, 5.9f, 0), matAccent); // Oeste
            CreateEmergencySiren(environmentRoot.transform, new Vector3(45, 5.9f, 0), matAccent);  // Este

            // 6. Configurar Sistema de Partículas Ambiental (Polvo cósmico/atmósfera)
            CreateAtmosphericDust(environmentRoot.transform);

            // Sistema de Luces Dinámicas de Evacuación (Alerta roja pulsante -> Verde de escape)
            GameObject evacManagerObj = new GameObject("EvacuationLightingManager");
            evacManagerObj.transform.SetParent(environmentRoot.transform);
            evacManagerObj.AddComponent<ChronosEscape.Environment.EvacuationLightingManager>();

            // 7. Instanciar Mecanismo de Escape: Compuerta de Evacuación + Botón de Pared en Cámara Sur
            GameObject portalObj = CreatePortalMechanism(new Vector3(0, 0.5f, -44), matPortalLocked, matAccent, matWall);

            // 8. Instanciar Núcleos de Energía (Distribuidos equitativamente en cada ala accesible)
            List<GameObject> cores = new List<GameObject>();
            cores.Add(CreateEnergyCore(new Vector3(0, 1.4f, 0), "Core_CentralHub", matCore));
            cores.Add(CreateEnergyCore(new Vector3(0, 1.4f, 44), "Core_NorthChamber", matCore));
            cores.Add(CreateEnergyCore(new Vector3(46, 2.2f, 0), "Core_EastHighPlatform", matCore));
            cores.Add(CreateEnergyCore(new Vector3(-45, 1.4f, 0), "Core_WestChamber", matCore));
            cores.Add(CreateEnergyCore(new Vector3(0, 1.4f, -28), "Core_SouthHall", matCore));

            // 9. Instanciar Enemigos Centinela con IA y Waypoints (Exactamente 2 NPCs funcionales y estables)
            CreateSentinelEnemy(
                new Vector3(0, 1.1f, 40),
                new Vector3[] { 
                    new Vector3(-6, 1.1f, 44), 
                    new Vector3(6, 1.1f, 44), 
                    new Vector3(6, 1.1f, 38), 
                    new Vector3(-6, 1.1f, 38) 
                },
                matEnemy,
                "Sentinel_North"
            );

            CreateSentinelEnemy(
                new Vector3(0, 1.1f, 6),
                new Vector3[] { 
                    new Vector3(6, 1.1f, 6), 
                    new Vector3(6, 1.1f, -6), 
                    new Vector3(-6, 1.1f, -6), 
                    new Vector3(-6, 1.1f, 6) 
                },
                matEnemy,
                "Sentinel_CentralPatrol"
            );

            // 10. Instanciar Jugador
            GameObject player = CreatePlayer(new Vector3(0, 1.2f, -8));

            // 11. Crear Interfaz de Usuario (Canvas + EventSystem)
            GameObject canvasObj = CreateUserInterface();
            GameUIManager uiManager = canvasObj.GetComponent<GameUIManager>();

            // 12. Crear Gestores Centrales (GameManager, AudioManager, ObjectPooler)
            GameObject systemsRoot = new GameObject("=== MANAGERS ===");
            AudioManager audioMgr = systemsRoot.AddComponent<AudioManager>();
            ObjectPooler pooler = systemsRoot.AddComponent<ObjectPooler>();
            GameManager gameMgr = systemsRoot.AddComponent<GameManager>();

            // Enlazar los clips de audio desde Assets/Sounds y calibrar volúmenes
            audioMgr.audioPaso = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/pasos.wav");
            audioMgr.audioDanioJugador = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/golpe_npc.wav");
            audioMgr.musicaAmbiente = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/alarm.mp3");
            audioMgr.volumenMusica = 0.20f; // Volumen atenuado de alarma de fondo para que los pasos destaquen
            audioMgr.volumenEfectos = 1.0f;

            // Configurar Pool en ObjectPooler
            pooler.RegistrarNuevoPool("PickupEffect", pooledVfxPrefab, 10);
            gameMgr.ConfigurarReferencias(portalObj.GetComponentInChildren<EscapePortal>(), uiManager);

            // 13. Guardar escena
            string scenePath = "Assets/Scenes/MainLevel.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[LevelBuilder] Escena guardada exitosamente en {scenePath}");

            // Registrar en EditorBuildSettings
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = scenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelBuilder] ¡Generación del proyecto PA4 completada al 100%!");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private static Material CreateMaterial(string path, Color baseColor, float metallic, float smoothness, Color? emission = null)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Diffuse");

            Material mat = new Material(standardShader);
            mat.color = baseColor;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emission.Value);
                }
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static Material GetOrCreateParticleMaterial(string matName, Color color)
        {
            string path = $"Assets/Materials/{matName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = matName;
            mat.color = color;
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.5f);
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void SetupLighting()
        {
            // Luz Solar / Direccional ambiente
            GameObject dirLightObj = new GameObject("Directional_Light");
            Light dirLight = dirLightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(0.65f, 0.72f, 0.85f);
            dirLight.intensity = 0.55f;
            dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f);
        }

        private static void CreateFloor(Transform parent, Vector3 pos, Vector3 scale, Material mat, string name)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent);
            floor.transform.position = pos;
            floor.transform.localScale = scale;
            if (mat != null) floor.GetComponent<Renderer>().material = mat;
        }

        private static void CreateRamp(Transform parent, Vector3 pos, Vector3 scale, Vector3 rot, Material mat, string name)
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = name;
            ramp.transform.SetParent(parent);
            ramp.transform.position = pos;
            ramp.transform.localScale = scale;
            ramp.transform.rotation = Quaternion.Euler(rot);
            if (mat != null) ramp.GetComponent<Renderer>().material = mat;
        }

        private static void BuildCeilings(Transform parent, Material matCeiling)
        {
            GameObject ceilingGroup = new GameObject("Ceilings");
            ceilingGroup.transform.SetParent(parent);

            // Techos cubriendo todas las alas y pasillos a 6.25m de altura
            CreateFloor(ceilingGroup.transform, new Vector3(0, 6.25f, 0), new Vector3(30, 0.5f, 30), matCeiling, "Ceiling_CentralHub");
            CreateFloor(ceilingGroup.transform, new Vector3(0, 6.25f, 25), new Vector3(10, 0.5f, 20), matCeiling, "Ceiling_NorthHall");
            CreateFloor(ceilingGroup.transform, new Vector3(0, 6.25f, 44), new Vector3(28, 0.5f, 18), matCeiling, "Ceiling_NorthChamber");
            CreateFloor(ceilingGroup.transform, new Vector3(0, 6.25f, -25), new Vector3(10, 0.5f, 20), matCeiling, "Ceiling_SouthHall");
            CreateFloor(ceilingGroup.transform, new Vector3(0, 6.25f, -44), new Vector3(28, 0.5f, 18), matCeiling, "Ceiling_SouthChamber");
            CreateFloor(ceilingGroup.transform, new Vector3(-25, 6.25f, 0), new Vector3(20, 0.5f, 10), matCeiling, "Ceiling_WestHall");
            CreateFloor(ceilingGroup.transform, new Vector3(-45, 6.25f, 0), new Vector3(20, 0.5f, 28), matCeiling, "Ceiling_WestChamber");
            CreateFloor(ceilingGroup.transform, new Vector3(25, 6.25f, 0), new Vector3(20, 0.5f, 10), matCeiling, "Ceiling_EastHall");
            CreateFloor(ceilingGroup.transform, new Vector3(45, 6.25f, 0), new Vector3(20, 0.5f, 28), matCeiling, "Ceiling_EastChamber");
        }

        private static void CreateEmergencySiren(Transform parent, Vector3 pos, Material matAccent)
        {
            GameObject sirenObj = new GameObject("EmergencySiren");
            sirenObj.transform.SetParent(parent);
            sirenObj.transform.position = pos;

            // Soporte fixture en el techo
            GameObject fixture = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fixture.name = "SirenFixture";
            fixture.transform.SetParent(sirenObj.transform);
            fixture.transform.localPosition = new Vector3(0, 0.15f, 0);
            fixture.transform.localScale = new Vector3(0.7f, 0.2f, 0.7f);
            GameObject.DestroyImmediate(fixture.GetComponent<Collider>());
            if (matAccent != null) fixture.GetComponent<Renderer>().material = matAccent;

            // Domo translúcido de la sirena
            GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "SirenDome";
            dome.transform.SetParent(sirenObj.transform);
            dome.transform.localPosition = new Vector3(0, -0.1f, 0);
            dome.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            GameObject.DestroyImmediate(dome.GetComponent<Collider>());
            Renderer domeRend = dome.GetComponent<Renderer>();

            // Luz de punto ambiental roja del domo
            GameObject ptLightObj = new GameObject("SirenDomeLight");
            ptLightObj.transform.SetParent(dome.transform);
            ptLightObj.transform.localPosition = Vector3.zero;
            Light ptLight = ptLightObj.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.range = 8f;
            ptLight.intensity = 2.0f;
            ptLight.color = new Color(1f, 0.08f, 0.08f);

            // Cabezal giratorio
            GameObject headObj = new GameObject("RotatingHead");
            headObj.transform.SetParent(sirenObj.transform);
            headObj.transform.localPosition = new Vector3(0, -0.1f, 0);

            // Foco giratorio (Spotlight en ángulo hacia abajo y afuera)
            GameObject spotObj = new GameObject("SirenBeam");
            spotObj.transform.SetParent(headObj.transform);
            spotObj.transform.localPosition = Vector3.zero;
            spotObj.transform.localRotation = Quaternion.Euler(50f, 0f, 0f); // Inclinado 50 grados hacia abajo
            Light spotLight = spotObj.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = 70f;
            spotLight.range = 22f;
            spotLight.intensity = 4.5f;
            spotLight.color = new Color(1f, 0.08f, 0.08f);

            ChronosEscape.Environment.RotatingEmergencySiren sirenComp = sirenObj.AddComponent<ChronosEscape.Environment.RotatingEmergencySiren>();
            sirenComp.Configurar(headObj.transform, spotLight, ptLight, domeRend);
        }

        private static void BuildWallsAndCovers(Transform parent, Material matWall, Material matPillar, Material matAccent)
        {
            GameObject wallsGroup = new GameObject("Walls_And_Cover");
            wallsGroup.transform.SetParent(parent);

            // 1. Muros perimetrales Sala Central (dejando 4 accesos abiertos de 10m hacia Norte, Sur, Este y Oeste)
            CreateWall(wallsGroup.transform, new Vector3(-10, 3, 15), new Vector3(10, 6, 1), matWall);  // Norte Izquierda
            CreateWall(wallsGroup.transform, new Vector3(10, 3, 15), new Vector3(10, 6, 1), matWall);   // Norte Derecha

            CreateWall(wallsGroup.transform, new Vector3(-10, 3, -15), new Vector3(10, 6, 1), matWall); // Sur Izquierda
            CreateWall(wallsGroup.transform, new Vector3(10, 3, -15), new Vector3(10, 6, 1), matWall);  // Sur Derecha

            CreateWall(wallsGroup.transform, new Vector3(15, 3, 10), new Vector3(1, 6, 10), matWall);   // Este Arriba
            CreateWall(wallsGroup.transform, new Vector3(15, 3, -10), new Vector3(1, 6, 10), matWall);  // Este Abajo

            CreateWall(wallsGroup.transform, new Vector3(-15, 3, 10), new Vector3(1, 6, 10), matWall);  // Oeste Arriba (¡ABIERTO EN EL CENTRO!)
            CreateWall(wallsGroup.transform, new Vector3(-15, 3, -10), new Vector3(1, 6, 10), matWall); // Oeste Abajo (¡ABIERTO EN EL CENTRO!)

            // 2. Arcos Luminosos de Entrada con Indicadores Neón hacia cada sector
            CreateDoorwayArch(wallsGroup.transform, new Vector3(0, 0, 15), Quaternion.identity, matAccent, "Arch_NorthSector");
            CreateDoorwayArch(wallsGroup.transform, new Vector3(0, 0, -15), Quaternion.identity, matAccent, "Arch_SouthSector");
            CreateDoorwayArch(wallsGroup.transform, new Vector3(15, 0, 0), Quaternion.Euler(0, 90, 0), matAccent, "Arch_EastSector");
            CreateDoorwayArch(wallsGroup.transform, new Vector3(-15, 0, 0), Quaternion.Euler(0, 90, 0), matAccent, "Arch_WestSector");

            // 3. Muros laterales de los 4 Pasillos
            // Pasillo Norte
            CreateWall(wallsGroup.transform, new Vector3(-5.5f, 3, 25), new Vector3(1, 6, 20), matWall);
            CreateWall(wallsGroup.transform, new Vector3(5.5f, 3, 25), new Vector3(1, 6, 20), matWall);

            // Pasillo Sur
            CreateWall(wallsGroup.transform, new Vector3(-5.5f, 3, -25), new Vector3(1, 6, 20), matWall);
            CreateWall(wallsGroup.transform, new Vector3(5.5f, 3, -25), new Vector3(1, 6, 20), matWall);

            // Pasillo Oeste
            CreateWall(wallsGroup.transform, new Vector3(-25, 3, 5.5f), new Vector3(20, 6, 1), matWall);
            CreateWall(wallsGroup.transform, new Vector3(-25, 3, -5.5f), new Vector3(20, 6, 1), matWall);

            // Pasillo Este
            CreateWall(wallsGroup.transform, new Vector3(25, 3, 5.5f), new Vector3(20, 6, 1), matWall);
            CreateWall(wallsGroup.transform, new Vector3(25, 3, -5.5f), new Vector3(20, 6, 1), matWall);

            // 4. Perímetros exteriores de cada una de las 4 Salas
            // Sala Norte (Reactores)
            CreateWall(wallsGroup.transform, new Vector3(0, 3, 53.5f), new Vector3(28, 6, 1), matWall);
            CreateWall(wallsGroup.transform, new Vector3(-14.5f, 3, 44), new Vector3(1, 6, 18), matWall);
            CreateWall(wallsGroup.transform, new Vector3(14.5f, 3, 44), new Vector3(1, 6, 18), matWall);

            // Sala Sur (Portal de Escape) - Muro perimetral con vano central abierto de 5.6m para encajar la compuerta Sci-Fi
            CreateWall(wallsGroup.transform, new Vector3(-8.4f, 3, -53.5f), new Vector3(11.2f, 6, 1), matWall); // Muro sur izquierdo
            CreateWall(wallsGroup.transform, new Vector3(8.4f, 3, -53.5f), new Vector3(11.2f, 6, 1), matWall);  // Muro sur derecho
            CreateWall(wallsGroup.transform, new Vector3(0, 5.2f, -53.5f), new Vector3(5.6f, 1.6f, 1), matWall);  // Dintel superior sobre la compuerta
            CreateWall(wallsGroup.transform, new Vector3(-14.5f, 3, -44), new Vector3(1, 6, 18), matWall);
            CreateWall(wallsGroup.transform, new Vector3(14.5f, 3, -44), new Vector3(1, 6, 18), matWall);

            // Sala Oeste (Generadores)
            CreateWall(wallsGroup.transform, new Vector3(-55.5f, 3, 0), new Vector3(1, 6, 28), matWall);
            CreateWall(wallsGroup.transform, new Vector3(-45, 3, 14.5f), new Vector3(20, 6, 1), matWall);
            CreateWall(wallsGroup.transform, new Vector3(-45, 3, -14.5f), new Vector3(20, 6, 1), matWall);

            // Sala Este (Observatorio y Plataformas)
            CreateWall(wallsGroup.transform, new Vector3(55.5f, 3, 0), new Vector3(1, 6, 28), matWall);
            CreateWall(wallsGroup.transform, new Vector3(45, 3, 14.5f), new Vector3(20, 6, 1), matWall);
            CreateWall(wallsGroup.transform, new Vector3(45, 3, -14.5f), new Vector3(20, 6, 1), matWall);

            // 5. Columnas y Coberturas Tácticas (para ocultarse de los enemigos)
            CreatePillar(wallsGroup.transform, new Vector3(-6, 3, -6), matPillar, matAccent);
            CreatePillar(wallsGroup.transform, new Vector3(6, 3, -6), matPillar, matAccent);
            CreatePillar(wallsGroup.transform, new Vector3(-6, 3, 6), matPillar, matAccent);
            CreatePillar(wallsGroup.transform, new Vector3(6, 3, 6), matPillar, matAccent);

            // ==================== COBERTURAS TÁCTICAS: CAJAS Y CONTENEDORES MINIMAL SCIFI PACK ====================
            // Reemplazo total de cubos genéricos primitivos por las cajas 3D del Minimal Scifi Pack
            // 1. Hub Central
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-3f, 0f, 2f), Quaternion.Euler(0, 15f, 0), "SM_Trim_L_Container1", new Vector3(1.1f, 1.1f, 1.1f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-2.2f, 1.2f, 2f), Quaternion.Euler(0, -25f, 0), "SM_Box1", Vector3.one);

            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(3f, 0f, -2f), Quaternion.Euler(0, -30f, 0), "SM_Trim_L_Container2", new Vector3(1.1f, 1.1f, 1.1f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(3.6f, 1.2f, -2f), Quaternion.Euler(0, 10f, 0), "SM_Box1", Vector3.one);

            // 2. Sala Oeste (Generadores)
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-45f, 0f, 5f), Quaternion.Euler(0, 45f, 0), "SM_Trim_L_Container1", new Vector3(1.2f, 1.2f, 1.2f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-44.2f, 0f, 6.2f), Quaternion.identity, "SM_Box1", new Vector3(1.1f, 1.1f, 1.1f));

            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-45f, 0f, -5f), Quaternion.Euler(0, -45f, 0), "SM_Trim_L_Container2", new Vector3(1.2f, 1.2f, 1.2f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-46.2f, 0f, -5f), Quaternion.identity, "SM_Canister1", new Vector3(1.2f, 1.2f, 1.2f));

            // 3. Sala Norte (Reactores)
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(0f, 0f, 39f), Quaternion.Euler(0, 90f, 0), "SM_Trim_L_Container1", new Vector3(1.3f, 1.3f, 1.3f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(1.3f, 0f, 39f), Quaternion.Euler(0, 20f, 0), "SM_Box1", Vector3.one);
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-1.3f, 0f, 39f), Quaternion.Euler(0, -15f, 0), "SM_Box1", Vector3.one);

            // 4. Sala Sur (Cámara de Evacuación)
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(-8f, 0f, -46f), Quaternion.Euler(0, 35f, 0), "SM_Trim_L_Container2", new Vector3(1.2f, 1.2f, 1.2f));
            PlaceMinimalScifiCrate(wallsGroup.transform, new Vector3(8f, 0f, -46f), Quaternion.Euler(0, -35f, 0), "SM_Trim_L_Container1", new Vector3(1.2f, 1.2f, 1.2f));

            // ==================== ASSETS SCI-FI IMPORTADOS (OFFICE PACK & MODULAR PACK) ====================
            // 1. Hub Central: Puesto de control, servidores y contenedores de carga
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-4f, 0.5f, 6f), Quaternion.Euler(0, 170f, 0), "Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-4f, 1.35f, 6f), Quaternion.Euler(0, 170f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", Vector3.one, false);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-4f, 0.5f, 4.8f), Quaternion.Euler(0, 10f, 0), "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", Vector3.one);

            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-13.5f, 0.5f, 8f), Quaternion.Euler(0, 90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-13.5f, 0.5f, 5.5f), Quaternion.Euler(0, 90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(13.5f, 0.5f, 8f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(13.5f, 0.5f, 5.5f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);

            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-11f, 0.5f, -11f), Quaternion.Euler(0, 25f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box1.prefab", new Vector3(1.4f, 1.4f, 1.4f));
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-12.5f, 0.5f, -9.5f), Quaternion.Euler(0, -15f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box3.prefab", new Vector3(1.3f, 1.3f, 1.3f));
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(11f, 0.5f, -11f), Quaternion.Euler(0, 40f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box1.prefab", new Vector3(1.4f, 1.4f, 1.4f));

            // 2. Sala Oeste: Estantería industrial, brazo robótico de ingeniería y cajas de reactores
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-53f, 0.5f, 7f), Quaternion.Euler(0, 90f, 0), "Assets/ScifiOfficeLite/Prefabs/Shelf with Crates.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-45f, 0.5f, 10f), Quaternion.Euler(0, 180f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Mechanical arm 1.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-52f, 0.5f, -8f), Quaternion.Euler(0, 30f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box1.prefab", new Vector3(1.6f, 1.6f, 1.6f));
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-50f, 0.5f, -8f), Quaternion.Euler(0, -20f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box3.prefab", new Vector3(1.4f, 1.4f, 1.4f));

            // 3. Sala Norte: Estación de telemetría de reactores, racks de contención y suministros
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(11f, 0.5f, 45f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(11f, 1.35f, 45f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", Vector3.one, false);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(9.8f, 0.5f, 45f), Quaternion.Euler(0, 90f, 0), "Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-12f, 0.5f, 50f), Quaternion.identity, "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-10f, 0.5f, 50f), Quaternion.identity, "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(-11f, 0.5f, 38f), Quaternion.Euler(0, 15f, 0), "Assets/Sci Fi Modular Pack/Prefabs/Box1.prefab", new Vector3(1.5f, 1.5f, 1.5f));

            // 4. Sala Este: Puesto de observación sobre la plataforma elevada
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(50f, 1.3f, 4f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab", Vector3.one);
            PlaceSciFiAsset(wallsGroup.transform, new Vector3(50f, 2.15f, 4f), Quaternion.Euler(0, -90f, 0), "Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab", Vector3.one, false);
        }

        private static GameObject PlaceSciFiAsset(Transform parent, Vector3 pos, Quaternion rot, string prefabPath, Vector3 scale, bool addCollider = true)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return null;

            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;

            if (addCollider && instance.GetComponentInChildren<Collider>() == null)
            {
                Renderer rend = instance.GetComponentInChildren<Renderer>();
                BoxCollider bc = instance.AddComponent<BoxCollider>();
                if (rend != null)
                {
                    Vector3 localCenter = instance.transform.InverseTransformPoint(rend.bounds.center);
                    bc.center = localCenter;
                    bc.size = new Vector3(
                        Mathf.Max(0.6f, rend.bounds.size.x / Mathf.Max(0.01f, scale.x)),
                        Mathf.Max(0.6f, rend.bounds.size.y / Mathf.Max(0.01f, scale.y)),
                        Mathf.Max(0.6f, rend.bounds.size.z / Mathf.Max(0.01f, scale.z))
                    );
                }
                else
                {
                    bc.center = new Vector3(0, 0.5f, 0);
                    bc.size = Vector3.one;
                }
            }
            return instance;
        }

        private static void CreateDoorwayArch(Transform parent, Vector3 pos, Quaternion rot, Material matAccent, string name)
        {
            GameObject arch = new GameObject(name);
            arch.transform.SetParent(parent);
            arch.transform.position = pos;
            arch.transform.rotation = rot;

            // Poste izquierdo
            GameObject postL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postL.name = "ArchPost_L";
            postL.transform.SetParent(arch.transform);
            postL.transform.localPosition = new Vector3(-5.2f, 3f, 0);
            postL.transform.localScale = new Vector3(0.6f, 6f, 0.8f);
            if (matAccent != null) postL.GetComponent<Renderer>().material = matAccent;

            // Poste derecho
            GameObject postR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            postR.name = "ArchPost_R";
            postR.transform.SetParent(arch.transform);
            postR.transform.localPosition = new Vector3(5.2f, 3f, 0);
            postR.transform.localScale = new Vector3(0.6f, 6f, 0.8f);
            if (matAccent != null) postR.GetComponent<Renderer>().material = matAccent;

            // Dintel superior
            GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = "ArchLintel";
            lintel.transform.SetParent(arch.transform);
            lintel.transform.localPosition = new Vector3(0, 5.8f, 0);
            lintel.transform.localScale = new Vector3(11f, 0.6f, 0.8f);
            if (matAccent != null) lintel.GetComponent<Renderer>().material = matAccent;

            // Luz suave para marcar el paso
            GameObject lightObj = new GameObject("ArchLight");
            lightObj.transform.SetParent(arch.transform);
            lightObj.transform.localPosition = new Vector3(0, 4.5f, 0);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0f, 0.85f, 1f);
            l.intensity = 2.0f;
            l.range = 8f;
        }

        private static void EnsureMinimalScifiMaterialsConverted()
        {
            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) return;

            Texture2D colorTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dzeruza/MinimalScifiPack/Textures&Materials/T_Trim_Walls_Color.png");
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Dzeruza/MinimalScifiPack/Textures&Materials/T_Trim_Walls_Normal.png");

            string[] matPaths = new string[] {
                "Assets/Dzeruza/MinimalScifiPack/Textures&Materials/M_Trim_Walls.mat",
                "Assets/Dzeruza/MinimalScifiPack/Textures&Materials/M_Trim_Walls_Displacement.mat"
            };

            foreach (string p in matPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m != null)
                {
                    m.shader = standardShader;
                    if (colorTex != null) m.mainTexture = colorTex;
                    if (normalTex != null)
                    {
                        m.SetTexture("_BumpMap", normalTex);
                        m.EnableKeyword("_NORMALMAP");
                    }
                    m.SetFloat("_Glossiness", 0.4f);
                    m.SetFloat("_Metallic", 0.2f);
                    EditorUtility.SetDirty(m);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static GameObject PlaceMinimalScifiCrate(Transform parent, Vector3 pos, Quaternion rot, string prefabName, Vector3 scale)
        {
            string path = $"Assets/Dzeruza/MinimalScifiPack/3DModels/Props/Prefabs/{prefabName}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) return null;

            instance.name = $"MinimalScifi_{prefabName}";
            instance.transform.SetParent(parent);
            instance.transform.position = pos;
            instance.transform.rotation = rot;
            instance.transform.localScale = scale;

            // Asegurar que ningún Renderer tenga shader incompatible (magenta/rosa HDRP)
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Dzeruza/MinimalScifiPack/Textures&Materials/M_Trim_Walls.mat");
            if (trimMat != null)
            {
                foreach (Renderer rend in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = rend.sharedMaterials;
                    bool updated = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || mats[i].shader == null || mats[i].shader.name == "Hidden/InternalErrorShader" || mats[i].shader.name.Contains("HDRP"))
                        {
                            mats[i] = trimMat;
                            updated = true;
                        }
                    }
                    if (updated)
                    {
                        rend.sharedMaterials = mats;
                    }
                }
            }

            if (instance.GetComponentInChildren<Collider>() == null)
            {
                Renderer rend = instance.GetComponentInChildren<Renderer>();
                BoxCollider bc = instance.AddComponent<BoxCollider>();
                if (rend != null)
                {
                    Vector3 localCenter = instance.transform.InverseTransformPoint(rend.bounds.center);
                    bc.center = localCenter;
                    bc.size = new Vector3(
                        Mathf.Max(0.6f, rend.bounds.size.x / Mathf.Max(0.01f, scale.x)),
                        Mathf.Max(0.6f, rend.bounds.size.y / Mathf.Max(0.01f, scale.y)),
                        Mathf.Max(0.6f, rend.bounds.size.z / Mathf.Max(0.01f, scale.z))
                    );
                }
                else
                {
                    bc.center = new Vector3(0, 0.5f, 0);
                    bc.size = Vector3.one;
                }
            }
            return instance;
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            if (mat != null) wall.GetComponent<Renderer>().material = mat;
        }

        private static void CreatePillar(Transform parent, Vector3 pos, Material matPillar, Material matAccent)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(parent);
            pillar.transform.position = pos;
            pillar.transform.localScale = new Vector3(1.4f, 3f, 1.4f);
            if (matPillar != null) pillar.GetComponent<Renderer>().material = matPillar;

            // Luz suave en el pilar para iluminar y ambientar
            GameObject lightObj = new GameObject("PillarLight");
            lightObj.transform.SetParent(pillar.transform);
            lightObj.transform.position = pos + Vector3.up * 1.5f;
            Light pl = lightObj.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = new Color(0.1f, 0.8f, 1f);
            pl.intensity = 1.2f;
            pl.range = 8f;
        }

        private static GameObject CreatePickupVfxPrefab(Material matCore)
        {
            string prefabPath = "Assets/Prefabs/VFX_PickupBurst.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null)
            {
                // Asegurar que el prefab existente tenga material asignado
                ParticleSystemRenderer existingPsr = existing.GetComponent<ParticleSystemRenderer>();
                if (existingPsr != null && existingPsr.sharedMaterial == null)
                {
                    existingPsr.sharedMaterial = GetOrCreateParticleMaterial("Mat_PickupParticle", new Color(1f, 0.85f, 0.2f, 1f));
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            GameObject vfxObj = new GameObject("VFX_PickupBurst");
            ParticleSystem ps = vfxObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 4.5f;
            main.startSize = 0.25f;
            main.startColor = new Color(1f, 0.85f, 0.2f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            // Asignar material de partículas para evitar el error de texturas rosadas
            ParticleSystemRenderer psr = vfxObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = GetOrCreateParticleMaterial("Mat_PickupParticle", new Color(1f, 0.85f, 0.2f, 1f));
            }

            PooledParticleEffect pooledEffect = vfxObj.AddComponent<PooledParticleEffect>();
            pooledEffect.Setup("PickupEffect", 1.2f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(vfxObj, prefabPath);
            GameObject.DestroyImmediate(vfxObj);
            return prefab;
        }

        private static void CreateAtmosphericDust(Transform parent)
        {
            GameObject dustObj = new GameObject("AtmosphericDust");
            dustObj.transform.SetParent(parent);
            dustObj.transform.position = new Vector3(0, 4f, 0);

            ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 6f;
            main.startSpeed = 0.2f;
            main.startSize = 0.08f;
            main.startColor = new Color(0.4f, 0.7f, 1f, 0.35f);
            main.maxParticles = 150;

            var emission = ps.emission;
            emission.rateOverTime = 20;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(50, 8, 50);

            // Material para partículas ambientales (sin textura rosada)
            ParticleSystemRenderer psr = dustObj.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = GetOrCreateParticleMaterial("Mat_AtmosphericDust", new Color(0.4f, 0.7f, 1f, 0.35f));
            }
        }

        private static GameObject CreateEnergyCore(Vector3 pos, string name, Material matCore)
        {
            GameObject coreGroup = new GameObject(name);
            coreGroup.transform.position = pos;

            // Esfera central luminosa (Núcleo de Fusión)
            GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "CoreSphere";
            coreSphere.transform.SetParent(coreGroup.transform);
            coreSphere.transform.localPosition = Vector3.zero;
            coreSphere.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            if (matCore != null) coreSphere.GetComponent<Renderer>().material = matCore;

            // Anillos orbitales giratorios para aspecto de reactor cuántico futurista
            GameObject ring1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring1.name = "OrbitalRing_Outer";
            ring1.transform.SetParent(coreGroup.transform);
            ring1.transform.localPosition = Vector3.zero;
            ring1.transform.localScale = new Vector3(1.35f, 0.04f, 1.35f);
            ring1.transform.localRotation = Quaternion.Euler(45f, 0, 45f);
            GameObject.DestroyImmediate(ring1.GetComponent<Collider>());
            if (matCore != null) ring1.GetComponent<Renderer>().material = matCore;

            GameObject ring2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring2.name = "OrbitalRing_Inner";
            ring2.transform.SetParent(coreGroup.transform);
            ring2.transform.localPosition = Vector3.zero;
            ring2.transform.localScale = new Vector3(1.1f, 0.04f, 1.1f);
            ring2.transform.localRotation = Quaternion.Euler(-45f, 30f, -45f);
            GameObject.DestroyImmediate(ring2.GetComponent<Collider>());
            if (matCore != null) ring2.GetComponent<Renderer>().material = matCore;

            // Trigger Collider sobre el grupo principal
            SphereCollider col = coreGroup.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 1.0f;

            // Remover el collider de la esfera interna para evitar dobles triggers
            Collider sphereCol = coreSphere.GetComponent<Collider>();
            if (sphereCol != null) GameObject.DestroyImmediate(sphereCol);

            // Luz brillante dorada
            Light coreLight = coreGroup.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = new Color(1f, 0.8f, 0.2f);
            coreLight.intensity = 2.5f;
            coreLight.range = 6.0f;

            // Partículas de brillo flotante con material asignado
            ParticleSystem ps = coreGroup.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = 0.5f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.9f, 0.3f, 0.8f);

            var emission = ps.emission;
            emission.rateOverTime = 12;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ParticleSystemRenderer psr = coreGroup.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.sharedMaterial = GetOrCreateParticleMaterial("Mat_CoreParticle", new Color(1f, 0.9f, 0.3f, 0.8f));
            }

            coreGroup.AddComponent<CollectibleCore>();
            return coreGroup;
        }

        private static GameObject CreatePortalMechanism(Vector3 pos, Material matLocked, Material matAccent, Material matWall)
        {
            GameObject portalGroup = new GameObject("EscapePortal_Mechanism");
            portalGroup.transform.position = pos;

            // ==================== COMPUERTA DE EVACUACIÓN (SCI FI DOORS ASSET) ====================
            // La pared sur perimetral está en z_global = -53.5f. Con portalGroup en pos = (0, 0.5f, -44), local z = -9.4f
            GameObject doorRoot = new GameObject("EscapeBlastDoor");
            doorRoot.transform.SetParent(portalGroup.transform);
            doorRoot.transform.localPosition = new Vector3(0, 0, -9.4f);

            // Cargar e instanciar el prefab "Sci Fi Gates.prefab" del paquete oficial Sci Fi Doors
            GameObject gatesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MASH Virtual/Sci Fi Doors/Prefab/Sci Fi Gates.prefab");
            Transform pLeft = null;
            Transform pRight = null;

            if (gatesPrefab != null)
            {
                GameObject gatesInstance = (GameObject)PrefabUtility.InstantiatePrefab(gatesPrefab);
                gatesInstance.name = "SciFiGates_BlastDoor";
                gatesInstance.transform.SetParent(doorRoot.transform);
                // Con rotación (0, -90, 0), el ancho (5.51m) queda paralelo al muro X y el frente mira hacia +Z (hacia el jugador)
                // Offset x = -1.34f centra exactamente el marco de la compuerta en x = 0
                gatesInstance.transform.localPosition = new Vector3(-1.34f, 0, 0);
                gatesInstance.transform.localRotation = Quaternion.Euler(0, -90f, 0);
                gatesInstance.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

                pLeft = gatesInstance.transform.Find("Door_Left");
                pRight = gatesInstance.transform.Find("Door_Right");

                // Añadir BoxCollider a las hojas de la puerta para bloquear el paso mientras esté cerrada
                if (pLeft != null && pLeft.GetComponent<Collider>() == null)
                {
                    BoxCollider bcL = pLeft.gameObject.AddComponent<BoxCollider>();
                    bcL.size = new Vector3(0.5f, 3.2f, 2.2f);
                    bcL.center = new Vector3(0f, 1.5f, 0.9f);
                }
                if (pRight != null && pRight.GetComponent<Collider>() == null)
                {
                    BoxCollider bcR = pRight.gameObject.AddComponent<BoxCollider>();
                    bcR.size = new Vector3(0.5f, 3.2f, 2.2f);
                    bcR.center = new Vector3(0f, 1.5f, -0.9f);
                }
                Transform frame = gatesInstance.transform.Find("Frame");
                if (frame != null && frame.GetComponent<Collider>() == null)
                {
                    frame.gameObject.AddComponent<MeshCollider>();
                }
            }

            // ==================== TÚNEL DE EXTRACCIÓN DETRÁS DE LA COMPUERTA ====================
            GameObject tunnelFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelFloor.name = "EscapeTunnelFloor";
            tunnelFloor.transform.SetParent(doorRoot.transform);
            tunnelFloor.transform.localPosition = new Vector3(0, -0.4f, -4f);
            tunnelFloor.transform.localScale = new Vector3(5.6f, 0.4f, 8f);
            if (matAccent != null) tunnelFloor.GetComponent<Renderer>().material = matAccent;

            GameObject tunnelCeiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelCeiling.name = "EscapeTunnelCeiling";
            tunnelCeiling.transform.SetParent(doorRoot.transform);
            tunnelCeiling.transform.localPosition = new Vector3(0, 5.4f, -4f);
            tunnelCeiling.transform.localScale = new Vector3(5.6f, 0.4f, 8f);
            if (matWall != null) tunnelCeiling.GetComponent<Renderer>().material = matWall;

            GameObject tunnelWallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelWallL.name = "EscapeTunnelWall_L";
            tunnelWallL.transform.SetParent(doorRoot.transform);
            tunnelWallL.transform.localPosition = new Vector3(-2.8f, 2.5f, -4f);
            tunnelWallL.transform.localScale = new Vector3(0.5f, 6f, 8f);
            if (matWall != null) tunnelWallL.GetComponent<Renderer>().material = matWall;

            GameObject tunnelWallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelWallR.name = "EscapeTunnelWall_R";
            tunnelWallR.transform.SetParent(doorRoot.transform);
            tunnelWallR.transform.localPosition = new Vector3(2.8f, 2.5f, -4f);
            tunnelWallR.transform.localScale = new Vector3(0.5f, 6f, 8f);
            if (matWall != null) tunnelWallR.GetComponent<Renderer>().material = matWall;

            GameObject tunnelBackWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tunnelBackWall.name = "EscapeTunnelBackWall";
            tunnelBackWall.transform.SetParent(doorRoot.transform);
            tunnelBackWall.transform.localPosition = new Vector3(0, 2.5f, -8f);
            tunnelBackWall.transform.localScale = new Vector3(6f, 6f, 0.5f);
            if (matWall != null) tunnelBackWall.GetComponent<Renderer>().material = matWall;

            GameObject tunnelLight = new GameObject("EscapeTunnelLight");
            tunnelLight.transform.SetParent(doorRoot.transform);
            tunnelLight.transform.localPosition = new Vector3(0, 3f, -4f);
            Light tLight = tunnelLight.AddComponent<Light>();
            tLight.type = LightType.Point;
            tLight.color = new Color(0.2f, 1f, 0.6f);
            tLight.range = 14f;
            tLight.intensity = 4.0f;

            // ==================== BOTÓN DE PARED DE EVACUACIÓN ====================
            // Montado en la pared derecha (x = 3.8m, z_local = 0.15m), perfectamente visible y plano al lado de la compuerta
            GameObject wallButtonConsole = new GameObject("WallButtonConsole");
            wallButtonConsole.transform.SetParent(doorRoot.transform);
            wallButtonConsole.transform.localPosition = new Vector3(3.8f, 1.4f, 0.15f);

            // Soporte en la pared
            GameObject buttonBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonBox.name = "ConsoleHousing";
            buttonBox.transform.SetParent(wallButtonConsole.transform);
            buttonBox.transform.localPosition = Vector3.zero;
            buttonBox.transform.localScale = new Vector3(0.8f, 1.2f, 0.25f);
            if (matAccent != null) buttonBox.GetComponent<Renderer>().material = matAccent;

            // Botón físico redondo
            GameObject physicalButton = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            physicalButton.name = "EmergencyWallButton";
            physicalButton.transform.SetParent(wallButtonConsole.transform);
            physicalButton.transform.localPosition = new Vector3(0, 0, 0.15f);
            physicalButton.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
            physicalButton.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            if (matLocked != null) physicalButton.GetComponent<Renderer>().material = matLocked;

            // Luz emisiva del botón de pared
            GameObject btnLightObj = new GameObject("WallButtonLight");
            btnLightObj.transform.SetParent(wallButtonConsole.transform);
            btnLightObj.transform.localPosition = new Vector3(0, 0, 0.4f);
            Light btnLight = btnLightObj.AddComponent<Light>();
            btnLight.type = LightType.Point;
            btnLight.color = new Color(0.9f, 0.15f, 0.15f); // Rojo inicial
            btnLight.intensity = 2.5f;
            btnLight.range = 5f;

            // ==================== TRIGGER DE DETECCIÓN Y VICTORIA ====================
            GameObject triggerObj = new GameObject("PortalTrigger");
            triggerObj.transform.SetParent(doorRoot.transform);
            triggerObj.transform.localPosition = new Vector3(0, 1.5f, 2.0f); // Delante de la compuerta y botón
            
            BoxCollider boxCol = triggerObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(10f, 5f, 7f); // Amplio volumen de detección

            Rigidbody rb = triggerObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            // Script de mecanismo con compuerta Sci-Fi Doors animada y botón de pared enlazados (sin círculo ni partículas obsoletas)
            EscapePortal escapePortal = triggerObj.AddComponent<EscapePortal>();
            escapePortal.ConfigurarReferencias(null, null, null, null);
            escapePortal.ConfigurarCompuertaYBoton(physicalButton.GetComponent<Renderer>(), btnLight, pLeft, pRight);

            return portalGroup;
        }

        private static void CreateSentinelEnemy(Vector3 startPos, Vector3[] waypointPositions, Material matEnemy, string name)
        {
            GameObject enemyGroup = new GameObject(name);
            enemyGroup.transform.position = startPos;

            // Colisionador físico CharacterController para impedir que atraviese paredes
            CharacterController cc = enemyGroup.AddComponent<CharacterController>();
            cc.height = 1.2f;
            cc.radius = 0.5f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f;

            // Cuerpo del Dron (Esfera con cilindros o cubo flotante futurista)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "SentinelBody";
            body.transform.SetParent(enemyGroup.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            if (matEnemy != null) body.GetComponent<Renderer>().material = matEnemy;
            // Eliminar colisionador del hijo para evitar trabas físicas internas y con obstáculos
            Collider bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) GameObject.DestroyImmediate(bodyCol);

            // Ojo / Lente frontal
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "SentinelEye";
            eye.transform.SetParent(body.transform);
            eye.transform.localPosition = new Vector3(0, 0, 0.45f);
            eye.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            // Eliminar colisionador del ojo
            Collider eyeCol = eye.GetComponent<Collider>();
            if (eyeCol != null) GameObject.DestroyImmediate(eyeCol);

            // Luz de escaneo
            GameObject lightObj = new GameObject("EyeLight");
            lightObj.transform.SetParent(body.transform);
            lightObj.transform.localPosition = new Vector3(0, 0, 0.6f);
            Light eyeLight = lightObj.AddComponent<Light>();
            eyeLight.type = LightType.Spot;
            eyeLight.spotAngle = 60f;
            eyeLight.range = 14f;
            eyeLight.intensity = 3.5f;

            // Waypoints
            Transform[] wpTransforms = new Transform[waypointPositions.Length];
            GameObject wpGroup = new GameObject($"{name}_Waypoints");
            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject wp = new GameObject($"WP_{i + 1}");
                wp.transform.SetParent(wpGroup.transform);
                wp.transform.position = waypointPositions[i];
                wpTransforms[i] = wp.transform;
            }

            EnemyAI ai = enemyGroup.AddComponent<EnemyAI>();
            ai.AsignarWaypoints(wpTransforms);
            ai.ConfigurarReferencias(eyeLight, eye.GetComponent<Renderer>());
        }

        private static GameObject CreatePlayer(Vector3 startPos)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = startPos;

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1.0f, 0);

            PlayerController playerCtrl = player.AddComponent<PlayerController>();

            // Cámara del jugador en primera persona
            GameObject camObj = new GameObject("PlayerCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0, 1.7f, 0);

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 150f;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<MouseLook>();

            return player;
        }

        private static GameObject CreateUserInterface()
        {
            // EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            // Canvas Raíz
            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            GameUIManager uiManager = canvasObj.AddComponent<GameUIManager>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Damage Flash Overlay
            GameObject flashObj = new GameObject("DamageFlashOverlay");
            flashObj.transform.SetParent(canvasObj.transform, false);
            Image flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(1, 0, 0, 0);
            flashImg.raycastTarget = false;
            StretchToFull(flashObj.GetComponent<RectTransform>());

            // --- HUD TOP-LEFT (Salud) ---
            GameObject healthPanel = CreateUIBox(canvasObj.transform, "HealthHUD", new Vector2(20, -20), new Vector2(280, 55), new Vector2(0, 1), new Vector2(0, 1), new Color(0.08f, 0.10f, 0.14f, 0.85f));

            // Slider Barra de Salud
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(healthPanel.transform, false);
            Slider healthSlider = sliderObj.AddComponent<Slider>();
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(10, -10);
            sliderRect.sizeDelta = new Vector2(260, 22);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(sliderObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.9f, 0.3f);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            StretchToFull(fillRect);
            healthSlider.fillRect = fillRect;

            // Texto de Salud
            Text healthTxt = CreateText(healthPanel.transform, "HealthText", "100 / 100 HP", 16, defaultFont, Color.white, new Vector2(10, -34), new Vector2(260, 20));

            // --- HUD TOP-RIGHT (Contador de Núcleos) ---
            GameObject coresPanel = CreateUIBox(canvasObj.transform, "CoresHUD", new Vector2(-20, -20), new Vector2(320, 55), new Vector2(1, 1), new Vector2(1, 1), new Color(0.08f, 0.10f, 0.14f, 0.85f));
            Text coresTxt = CreateText(coresPanel.transform, "CoresText", "NÚCLEOS DE ENERGÍA: 0 / 5", 18, defaultFont, new Color(1f, 0.85f, 0.2f), new Vector2(15, -15), new Vector2(290, 30));

            // --- HUD TOP-CENTER (Objetivo) ---
            GameObject objPanel = CreateUIBox(canvasObj.transform, "ObjectiveHUD", new Vector2(0, -25), new Vector2(650, 45), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Color(0.05f, 0.08f, 0.12f, 0.85f));
            Text objTxt = CreateText(objPanel.transform, "ObjectiveText", "OBJETIVO: Recolecta los 5 núcleos de energía.", 17, defaultFont, new Color(0.3f, 0.9f, 1f), new Vector2(10, -10), new Vector2(630, 25), TextAnchor.MiddleCenter);

            // --- NOTIFICACIÓN TEMPORAL (Centro-Inferior) ---
            Text notifTxt = CreateText(canvasObj.transform, "NotificationText", "", 22, defaultFont, Color.white, new Vector2(0, 160), new Vector2(800, 40), TextAnchor.MiddleCenter);
            RectTransform notifRect = notifTxt.GetComponent<RectTransform>();
            notifRect.anchorMin = new Vector2(0.5f, 0);
            notifRect.anchorMax = new Vector2(0.5f, 0);

            // --- PANTALLA DE VICTORIA ---
            GameObject victoryPanel = CreateUIBox(canvasObj.transform, "VictoryPanel", Vector2.zero, new Vector2(600, 460), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.05f, 0.12f, 0.18f, 0.96f));
            CreateText(victoryPanel.transform, "Title", "¡MISIÓN CUMPLIDA!", 32, defaultFont, new Color(0.2f, 1f, 0.6f), new Vector2(0, -30), new Vector2(520, 50), TextAnchor.MiddleCenter);
            Text victoryStats = CreateText(victoryPanel.transform, "StatsText", "¡Has escapado con éxito!", 17, defaultFont, Color.white, new Vector2(0, -115), new Vector2(520, 140), TextAnchor.MiddleCenter);
            CreateButton(victoryPanel.transform, "RestartButton", "JUGAR DE NUEVO", defaultFont, new Vector2(0, -280), new Vector2(270, 48), null);
            CreateButton(victoryPanel.transform, "QuitButton", "SALIR AL ESCRITORIO", defaultFont, new Vector2(0, -350), new Vector2(270, 48), null);
            CreateText(victoryPanel.transform, "ShortcutsHint", "[R / Espacio / Enter] Jugar de nuevo   |   [ESC / Q] Salir", 14, defaultFont, new Color(0.85f, 0.85f, 0.85f, 0.85f), new Vector2(0, -415), new Vector2(520, 30), TextAnchor.MiddleCenter);

            // --- PANTALLA DE DERROTA ---
            GameObject defeatPanel = CreateUIBox(canvasObj.transform, "DefeatPanel", Vector2.zero, new Vector2(560, 430), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.18f, 0.05f, 0.05f, 0.96f));
            CreateText(defeatPanel.transform, "Title", "CONEXIÓN PERDIDA", 32, defaultFont, new Color(1f, 0.2f, 0.2f), new Vector2(0, -35), new Vector2(500, 50), TextAnchor.MiddleCenter);
            CreateText(defeatPanel.transform, "Subtitle", "El centinela neutralizó al explorador.\nNo fue posible activar el protocolo de escape.", 18, defaultFont, Color.white, new Vector2(0, -115), new Vector2(480, 70), TextAnchor.MiddleCenter);
            CreateButton(defeatPanel.transform, "RetryButton", "REINTENTAR MISIÓN", defaultFont, new Vector2(0, -220), new Vector2(270, 48), null);
            CreateButton(defeatPanel.transform, "QuitButton", "SALIR AL ESCRITORIO", defaultFont, new Vector2(0, -290), new Vector2(270, 48), null);
            CreateText(defeatPanel.transform, "ShortcutsHint", "[R / Espacio / Enter] Reintentar   |   [ESC / Q] Salir", 14, defaultFont, new Color(0.85f, 0.85f, 0.85f, 0.85f), new Vector2(0, -370), new Vector2(500, 30), TextAnchor.MiddleCenter);

            // --- PANTALLA DE PAUSA ---
            GameObject pausePanel = CreateUIBox(canvasObj.transform, "PausePanel", Vector2.zero, new Vector2(420, 380), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(0.08f, 0.10f, 0.15f, 0.95f));
            CreateText(pausePanel.transform, "Title", "PAUSA", 30, defaultFont, Color.white, new Vector2(0, -35), new Vector2(300, 50), TextAnchor.MiddleCenter);
            CreateButton(pausePanel.transform, "ResumeButton", "CONTINUAR", defaultFont, new Vector2(0, -130), new Vector2(240, 45), null);
            CreateButton(pausePanel.transform, "RestartPauseButton", "REINICIAR NIVEL", defaultFont, new Vector2(0, -200), new Vector2(240, 45), null);
            CreateButton(pausePanel.transform, "QuitButton", "SALIR AL ESCRITORIO", defaultFont, new Vector2(0, -270), new Vector2(240, 45), null);

            // Vincular campos de UIManager directamente
            uiManager.ConfigurarReferenciasUI(
                healthSlider, fillImg, healthTxt, coresTxt,
                objTxt, flashImg, notifTxt,
                victoryPanel, victoryStats, defeatPanel, pausePanel
            );

            return canvasObj;
        }

        private static GameObject CreateUIBox(Transform parent, string name, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return obj;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, Font font, Color color, Vector2 pos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.font = font;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false; // Desactivado para no bloquear clics a los botones

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return t;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Font font, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.32f, 0.48f);
            btnImg.raycastTarget = true;

            Button btn = btnObj.AddComponent<Button>();
            if (action != null) btn.onClick.AddListener(action);

            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.20f, 0.32f, 0.48f);
            cb.highlightedColor = new Color(0.32f, 0.50f, 0.72f);
            cb.pressedColor = new Color(0.12f, 0.20f, 0.32f);
            cb.selectedColor = new Color(0.25f, 0.38f, 0.56f);
            btn.colors = cb;

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Texto botón
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = label;
            t.fontSize = 16;
            t.font = font;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false; // Desactivado para que el botón reciba los eventos del mouse directamente
            StretchToFull(txtObj.GetComponent<RectTransform>());

            return btnObj;
        }

        private static void StretchToFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
