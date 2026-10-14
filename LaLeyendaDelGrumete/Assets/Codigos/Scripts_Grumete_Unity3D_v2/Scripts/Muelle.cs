using UnityEngine;

// Muelle de una isla (mundo abierto: todo en la misma escena).
//  - Es el punto donde aparece Kai al bajarse del barco cerca de esta isla (Punto Llegada).
//  - Si le asignas el Barco: el barco esta escondido hasta que Kai tenga los fragmentos necesarios
//    (en Caleta Gris: al recibir el primer nudo de Barbagris). Entonces aparece amarrado en "Punto Barco".
//  - Si no le asignas barco, es solo un muelle de llegada.
public class Muelle : MonoBehaviour
{
    public float radio = 3f;
    [Tooltip("Isla a la que pertenece este muelle (1 Caleta Gris, 2 Faro Roto, 3 Astillero...)")]
    public int idIsla = 1;
    [Tooltip("Donde aparece Kai al desembarcar en esta isla (vacio = en este mismo punto)")]
    public Transform puntoLlegada;

    [Header("Barco (solo en el muelle donde empieza el barco)")]
    public BarcoController barco;
    [Tooltip("Donde queda amarrado el barco al aparecer (vacio = donde esta el barco en la escena)")]
    public Transform puntoBarco;
    [Tooltip("Fragmentos de mapa necesarios para que aparezca el barco (1 = al terminar las pruebas de Barbagris)")]
    public int fragmentosParaBarco = 1;

    public static Muelle[] Todos = new Muelle[0];
    public Transform PuntoLlegada => puntoLlegada ? puntoLlegada : transform;
    public bool BarcoListo => barco && GameState.Fragmentos >= fragmentosParaBarco;

    bool aparecio;

    public static Muelle De(int id)
    {
        foreach (var m in Todos) if (m && m.idIsla == id) return m;
        return null;
    }

    void OnEnable() { Todos = FindObjectsByType<Muelle>(FindObjectsSortMode.None); }

    void Start()
    {
        MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Puerto, false, "", "Muelle");
        if (!barco) return;
        if (BarcoListo) { aparecio = true; Amarrar(); }   // partida cargada: el barco ya estaba
        else barco.gameObject.SetActive(false);
    }

    void Amarrar()
    {
        if (puntoBarco) barco.transform.SetPositionAndRotation(puntoBarco.position, puntoBarco.rotation);
        barco.gameObject.SetActive(true);
        MarcadorMinimapa.Agregar(barco.gameObject, MarcadorMinimapa.Tipo.Puerto, false, "B", "Tu barco");
    }

    void Update()
    {
        if (!barco || aparecio || !BarcoListo) return;
        aparecio = true;
        Amarrar();
        Efectos.Anillo(barco.transform.position, 6f, new Color(1f, 0.85f, 0.4f), 0.8f);
        GameState.Hablar("Barbagrís", "Tu barco te espera en el muelle, grumete. Sube con E y elige tu rumbo en el mapa (M).", 4f);
    }

    public bool Cerca(Vector3 p) => Vector3.Distance(p, transform.position) < radio;

    // Texto del aviso cuando Kai esta cerca ("" = no mostrar nada)
    public string Prompt()
    {
        if (barco && !BarcoListo) return "Muelle: el barco llegará al terminar las pruebas";
        return "";   // con el barco listo, el aviso lo da el propio barco ("Subir al barco")
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radio);
        var p = PuntoLlegada.position;   // flecha verde: donde aparece Kai y hacia donde mira
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(p, 0.4f);
        Gizmos.DrawLine(p, p + PuntoLlegada.forward * 1.5f);
        if (puntoBarco) { Gizmos.color = new Color(1f, 0.6f, 0.2f); Gizmos.DrawWireCube(puntoBarco.position, new Vector3(3f, 1f, 7f)); }
    }
}
