using System.Collections.Generic;
using UnityEngine;

// Sonidos del juego. No hay que ponerlo en ningun objeto: se crea solo.
// Los sonidos se cargan desde Assets/Resources/Sonidos/ por nombre (si hay "nombre_2", "nombre_3"... elige uno al azar):
//   ambiente        -> mar, viento y gaviotas de fondo (en bucle)
//   espada_aire     -> Kai blande el arma
//   espada_golpe    -> el arma le pega a un enemigo
//   punio_golpe     -> golpe a punos
//   bloqueo         -> Kai bloquea un golpe
//   kai_dolor       -> Kai recibe dano
//   enemigo_dolor   -> un enemigo recibe dano
// Para cambiar un sonido por uno mejor, reemplaza el archivo con el mismo nombre (wav, mp3 u ogg).
// El volumen general es el de Opciones.
public class Sonidos : MonoBehaviour
{
    public static float VolumenAmbiente = 0.45f;
    public static float VolumenEfectos = 0.9f;

    static Sonidos instancia;
    readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    readonly Dictionary<string, float> ultimaVez = new Dictionary<string, float>();
    AudioSource[] fuentes;
    AudioSource ambiente;
    int siguiente;

    static Sonidos I
    {
        get
        {
            if (!instancia)
            {
                var go = new GameObject("Sonidos");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<Sonidos>();
            }
            return instancia;
        }
    }

    void Awake()
    {
        fuentes = new AudioSource[10];
        for (int i = 0; i < fuentes.Length; i++)
        {
            var hijo = new GameObject("Fuente" + i);   // cada fuente en su propio objeto (para ponerla donde suena)
            hijo.transform.SetParent(transform, false);
            fuentes[i] = hijo.AddComponent<AudioSource>();
            fuentes[i].playOnAwake = false;
        }
    }

    // ------------------------------------------------------------ uso desde otros scripts
    // Sonidos.Tocar("espada_golpe", posicion)  (posicion opcional; sin posicion suena igual en los dos oidos)
    public static void Tocar(string nombre, Vector3? posicion = null, float volumen = 1f, float variacionTono = 0.08f, float minimoEntre = 0.05f)
    {
        var s = I;
        float ahora = Time.unscaledTime;
        if (s.ultimaVez.TryGetValue(nombre, out float t) && ahora - t < minimoEntre) return;   // que no se amontonen
        var clip = s.Clip(nombre);
        if (!clip) return;
        s.ultimaVez[nombre] = ahora;

        var f = s.fuentes[s.siguiente]; s.siguiente = (s.siguiente + 1) % s.fuentes.Length;
        f.Stop();
        f.clip = clip;
        f.volume = volumen * VolumenEfectos;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        if (posicion.HasValue)
        {
            // sonido en el mundo: se oye mas fuerte cerca (pero siempre se oye un poco)
            f.transform.position = posicion.Value;
            f.spatialBlend = 0.6f; f.minDistance = 4f; f.maxDistance = 40f; f.rolloffMode = AudioRolloffMode.Linear;
        }
        else f.spatialBlend = 0f;
        f.Play();
    }

    // Sonido de fondo en bucle (mar y viento). Lo llama el HUD al empezar cada escena.
    public static void Ambiente(string nombre = "ambiente")
    {
        var s = I;
        var clip = s.Clip(nombre);
        if (!clip) return;
        if (!s.ambiente) { s.ambiente = s.gameObject.AddComponent<AudioSource>(); s.ambiente.loop = true; s.ambiente.spatialBlend = 0f; s.ambiente.playOnAwake = false; }
        if (s.ambiente.clip == clip && s.ambiente.isPlaying) return;
        s.ambiente.clip = clip; s.ambiente.volume = 0f; s.ambiente.Play();
    }

    void Update()
    {
        // el ambiente sube suave al empezar
        if (ambiente && ambiente.isPlaying)
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, VolumenAmbiente, Time.unscaledDeltaTime * 0.3f);
    }

    AudioClip Clip(string nombre)
    {
        if (!clips.TryGetValue(nombre, out var lista))
        {
            var l = new List<AudioClip>();
            var c = Resources.Load<AudioClip>("Sonidos/" + nombre);
            if (c) l.Add(c);
            for (int i = 2; i <= 6; i++) { c = Resources.Load<AudioClip>("Sonidos/" + nombre + "_" + i); if (c) l.Add(c); }
            if (l.Count == 0) Debug.LogWarning("[Sonidos] Falta el sonido \"" + nombre + "\" en Assets/Resources/Sonidos/");
            lista = l.ToArray();
            clips[nombre] = lista;
        }
        return lista.Length == 0 ? null : lista[Random.Range(0, lista.Length)];
    }
}
