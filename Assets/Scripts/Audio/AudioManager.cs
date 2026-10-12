using UnityEngine;

namespace ChronosEscape.Audio
{
    /// <summary>
    /// Administrador de Audio centralizado para BGM y SFX.
    /// Incluye sintetizador procedural de audio en tiempo de ejecución para garantizar
    /// que todos los efectos de sonido y música ambiental funcionen sin dependencias externas.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Fuentes de Audio")]
        [SerializeField] private AudioSource fuenteMusica;
        [SerializeField] private AudioSource fuenteEfectos;

        [Header("Clips de Sonido")]
        public AudioClip audioPaso;
        public AudioClip audioRecolectar;
        public AudioClip audioAlertaEnemigo;
        public AudioClip audioDanioJugador;
        public AudioClip audioPortal;
        public AudioClip audioVictoria;
        public AudioClip audioDerrota;
        public AudioClip musicaAmbiente;

        [Header("Volumen")]
        [Range(0f, 1f)] public float volumenMusica = 0.20f; // Alarma ambiente atenuada para no tapar los pasos
        [Range(0f, 1f)] public float volumenEfectos = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ConfigurarFuentesAudio();
            AsegurarExistenciaDeClips();
        }

        private void Start()
        {
            ReproducirMusicaFondo();
        }

        private void ConfigurarFuentesAudio()
        {
            if (fuenteMusica == null)
            {
                GameObject objMusica = new GameObject("MusicSource");
                objMusica.transform.SetParent(transform);
                fuenteMusica = objMusica.AddComponent<AudioSource>();
                fuenteMusica.loop = true;
                fuenteMusica.playOnAwake = false;
            }

            if (fuenteEfectos == null)
            {
                GameObject objEfectos = new GameObject("SFXSource");
                objEfectos.transform.SetParent(transform);
                fuenteEfectos = objEfectos.AddComponent<AudioSource>();
                fuenteEfectos.loop = false;
                fuenteEfectos.playOnAwake = false;
            }

            fuenteMusica.volume = volumenMusica;
            fuenteEfectos.volume = volumenEfectos;
        }

        private void AsegurarExistenciaDeClips()
        {
#if UNITY_EDITOR
            if (audioPaso == null) audioPaso = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/pasos.wav");
            if (audioDanioJugador == null) audioDanioJugador = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/golpe_npc.wav");
            if (musicaAmbiente == null) musicaAmbiente = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/alarm.mp3");
#endif

            if (audioPaso == null) audioPaso = AudioSynthesizer.CreateFootstepClip();
            if (audioRecolectar == null) audioRecolectar = AudioSynthesizer.CreatePickupClip();
            if (audioAlertaEnemigo == null) audioAlertaEnemigo = AudioSynthesizer.CreateAlertClip();
            if (audioDanioJugador == null) audioDanioJugador = AudioSynthesizer.CreateHurtClip();
            if (audioPortal == null) audioPortal = AudioSynthesizer.CreatePortalClip();
            if (audioVictoria == null) audioVictoria = AudioSynthesizer.CreateVictoryClip();
            if (audioDerrota == null) audioDerrota = AudioSynthesizer.CreateGameOverClip();
            if (musicaAmbiente == null) musicaAmbiente = AudioSynthesizer.CreateAmbientMusicClip();
        }

        public void ReproducirMusicaFondo()
        {
            if (fuenteMusica != null && musicaAmbiente != null)
            {
                fuenteMusica.clip = musicaAmbiente;
                fuenteMusica.volume = volumenMusica;
                fuenteMusica.Play();
            }
        }

        public void DetenerMusica()
        {
            if (fuenteMusica != null) fuenteMusica.Stop();
        }

        public void SilenciarAlarma() => DetenerMusica();

        public void ReproducirEfecto(AudioClip clip, float escalaVolumen = 1.0f)
        {
            if (fuenteEfectos != null && clip != null)
            {
                fuenteEfectos.PlayOneShot(clip, volumenEfectos * escalaVolumen);
            }
        }

        public void ReproducirPaso() => ReproducirEfecto(audioPaso, 1.6f);
        public void ReproducirRecolectar() => ReproducirEfecto(audioRecolectar, 0.9f);
        public void ReproducirAlertaEnemigo() => ReproducirEfecto(audioAlertaEnemigo, 1.0f);
        public void ReproducirDanioJugador() => ReproducirEfecto(audioDanioJugador, 1.0f);
        public void ReproducirPortal() => ReproducirEfecto(audioPortal, 1.0f);
        public void ReproducirVictoria()
        {
            DetenerMusica();
            ReproducirEfecto(audioVictoria, 1.0f);
        }
        public void ReproducirDerrota()
        {
            DetenerMusica();
            ReproducirEfecto(audioDerrota, 1.0f);
        }

        // Aliases en inglés para compatibilidad total con tutoriales
        public void PlayBGM() => ReproducirMusicaFondo();
        public void StopBGM() => DetenerMusica();
        public void PlaySFX(AudioClip clip, float scale = 1.0f) => ReproducirEfecto(clip, scale);
        public void PlayFootstep() => ReproducirPaso();
        public void PlayPickup() => ReproducirRecolectar();
        public void PlayEnemyAlert() => ReproducirAlertaEnemigo();
        public void PlayPlayerHurt() => ReproducirDanioJugador();
        public void PlayPortalOpen() => ReproducirPortal();
        public void PlayVictory() => ReproducirVictoria();
        public void PlayGameOver() => ReproducirDerrota();
    }

    /// <summary>
    /// Generador procedural de audio PCM en memoria para efectos sonoros de calidad.
    /// </summary>
    public static class AudioSynthesizer
    {
        private const int SampleRate = 44100;

        public static AudioClip CreateFootstepClip()
        {
            int samples = (int)(SampleRate * 0.15f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = 1f - ((float)i / samples);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float tone = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.6f;
                data[i] = (noise + tone) * env * env;
            }
            AudioClip clip = AudioClip.Create("FootstepProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreatePickupClip()
        {
            int samples = (int)(SampleRate * 0.45f);
            float[] data = new float[samples];
            // Arpegio ascendente de tres notas (E5, G#5, B5, E6)
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / samples;
                float freq = progress < 0.25f ? 659.25f :
                             progress < 0.50f ? 830.61f :
                             progress < 0.75f ? 987.77f : 1318.51f;

                float envelope = Mathf.Exp(- progress * 5f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f +
                             Mathf.Sin(4f * Mathf.PI * freq * t) * 0.3f;
                data[i] = wave * envelope * 0.5f;
            }
            AudioClip clip = AudioClip.Create("PickupProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateAlertClip()
        {
            int samples = (int)(SampleRate * 0.5f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.PingPong(t * 1200f, 400f) + 500f; // Sirena modulada
                float env = 1f - Mathf.Pow((float)i / samples, 2);
                float saw = (2f * (t * freq - Mathf.Floor(t * freq + 0.5f)));
                data[i] = saw * env * 0.4f;
            }
            AudioClip clip = AudioClip.Create("AlertProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateHurtClip()
        {
            int samples = (int)(SampleRate * 0.35f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(- ((float)i / samples) * 7f);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                float punch = Mathf.Sin(2f * Mathf.PI * 75f * t) * 0.8f;
                data[i] = (noise + punch) * env * 0.6f;
            }
            AudioClip clip = AudioClip.Create("HurtProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreatePortalClip()
        {
            int samples = (int)(SampleRate * 1.5f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / samples;
                float freq = Mathf.Lerp(150f, 600f, progress);
                float lfo = 1f + 0.3f * Mathf.Sin(2f * Mathf.PI * 8f * t);
                float env = Mathf.Sin(progress * Mathf.PI);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * lfo;
                data[i] = wave * env * 0.5f;
            }
            AudioClip clip = AudioClip.Create("PortalProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateVictoryClip()
        {
            int samples = (int)(SampleRate * 2.2f);
            float[] data = new float[samples];
            // Acorde triunfal ascendente
            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float progress = (float)i / samples;
                float wave = 0f;
                for (int note = 0; note < freqs.Length; note++)
                {
                    float noteStart = note * 0.22f;
                    if (t >= noteStart)
                    {
                        float noteT = t - noteStart;
                        float noteEnv = Mathf.Clamp01(1f - (noteT / 1.5f));
                        wave += Mathf.Sin(2f * Mathf.PI * freqs[note] * noteT) * noteEnv * 0.2f;
                    }
                }
                data[i] = Mathf.Clamp(wave, -0.9f, 0.9f);
            }
            AudioClip clip = AudioClip.Create("VictoryProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateGameOverClip()
        {
            int samples = (int)(SampleRate * 1.8f);
            float[] data = new float[samples];
            float[] freqs = new float[] { 440f, 370f, 311f, 220f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int noteIndex = Mathf.Clamp((int)(t / 0.4f), 0, freqs.Length - 1);
                float freq = freqs[noteIndex];
                float env = 1f - ((float)i / samples);
                float saw = (2f * (t * freq - Mathf.Floor(t * freq + 0.5f)));
                data[i] = saw * env * 0.35f;
            }
            AudioClip clip = AudioClip.Create("GameOverProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateAmbientMusicClip()
        {
            // Bucle ambiental relajante y misterioso de 6 segundos
            float duration = 6.0f;
            int samples = (int)(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                // Pad con dos osciladores armónicos y modulador de fase suave
                float drone1 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f; // A2
                float drone2 = Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.20f; // E3
                float sub = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.20f; // A1
                float shimmer = Mathf.Sin(2f * Mathf.PI * 440f * t) * 0.05f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.5f * t));

                // Suave envoltura en extremos para loop sin clics
                float fade = Mathf.Clamp01(i / 1000f) * Mathf.Clamp01((samples - 1 - i) / 1000f);
                data[i] = (drone1 + drone2 + sub + shimmer) * fade * 0.35f;
            }

            AudioClip clip = AudioClip.Create("AmbientMusicProcedural", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
