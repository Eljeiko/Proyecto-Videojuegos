using UnityEngine;

// Kai en 3D: WASD/flechas moverse, Shift correr, Espacio saltar, C agacharse (junto a barril = oculto),
// J o clic izquierdo atacar, K o clic derecho bloquear.
// 1/2/3 cambiar arma / pocion / objeto, Q usar pocion, R usar objeto (ver Equipo.cs).
// El objeto raiz esta a la altura del CENTRO del personaje (Y = 1 sobre el suelo).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float velocidad = 6f;
    public float velocidadCorrer = 9f;
    public float esperaAtaque = 0.5f;
    public float tiempoMuerte = 2.5f;
    [Tooltip("Si esta desmarcado, al agacharse (C) Kai se queda quieto")]
    public bool puedeMoverseAgachado = false;
    public float fuerzaSalto = 8f;
    public float gravedad = -20f;

    [Header("Aguante (estamina)")]
    public float gastoCorrer = 22f;       // por segundo
    public float gastoSalto = 12f;
    public float recuperacion = 28f;      // por segundo
    public float esperaRecuperar = 0.7f;  // segundos sin gastar antes de recuperar
    public Transform spawnInicio;
    public Transform spawnFaro;

    [HideInInspector] public bool oculto, bloqueando, agachado;
    public bool EstaMuerto { get; private set; }

    CharacterController cc;
    float vy;
    int estadoVisual = -1;
    Transform ultimoSpawn;
    AnimacionSimple anim;
    float proximoAtaque, ultimoGasto, humoHasta;
    HabilidadesKai hab;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<AnimacionSimple>();
        GameState.Kai = transform;
        hab = GetComponent<HabilidadesKai>();
        if (!hab) hab = gameObject.AddComponent<HabilidadesKai>();   // barra de habilidades 1-4

        Reaparecer(PuntoInicial());
        if (GameState.IslaActual != 1)
            GameState.Avisar("Llegaste a " + (Archipielago.Buscar(GameState.IslaActual)?.nombre ?? "una isla nueva"), 4f);
        GameState.LlegandoEnBarco = false;
        if (!GameState.IslaPisada(GameState.IslaActual))   // primera vez en esta isla: experiencia
        {
            GameState.MarcarPisada(GameState.IslaActual);
            if (GameState.IslaActual != 1) Progreso.Ganar(Progreso.XpIslaNueva);
        }
    }

    void Update()
    {
        if (EstaMuerto || HUD.Pausado) return;
        if (GameState.VidaKai <= 0) { StartCoroutine(Morir()); return; }

        float h = Input.GetAxisRaw("Horizontal");
        float vIn = Input.GetAxisRaw("Vertical");
        Vector3 entrada = new Vector3(h, 0f, vIn);

        // Movimiento relativo a la camara (W = hacia donde mira la camara)
        Camera cam = Camera.main;
        if (cam)
        {
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            entrada = f * vIn + r * h;
        }
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();

        agachado = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        bloqueando = Input.GetKey(KeyCode.K) || Input.GetMouseButton(1);
        oculto = (agachado && CercaDeBarril()) || Time.time < humoHasta;
        if (anim) anim.Agachado = agachado;

        bool corriendo = Input.GetKey(KeyCode.LeftShift) && !agachado && GameState.Aguante > 1f && entrada.sqrMagnitude > 0.01f;
        if (corriendo) Gastar(gastoCorrer * Time.deltaTime);
        float v = bloqueando ? 0f
                : agachado ? (puedeMoverseAgachado ? velocidad * 0.4f : 0f)
                : (corriendo ? velocidadCorrer : velocidad) * Tripulacion.BonoPie;   // Mara invocada: +15%

        bool enHabilidad = hab && hab.Ocupado;   // estocada / abordaje: la habilidad mueve a Kai
        if (enHabilidad) { v = 0f; entrada = Vector3.zero; }

        var camGTA = CamaraGTA.Actual;
        if (camGTA && camGTA.EnPrimeraPersona)
            transform.rotation = Quaternion.Euler(0f, camGTA.Yaw, 0f);   // en primera persona mira con la camara
        else if (entrada.sqrMagnitude > 0.01f && v > 0f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(entrada), 15f * Time.deltaTime);

        if (cc.isGrounded)
        {
            if (vy < 0f) vy = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !agachado && GameState.Aguante >= gastoSalto)
            {
                vy = fuerzaSalto;
                Gastar(gastoSalto);
            }
        }
        vy += gravedad * Time.deltaTime;

        Vector3 mov = entrada * v;
        mov.y = vy;
        cc.Move(mov * Time.deltaTime);

        if (!enHabilidad && (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))) Atacar();

        // Equipo rapido (1-4 son ahora las habilidades del arma)
        if (Input.GetKeyDown(KeyCode.Z)) Equipo.SiguienteArma();
        if (Input.GetKeyDown(KeyCode.X)) Equipo.Siguiente(ref Equipo.pocion, Equipo.Pociones.Length);
        if (Input.GetKeyDown(KeyCode.T)) Equipo.Siguiente(ref Equipo.objeto, Equipo.Objetos.Length);
        if (Input.GetKeyDown(KeyCode.Q)) UsarPocion();
        if (Input.GetKeyDown(KeyCode.R)) UsarObjeto();

        // Recuperar aguante
        if (Time.time - ultimoGasto > esperaRecuperar)
            GameState.Aguante = Mathf.Min(100f, GameState.Aguante + recuperacion * (bloqueando ? 0.35f : 1f) * Time.deltaTime);

        // M = abrir el mapa vivo en cualquier momento
        if (Input.GetKeyDown(KeyCode.M)) MapaMundo.Abrir(MapaMundo.Pestana.Isla);

        if (transform.position.y < -5f && ultimoSpawn) Reaparecer(ultimoSpawn);

        ActualizarColor();
    }

    void ActualizarColor()
    {
        int estado = oculto ? 1 : bloqueando ? 2 : 0;
        if (estado == estadoVisual) return;
        estadoVisual = estado;
        if (estado == 1) Colorear.Poner(gameObject, new Color(0.15f, 0.15f, 0.25f));
        else if (estado == 2) Colorear.Poner(gameObject, new Color(0.6f, 0.6f, 1f));
        else Colorear.Restaurar(gameObject);
    }

    bool CercaDeBarril()
    {
        foreach (var col in Physics.OverlapSphere(transform.position, 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (col.GetComponentInParent<Barril>()) return true;
        return false;
    }

    // Gasta aguante y reinicia la espera de recuperacion
    public void Gastar(float cantidad)
    {
        GameState.Aguante = Mathf.Max(0f, GameState.Aguante - cantidad);
        ultimoGasto = Time.time;
    }

    void Atacar()
    {
        var a = Equipo.ArmaActual;
        if (Time.time < proximoAtaque) return;
        if (GameState.Aguante < a.costoAguante * 0.5f) { GameState.Avisar("¡Sin aliento!", 0.8f); return; }
        proximoAtaque = Time.time + a.espera;
        Gastar(a.costoAguante);
        if (anim) anim.Atacar();
        if (a.aDistancia) { if (hab) hab.DisparoBasico(a); return; }   // pistola: dispara en vez de golpear
        Sonidos.Tocar("espada_aire", transform.position, Equipo.arma == (int)Equipo.IdArma.Punos ? 0.45f : 0.75f);

        // golpea al enemigo mas cercano que este delante (no depende de que el collider este bien puesto)
        Rival mejor = null; float mejorD = float.MaxValue;
        foreach (var r in Rival.Todos)
        {
            if (!r || r.Muerto || !r.isActiveAndEnabled) continue;
            Vector3 d = r.transform.position - transform.position; d.y = 0f;
            float borde = d.magnitude - r.Radio;                       // distancia hasta el borde del enemigo
            if (borde > a.alcance + 0.6f) continue;
            if (d.magnitude > 0.5f && Vector3.Angle(transform.forward, d) > 75f) continue;   // tiene que estar al frente
            if (Mathf.Abs(r.transform.position.y - transform.position.y) > 3f) continue;
            if (borde < mejorD) { mejorD = borde; mejor = r; }
        }
        if (mejor)
        {
            int dano = Equipo.TirarDano(out bool critico);
            mejor.RecibirGolpe(dano, critico);
        }
    }

    void UsarPocion()
    {
        var p = Equipo.PocionActual;
        if (p.cantidad <= 0) { GameState.Avisar("No te queda " + p.nombre, 1.2f); return; }
        p.cantidad--;
        if (p.efecto == "vida")
        {
            int antes = GameState.VidaKai;
            GameState.VidaKai = Mathf.Min(Progreso.VidaMax, GameState.VidaKai + p.valor);
            HUD.Numero(transform.position + Vector3.up * 1.4f, "+" + (GameState.VidaKai - antes), new Color(0.4f, 1f, 0.45f), 1f);
        }
        else if (p.efecto == "aguante")
        {
            GameState.Aguante = 100f;
            HUD.Numero(transform.position + Vector3.up * 1.4f, "¡Estamina!", new Color(0.55f, 0.9f, 0.4f), 0.9f);
        }
        GameState.Avisar(p.nombre + " (" + p.cantidad + " restantes)", 1.5f);
    }

    void UsarObjeto()
    {
        var o = Equipo.ObjetoActual;
        if (o.efecto == "nada") { GameState.Avisar("Tienes " + Equipo.Doblones + " doblones.", 1.5f); return; }
        if (o.cantidad <= 0) { GameState.Avisar("No te queda " + o.nombre, 1.2f); return; }
        o.cantidad--;
        if (o.efecto == "humo")
        {
            humoHasta = Time.time + o.valor;
            GameState.Avisar("¡Bomba de humo! Nadie te ve durante " + o.valor + " segundos.", 2f);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.5f) return;
        var viga = hit.collider.GetComponent<VigaInestable>();
        if (viga) viga.Pisar();
    }

    System.Collections.IEnumerator Morir()
    {
        EstaMuerto = true;
        oculto = bloqueando = agachado = false;
        if (anim) anim.Agachado = false;
        Colorear.Restaurar(gameObject);
        estadoVisual = 0;
        GameState.Avisar("¡Kai cayó! Volviendo al inicio...", tiempoMuerte);

        float duracion = anim ? anim.Morir() : 0f;
        yield return new WaitForSeconds(Mathf.Max(tiempoMuerte, duracion + 0.5f));

        GameState.VidaKai = Progreso.VidaMax;
        GameState.Aguante = 100f;
        Reaparecer(PuntoInicial() ?? ultimoSpawn);   // revive en la isla donde esta, no en Caleta Gris
        if (anim) anim.Revivir();
        EstaMuerto = false;
    }

    // Donde empieza Kai en la isla actual:
    //  Caleta Gris (al empezar o al morir) -> spawnInicio
    //  cualquier otra isla, o al volver en barco -> el muelle de esa isla (Muelle con idIsla)
    Transform PuntoInicial()
    {
        int id = GameState.IslaActual;
        if (id != 1 || GameState.LlegandoEnBarco)
        {
            var mu = Muelle.De(id);
            if (mu) return mu.PuntoLlegada;
            if (id == 2 && spawnFaro) return spawnFaro;   // compatibilidad con la escena vieja
        }
        return spawnInicio;
    }

    public void Reaparecer(Transform punto)
    {
        if (punto == null) return;
        ultimoSpawn = punto;
        if (!cc) cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = punto.position;
        cc.enabled = true;
        vy = 0f;
    }
}
