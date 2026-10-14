using System.Collections.Generic;
using UnityEngine;

// Marca el area de una isla (mapa local, deteccion de "en que isla estoy" y activacion en el mapa del mundo).
// Ponlo en un objeto vacio en el centro de la isla y ajusta el tamano (el cuadro amarillo en la escena).
// Una isla aparece como disponible en el mapa del archipielago en cuanto existe su ZonaIsla.
public class ZonaIsla : MonoBehaviour
{
    [Tooltip("Id de la isla (1 Caleta Gris, 2 Faro Roto, 3 Astillero, 4 Bahia de la Corona, 5 Fuerte de Roca, 6 Fortaleza de Voss)")]
    public int idIsla = 1;
    [Tooltip("Ancho (X) y largo (Z) del area que se ve en el mapa")]
    public Vector2 tamano = new Vector2(80f, 40f);

    [Header("Objetivo en el HUD mientras Kai esta en esta isla")]
    [TextArea(2, 4)] public string objetivo = "";
    [Tooltip("Cuando esta recompensa se da, la isla cuenta como completada")]
    public RecompensaIsla recompensaPrincipal;

    public Rect Area => new Rect(transform.position.x - tamano.x / 2f, transform.position.z - tamano.y / 2f, tamano.x, tamano.y);
    public bool Completada => recompensaPrincipal && recompensaPrincipal.Hecha;

    static readonly List<ZonaIsla> todas = new List<ZonaIsla>();
    public static IReadOnlyList<ZonaIsla> Todas => todas;
    public static bool Existe(int id) { foreach (var z in todas) if (z && z.idIsla == id) return true; return false; }
    public static ZonaIsla De(int id) { foreach (var z in todas) if (z && z.idIsla == id) return z; return null; }

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); }
    void OnDisable() { todas.Remove(this); }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(tamano.x, 2f, tamano.y));
    }
}
