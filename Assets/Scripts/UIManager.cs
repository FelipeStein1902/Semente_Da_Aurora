using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface da cena de jogo: HUD ("Fase X / 5" e botão "Menu"), menu de
/// pausa, texto das pedras rúnicas e tela de nível concluído.
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>UIManager da cena atual (null em cenas sem HUD).</summary>
    public static UIManager Instancia { get; private set; }

    [Header("HUD")]
    [SerializeField] private Text textoFase;

    [Header("Pedras rúnicas")]
    [SerializeField] private GameObject painelRuna;
    [SerializeField] private Text textoRuna;

    [Header("Menu de pausa")]
    [SerializeField] private GameObject painelPausa;

    [Header("Nível concluído")]
    [SerializeField] private GameObject painelConcluido;
    [SerializeField] private Text textoConcluido;
    [SerializeField] private Text textoAvisoConcluido;
    [SerializeField] private GameObject botaoProximoNivel;
    [SerializeField] private GameObject botaoJogarNovamente;
    [SerializeField] private GameObject botaoMenuInicial;

    private bool pausado;
    private bool concluido;
    private string textoRunaAtual;

    /// <summary>
    /// Registra esta instância e esconde os painéis.
    /// </summary>
    private void Awake()
    {
        Instancia = this;

        if (painelRuna != null) painelRuna.SetActive(false);
        if (painelPausa != null) painelPausa.SetActive(false);
        if (painelConcluido != null) painelConcluido.SetActive(false);
    }

    /// <summary>
    /// Mostra a fase atual e prende o cursor para controlar a câmera.
    /// </summary>
    private void Start()
    {
        GameManager gm = GameManager.Instancia;
        AtualizarTextoFase(gm.FaseAtual, gm.TotalFases);

        // Garante que a música esteja tocando
        AudioManager.Instancia.TocarMusica();

        Time.timeScale = 1f;
        TravarCursor(true);
    }

    /// <summary>
    /// Limpa a referência estática ao sair da cena.
    /// </summary>
    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    /// <summary>
    /// Esc abre e fecha o menu de pausa.
    /// </summary>
    private void Update()
    {
        if (concluido)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado) FecharMenu();
            else AbrirMenu();
        }

        // Clicar na tela do jogo prende o cursor de novo (ex.: após Alt+Tab)
        if (!pausado && Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            TravarCursor(true);
        }
    }

    /// <summary>
    /// Atualiza o indicador de progresso no topo da tela.
    /// </summary>
    /// <param name="faseAtual">Número da fase atual.</param>
    /// <param name="totalFases">Número total de fases.</param>
    public void AtualizarTextoFase(int faseAtual, int totalFases)
    {
        if (textoFase != null)
        {
            textoFase.text = $"Fase {faseAtual} / {totalFases}";
        }
    }

    /// <summary>
    /// Abre o menu de pausa e para o tempo do jogo.
    /// </summary>
    public void AbrirMenu()
    {
        if (concluido)
        {
            return;
        }
        pausado = true;
        Time.timeScale = 0f;
        painelPausa.SetActive(true);
        TravarCursor(false);
    }

    /// <summary>
    /// Fecha o menu de pausa e volta o tempo ao normal (botão "Continuar").
    /// </summary>
    public void FecharMenu()
    {
        pausado = false;
        Time.timeScale = 1f;
        painelPausa.SetActive(false);
        TravarCursor(true);
    }

    /// <summary>
    /// Botão "Reiniciar": recomeça o jogo no nível 1.
    /// </summary>
    public void BotaoReiniciar()
    {
        GameManager.Instancia.ReiniciarJogo();
    }

    /// <summary>
    /// Botão "Sair do jogo": fecha o jogo.
    /// </summary>
    public void BotaoSair()
    {
        GameManager.Instancia.SairDoJogo();
    }

    /// <summary>
    /// Botão "Jogar novamente": começa uma partida nova no nível 1.
    /// </summary>
    public void BotaoJogarNovamente()
    {
        GameManager.Instancia.IniciarJogo();
    }

    /// <summary>
    /// Botão "Menu inicial": volta ao menu inicial.
    /// </summary>
    public void BotaoMenuInicial()
    {
        GameManager.Instancia.IrParaMenu();
    }

    /// <summary>
    /// Botão "Próximo nível": carrega o nível seguinte.
    /// </summary>
    public void BotaoProximoNivel()
    {
        GameManager.Instancia.IrParaProximaFase();
    }

    /// <summary>
    /// Mostra o texto de uma pedra rúnica na parte inferior da tela.
    /// </summary>
    /// <param name="texto">Texto a mostrar.</param>
    public void MostrarTextoRuna(string texto)
    {
        textoRunaAtual = texto;
        textoRuna.text = texto;
        painelRuna.SetActive(true);
    }

    /// <summary>
    /// Esconde o texto da pedra rúnica, se ele ainda for o que está na tela.
    /// Assim, sair do trigger de uma pedra não apaga o texto de outra.
    /// </summary>
    /// <param name="texto">Texto da pedra que pediu para esconder.</param>
    public void EsconderTextoRuna(string texto)
    {
        if (textoRunaAtual == texto)
        {
            painelRuna.SetActive(false);
            textoRunaAtual = null;
        }
    }

    /// <summary>
    /// Mostra a tela de nível concluído com o texto da restauração.
    /// </summary>
    /// <param name="textoRestauracao">Texto da cena de restauração.</param>
    /// <param name="proximoExiste">Se o próximo nível já existe no jogo.</param>
    /// <param name="ultimaFase">Se este era o último nível (vitória).</param>
    public void MostrarNivelConcluido(string textoRestauracao, bool proximoExiste, bool ultimaFase)
    {
        concluido = true;
        Time.timeScale = 0f;
        TravarCursor(false);

        painelRuna.SetActive(false);
        painelPausa.SetActive(false);
        painelConcluido.SetActive(true);
        textoConcluido.text = textoRestauracao;

        // Escolhe o aviso e os botões conforme a situação
        if (ultimaFase)
        {
            textoAvisoConcluido.text = "Verdália está viva, Bran. Obrigada.";
        }
        else if (proximoExiste)
        {
            textoAvisoConcluido.text = "";
        }
        else
        {
            textoAvisoConcluido.text = "Os próximos níveis estão em desenvolvimento.";
        }

        botaoProximoNivel.SetActive(proximoExiste);
        botaoJogarNovamente.SetActive(!proximoExiste);
        botaoMenuInicial.SetActive(true);
    }

    /// <summary>
    /// Prende (e esconde) ou solta (e mostra) o cursor do mouse.
    /// </summary>
    /// <param name="travar">Verdadeiro para prender o cursor no centro da tela.</param>
    private static void TravarCursor(bool travar)
    {
        Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !travar;
    }
}
