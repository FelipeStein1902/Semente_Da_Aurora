using UnityEngine;

/// <summary>
/// Controla o personagem Bran: andar, correr, pular, girar para a direção do
/// movimento e detectar o chão. O andar usa transform.Translate e o pulo usa
/// Rigidbody.AddForce no modo Impulse, como pedido no GDD.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade andando, em m/s.")]
    [SerializeField] private float velocidadeAndar = 5f;

    [Tooltip("Velocidade correndo (segurando Shift), em m/s.")]
    [SerializeField] private float velocidadeCorrer = 7f;

    [Tooltip("Velocidade com que o personagem gira para a direção do movimento.")]
    [SerializeField] private float velocidadeRotacao = 12f;

    [Tooltip("Fração da velocidade de andar que o jogador consegue corrigir no ar (0.5 = 50%).")]
    [Range(0f, 1f)]
    [SerializeField] private float controleNoAr = 0.7f;

    [Header("Pulo")]
    [Tooltip("Força do impulso do pulo. É fixa para o alcance ser previsível.")]
    [SerializeField] private float forcaPulo = 6.5f;

    [Tooltip("Multiplicador da gravidade durante a queda (deixa o pulo mais rápido e preciso).")]
    [SerializeField] private float multiplicadorQueda = 2f;

    [Header("Detecção de chão")]
    [Tooltip("Distância extra abaixo dos pés usada para detectar o chão.")]
    [SerializeField] private float distanciaChao = 0.15f;

    [Tooltip("Inclinação máxima (em graus) de uma superfície considerada chão.")]
    [SerializeField] private float inclinacaoMaxima = 50f;

    [Header("Efeitos de pouso")]
    [Tooltip("Partículas de poeira e pedrinhas tocadas ao aterrissar.")]
    [SerializeField] private ParticleSystem particulasPouso;

    [Tooltip("Velocidade vertical mínima (m/s) para considerar um pouso e tocar som/partículas.")]
    [SerializeField] private float velocidadeMinimaPouso = 2f;

    [Header("Referências")]
    [Tooltip("Câmera usada como referência para as direções de movimento. Se vazio, usa a Main Camera.")]
    [SerializeField] private Transform cameraReferencia;

    private Rigidbody corpo;
    private CapsuleCollider capsula;
    private PlayerAnimacao animacao;

    private Vector3 entrada;          // direção pedida pelo jogador (relativa à câmera)
    private bool correndo;
    private bool pedidoPulo;
    private bool noChao;
    private Vector3 velocidadeHorizontal;
    private Vector3 velocidadeNoPulo; // velocidade horizontal no instante do pulo

    /// <summary>Verdadeiro enquanto o personagem está sobre uma superfície.</summary>
    public bool NoChao => noChao;

    /// <summary>
    /// Pega os componentes e configura o Rigidbody e o material sem atrito.
    /// </summary>
    private void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        capsula = GetComponent<CapsuleCollider>();
        animacao = GetComponent<PlayerAnimacao>();

        // A rotação é feita por código; a física não pode tombar o personagem
        corpo.freezeRotation = true;
        corpo.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Material com atrito zero para não "grudar" nas paredes das plataformas
        capsula.material = new PhysicsMaterial("SemAtrito")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = 0f,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        if (cameraReferencia == null && Camera.main != null)
        {
            cameraReferencia = Camera.main.transform;
        }
    }

    /// <summary>
    /// Lê o teclado, move e gira o personagem e atualiza a animação.
    /// </summary>
    private void Update()
    {
        // Com o jogo pausado nada se move
        if (Time.timeScale == 0f)
        {
            return;
        }

        LerEntrada();
        noChao = EstaNoChao();

        // Pulo só é permitido no chão
        if (pedidoPulo && noChao)
        {
            Pular();
        }
        pedidoPulo = false;

        float velocidade = correndo ? velocidadeCorrer : velocidadeAndar;
        Mover(entrada, velocidade);

        if (animacao != null)
        {
            animacao.AtualizarAnimacao(velocidadeHorizontal.magnitude, noChao, corpo.linearVelocity.y);
        }
    }

    /// <summary>
    /// Remove o deslizamento horizontal e aplica a gravidade extra durante a queda.
    /// </summary>
    private void FixedUpdate()
    {
        // O movimento horizontal é todo feito pelo Translate; sem atrito, qualquer
        // velocidade horizontal vinda de colisões faria o personagem deslizar
        Vector3 v = corpo.linearVelocity;
        corpo.linearVelocity = new Vector3(0f, v.y, 0f);

        // Caindo: soma gravidade extra (multiplicador - 1, pois a normal já é aplicada)
        if (corpo.linearVelocity.y < 0f)
        {
            corpo.AddForce(Physics.gravity * (multiplicadorQueda - 1f), ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Lê WASD, Shift e Espaço e converte a direção para o referencial da câmera.
    /// </summary>
    private void LerEntrada()
    {
        // Eixos do Input Manager: Horizontal = A/D, Vertical = W/S
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Frente e direita da câmera, achatadas no plano do chão
        Vector3 frente = Vector3.forward;
        Vector3 direita = Vector3.right;
        if (cameraReferencia != null)
        {
            frente = Vector3.ProjectOnPlane(cameraReferencia.forward, Vector3.up).normalized;
            direita = Vector3.ProjectOnPlane(cameraReferencia.right, Vector3.up).normalized;
        }

        entrada = Vector3.ClampMagnitude(frente * v + direita * h, 1f);
        correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Input.GetButtonDown("Jump"))
        {
            pedidoPulo = true;
        }
    }

    /// <summary>
    /// Move o personagem na horizontal com transform.Translate e o gira
    /// suavemente para a direção do movimento.
    /// </summary>
    /// <param name="direcao">Direção no mundo (comprimento até 1).</param>
    /// <param name="velocidade">Velocidade desejada em m/s.</param>
    public void Mover(Vector3 direcao, float velocidade)
    {
        if (noChao)
        {
            // No chão o controle é total
            velocidadeHorizontal = direcao * velocidade;
        }
        else
        {
            // No ar mantém a velocidade do pulo e permite uma correção de 50%
            Vector3 correcao = direcao * (velocidadeAndar * controleNoAr);
            float limite = Mathf.Max(velocidadeNoPulo.magnitude, velocidadeAndar * controleNoAr);
            velocidadeHorizontal = Vector3.ClampMagnitude(velocidadeNoPulo + correcao, limite);
        }

        // Translate no espaço do mundo, independente da rotação do personagem
        transform.Translate(velocidadeHorizontal * Time.deltaTime, Space.World);

        // Gira suavemente para onde está andando
        if (direcao.sqrMagnitude > 0.01f)
        {
            Quaternion alvo = Quaternion.LookRotation(direcao, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, alvo, velocidadeRotacao * Time.deltaTime);
        }
    }

    /// <summary>
    /// Faz o personagem pular com um impulso vertical de força fixa.
    /// </summary>
    public void Pular()
    {
        // Zera a velocidade vertical para todo pulo ter a mesma altura
        Vector3 v = corpo.linearVelocity;
        v.y = 0f;
        corpo.linearVelocity = v;

        corpo.AddForce(Vector3.up * forcaPulo, ForceMode.Impulse);

        // Guarda a velocidade horizontal para manter o embalo no ar
        velocidadeNoPulo = velocidadeHorizontal;
        noChao = false;
    }

    /// <summary>
    /// Verifica se há chão logo abaixo dos pés com um raycast em forma de
    /// esfera (SphereCast), que funciona também na beirada das plataformas.
    /// </summary>
    /// <returns>Verdadeiro se o personagem está sobre uma superfície.</returns>
    public bool EstaNoChao()
    {
        // Subindo rápido logo depois do pulo não conta como chão
        if (corpo.linearVelocity.y > 0.5f)
        {
            return false;
        }

        float raio = capsula.radius * 0.9f;
        Vector3 origem = transform.position + Vector3.up * (raio + 0.05f);
        float distancia = 0.05f + distanciaChao;

        // Ignora triggers (pedras rúnicas, abismo...) e a camada do próprio jogador
        bool acertou = Physics.SphereCast(origem, raio, Vector3.down, out RaycastHit hit, distancia,
                                          Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Só conta como chão se a superfície não for íngreme demais
        bool chao = acertou && Vector3.Angle(hit.normal, Vector3.up) <= inclinacaoMaxima;

        // Ao pousar, o embalo do pulo deixa de valer
        if (chao)
        {
            velocidadeNoPulo = Vector3.zero;
        }
        else if (noChao)
        {
            // Saiu da plataforma andando: o embalo é a velocidade que tinha
            velocidadeNoPulo = velocidadeHorizontal;
        }
        return chao;
    }

    /// <summary>
    /// Detecta o pouso em plataformas para tocar o som de impacto e as partículas.
    /// </summary>
    /// <param name="colisao">Dados da colisão.</param>
    private void OnCollisionEnter(Collision colisao)
    {
        if (!colisao.collider.CompareTag("Plataforma"))
        {
            return;
        }

        // Só conta se bateu por cima e com velocidade suficiente
        bool porCima = colisao.contactCount > 0 && colisao.GetContact(0).normal.y > 0.5f;
        if (!porCima || Mathf.Abs(colisao.relativeVelocity.y) < velocidadeMinimaPouso)
        {
            return;
        }

        if (particulasPouso != null)
        {
            particulasPouso.Play();
        }
        AudioManager.Instancia.TocarImpacto();
    }
}
