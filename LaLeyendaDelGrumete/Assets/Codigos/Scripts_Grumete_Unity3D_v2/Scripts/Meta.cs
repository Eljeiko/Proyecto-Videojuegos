using UnityEngine;

// Collider con Is Trigger. tipo = "agilidad" o "sigilo" (no importan mayusculas ni espacios)
public class Meta : MonoBehaviour
{
    public string tipo = "agilidad";

    void Start()
    {
        // si la prueba ya estaba hecha (al volver a la isla), se muestra completada y sin icono
        if (Hecha()) Marcar();
    }

    public bool Hecha()
    {
        string t = tipo.Trim().ToLower();
        return (t == "agilidad" && GameState.Agilidad == 1) || (t == "sigilo" && GameState.Sigilo == 1);
    }

    void Marcar()
    {
        Colorear.Poner(gameObject, new Color(0.4f, 1f, 0.4f));
        var m = GetComponent<MarcadorMinimapa>(); if (m) m.Activo = false;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (!otro.GetComponentInParent<PlayerController>()) return;
        GameState.CompletarPrueba(tipo.Trim().ToLower());
        Marcar();
    }
}
