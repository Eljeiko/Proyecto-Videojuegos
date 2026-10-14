using System.Collections;
using UnityEngine;

// Efectos visuales sencillos hechos por codigo (no necesitan assets):
//   Efectos.Anillo(pos, radio, color)            anillo que se expande en el suelo (habilidades de area)
//   Efectos.Cono(pos, adelante, radio, color)     abanico frente a Kai (golpes de frente)
//   Efectos.Linea(desde, hasta, color)            cuerda del garfio / rastro de una estocada
//   Efectos.Sacudir(fuerza, duracion)             sacude la camara
// Mas adelante (Fase 7) se pueden cambiar por particulas.
public class Efectos : MonoBehaviour
{
    static Material material;

    static Material Mat()
    {
        if (material) return material;
        var sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(sh);
        return material;
    }

    static LineRenderer NuevaLinea(string nombre, int puntos, float ancho, Color color)
    {
        var go = new GameObject(nombre);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = Mat();
        lr.positionCount = puntos; lr.useWorldSpace = true;
        lr.widthMultiplier = ancho; lr.startColor = lr.endColor = color;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    public static void Anillo(Vector3 pos, float radio, Color color, float duracion = 0.45f)
    {
        var lr = NuevaLinea("EfectoAnillo", 49, 0.16f, color);
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAnillo(lr, pos + Vector3.up * 0.08f, radio, color, duracion));
    }

    static IEnumerator AnimarAnillo(LineRenderer lr, Vector3 c, float radio, Color color, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = Mathf.Lerp(radio * 0.25f, radio, 1f - (1f - k) * (1f - k));
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            var col = color; col.a = 1f - k; lr.startColor = lr.endColor = col;
            lr.widthMultiplier = Mathf.Lerp(0.3f, 0.06f, k);
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    public static void Cono(Vector3 pos, Vector3 adelante, float radio, Color color, float angulo = 120f, float duracion = 0.35f)
    {
        var lr = NuevaLinea("EfectoCono", 22, 0.14f, color);
        adelante.y = 0f; adelante.Normalize();
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarCono(lr, pos + Vector3.up * 0.4f, adelante, radio, angulo, color, duracion));
    }

    static IEnumerator AnimarCono(LineRenderer lr, Vector3 c, Vector3 f, float radio, float angulo, Color color, float dur)
    {
        float baseAng = Mathf.Atan2(f.z, f.x);
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = Mathf.Lerp(radio * 0.4f, radio, k);
            lr.SetPosition(0, c);
            for (int i = 1; i < 21; i++)
            {
                float a = baseAng + Mathf.Lerp(-angulo / 2f, angulo / 2f, (i - 1) / 19f) * Mathf.Deg2Rad;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            lr.SetPosition(21, c);
            var col = color; col.a = 1f - k; lr.startColor = lr.endColor = col;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    public static void Linea(Vector3 desde, Vector3 hasta, Color color, float duracion = 0.3f)
    {
        var lr = NuevaLinea("EfectoLinea", 2, 0.08f, color);
        lr.SetPosition(0, desde); lr.SetPosition(1, hasta);
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(Desvanecer(lr, color, duracion));
    }

    static IEnumerator Desvanecer(LineRenderer lr, Color color, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            var col = color; col.a = 1f - t / dur; lr.startColor = lr.endColor = col;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ---------------- avisos en el suelo (ataques de jefes, estilo Albion): una zona roja que se llena
    //   cuando se llena del todo, el golpe cae ahi. Da tiempo de salir corriendo.
    public static void AvisoCirculo(Vector3 pos, float radio, float duracion, Color color)
    {
        var borde = NuevaLinea("AvisoBorde", 49, 0.12f, color);
        var relleno = NuevaLinea("AvisoRelleno", 49, 0.1f, new Color(color.r, color.g, color.b, 0.35f));
        borde.loop = relleno.loop = true;
        borde.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAvisoCirculo(borde, relleno, pos + Vector3.up * 0.1f, radio, duracion, color));
    }

    static IEnumerator AnimarAvisoCirculo(LineRenderer borde, LineRenderer relleno, Vector3 c, float radio, float dur, Color color)
    {
        for (int i = 0; i < 49; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            borde.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio);
        }
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = radio * k;
            // un anillo muy grueso centrado en r/2 parece un disco que crece
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                relleno.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * 0.5f));
            }
            relleno.widthMultiplier = Mathf.Max(0.05f, r);
            float parpadeo = k > 0.75f ? (Mathf.Sin(Time.time * 40f) * 0.5f + 0.5f) : 1f;
            borde.startColor = borde.endColor = new Color(color.r, color.g, color.b, 0.9f * parpadeo);
            yield return null;
        }
        Destroy(relleno.gameObject);
        Destroy(borde.gameObject);
    }

    // franja roja en linea recta (embestida de un jefe)
    public static void AvisoLinea(Vector3 desde, Vector3 hasta, float ancho, float duracion, Color color)
    {
        desde.y += 0.1f; hasta.y += 0.1f;
        var fondo = NuevaLinea("AvisoLinea", 2, ancho, new Color(color.r, color.g, color.b, 0.25f));
        var relleno = NuevaLinea("AvisoLineaRelleno", 2, ancho, new Color(color.r, color.g, color.b, 0.55f));
        fondo.numCapVertices = relleno.numCapVertices = 0;
        fondo.SetPosition(0, desde); fondo.SetPosition(1, hasta);
        fondo.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAvisoLinea(fondo, relleno, desde, hasta, duracion));
    }

    static IEnumerator AnimarAvisoLinea(LineRenderer fondo, LineRenderer relleno, Vector3 desde, Vector3 hasta, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            relleno.SetPosition(0, desde); relleno.SetPosition(1, Vector3.Lerp(desde, hasta, t / dur));
            yield return null;
        }
        Destroy(relleno.gameObject);
        Destroy(fondo.gameObject);
    }

    // ---------------- sacudida de camara (la aplica CamaraGTA en su LateUpdate)
    public static float SacudidaHasta, SacudidaFuerza;
    const float SacudidaMaxima = 1f;   // ninguna sacudida dura mas de 1 segundo

    // Al dar Play se borra la sacudida anterior (si Unity no recarga los scripts, el valor viejo
    // quedaba guardado y la camara temblaba al iniciar).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { SacudidaHasta = 0f; SacudidaFuerza = 0f; material = null; }

    public static void Sacudir(float fuerza = 0.15f, float duracion = 0.2f)
    {
        duracion = Mathf.Min(duracion, SacudidaMaxima);
        SacudidaFuerza = Mathf.Max(fuerza, Time.time < SacudidaHasta ? SacudidaFuerza : 0f);
        SacudidaHasta = Time.time + duracion;
    }

    public static Vector3 OffsetSacudida()
    {
        float falta = SacudidaHasta - Time.time;
        if (falta <= 0f || falta > SacudidaMaxima || HUD.Pausado) return Vector3.zero;   // valor imposible: se ignora
        return Random.insideUnitSphere * SacudidaFuerza * Mathf.Clamp01(falta / 0.15f);   // se apaga suave al final
    }
}
