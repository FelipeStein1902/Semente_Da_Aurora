using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla o fluxo do jogo: guarda a fase atual, troca de cenas,
/// conclui fases e reinicia o jogo no nível 1 quando o jogador cai.
/// Existe uma única instância, que sobrevive à troca de cenas.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Nome da cena do menu inicial.</summary>
    public const string CenaMenu = "MenuInicial";

    /// <summary>Nome da cena da tela de introdução.</summary>
    public const string CenaIntroducao = "Introducao";

    /// <summary>Nome das cenas de cada fase, na ordem (índice 0 = fase 1).</summary>
    public static readonly string[] CenasFases = { "Nivel1", "Nivel2", "Nivel3", "Nivel4", "Nivel5" };

    private static GameManager instancia;

    [Tooltip("Duração do escurecimento da tela, em segundos.")]
    [SerializeField] private float duracaoFade = 0.6f;

    [Tooltip("Tempo que a mensagem de queda fica na tela, em segundos.")]
    [SerializeField] private float tempoMensagemQueda = 1.2f;

    private int faseAtual = 1;
    private bool trocandoCena;
    private CanvasGroup grupoFade;
    private Text textoFade;

    /// <summary>
    /// Instância única do GameManager. Se ela ainda não existir (por exemplo,
    /// ao dar Play direto na cena do nível), é criada automaticamente.
    /// </summary>
    public static GameManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                // Cria o objeto do GameManager na hora em que alguém precisar dele
                new GameObject("GameManager").AddComponent<GameManager>();
            }
            return instancia;
        }
    }

    /// <summary>Número da fase que está sendo jogada (de 1 a <see cref="TotalFases"/>).</summary>
    public int FaseAtual => faseAtual;

    /// <summary>Quantidade total de fases do jogo.</summary>
    public int TotalFases => CenasFases.Length;

    /// <summary>
    /// Garante que exista só um GameManager e prepara a tela de fade.
    /// </summary>
    private void Awake()
    {
        // Se já existe outro GameManager, este é descartado
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        // Cria a camada preta usada para escurecer a tela entre as cenas
        CriarTelaFade();

        // Descobre a fase da cena aberta agora e das próximas que forem carregadas
        AtualizarFasePelaCena(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    /// <summary>
    /// Remove o evento de carregamento de cena ao destruir o objeto.
    /// </summary>
    private void OnDestroy()
    {
        if (instancia == this)
        {
            SceneManager.sceneLoaded -= AoCarregarCena;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Tecla de teste F9 (só no editor): conclui o nível atual na hora.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F9))
        {
            // Usa a Nascente da cena, se houver, para mostrar a restauração completa
            NascenteFinal nascente = FindFirstObjectByType<NascenteFinal>();
            if (nascente != null)
            {
                nascente.AtivarRestauracao();
            }
            else
            {
                CompletarFase();
            }
        }
    }
#endif

    /// <summary>
    /// Começa uma partida nova a partir do nível 1, sem mensagem de queda.
    /// Usado pelos botões "Continuar" da introdução e "Jogar novamente".
    /// </summary>
    public void IniciarJogo()
    {
        CarregarFase(1);
    }

    /// <summary>
    /// Carrega a cena de uma fase.
    /// </summary>
    /// <param name="numeroFase">Número da fase, de 1 até o total de fases.</param>
    /// <returns>Verdadeiro se a cena da fase existe no Build Settings e começou a carregar.</returns>
    public bool CarregarFase(int numeroFase)
    {
        // Ignora números fora do intervalo de fases
        if (numeroFase < 1 || numeroFase > TotalFases)
        {
            return false;
        }

        // Fases que ainda não foram criadas não estão no Build Settings
        string cena = CenasFases[numeroFase - 1];
        if (!Application.CanStreamedLevelBeLoaded(cena))
        {
            return false;
        }

        faseAtual = numeroFase;
        CarregarCena(cena, null);
        return true;
    }

    /// <summary>
    /// Chamado quando o jogador alcança o ponto final do nível.
    /// Carrega o próximo nível ou, se ele ainda não existir, mostra a tela
    /// de nível concluído com as opções de jogar novamente ou voltar ao menu.
    /// </summary>
    /// <param name="textoRestauracao">Texto da cena de restauração do nível.</param>
    public void CompletarFase(string textoRestauracao = "")
    {
        if (trocandoCena)
        {
            return;
        }

        int proximaFase = faseAtual + 1;
        bool ultimaFase = faseAtual >= TotalFases;
        bool proximaExiste = !ultimaFase &&
                             Application.CanStreamedLevelBeLoaded(CenasFases[proximaFase - 1]);

        // A tela de nível concluído é controlada pelo UIManager da cena
        if (UIManager.Instancia != null)
        {
            UIManager.Instancia.MostrarNivelConcluido(textoRestauracao, proximaExiste, ultimaFase);
        }
        else if (proximaExiste)
        {
            // Sem interface na cena: segue direto para o próximo nível
            CarregarFase(proximaFase);
        }
    }

    /// <summary>
    /// Carrega o nível seguinte ao atual (botão "Próximo nível").
    /// </summary>
    public void IrParaProximaFase()
    {
        CarregarFase(faseAtual + 1);
    }

    /// <summary>
    /// Reinicia o jogo no nível 1. Usado quando o jogador cai no abismo
    /// e pelo botão "Reiniciar" do menu de pausa.
    /// </summary>
    /// <param name="porQueda">Se verdadeiro, mostra "Você caiu! Recomeçando do vale...".</param>
    public void ReiniciarJogo(bool porQueda = false)
    {
        if (trocandoCena)
        {
            return;
        }

        faseAtual = 1;
        string mensagem = porQueda ? "Você caiu! Recomeçando do vale..." : null;
        CarregarCena(CenasFases[0], mensagem);
    }

    /// <summary>
    /// Abre a tela de introdução (botão "Jogar" do menu inicial).
    /// </summary>
    public void IrParaIntroducao()
    {
        CarregarCena(CenaIntroducao, null);
    }

    /// <summary>
    /// Volta para o menu inicial.
    /// </summary>
    public void IrParaMenu()
    {
        CarregarCena(CenaMenu, null);
    }

    /// <summary>
    /// Fecha o jogo. No editor, apenas para o modo Play.
    /// </summary>
    public void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Começa a troca de cena com escurecimento da tela.
    /// </summary>
    /// <param name="cena">Nome da cena a carregar.</param>
    /// <param name="mensagem">Texto mostrado sobre a tela preta, ou null para nenhum.</param>
    private void CarregarCena(string cena, string mensagem)
    {
        if (trocandoCena)
        {
            return;
        }
        StartCoroutine(RotinaTrocarCena(cena, mensagem));
    }

    /// <summary>
    /// Escurece a tela, mostra a mensagem (se houver), carrega a cena e clareia de novo.
    /// </summary>
    /// <param name="cena">Nome da cena a carregar.</param>
    /// <param name="mensagem">Texto mostrado sobre a tela preta, ou null.</param>
    /// <returns>Rotina da corrotina.</returns>
    private IEnumerator RotinaTrocarCena(string cena, string mensagem)
    {
        trocandoCena = true;

        // Escurece a tela usando tempo real, porque o jogo pode estar pausado
        textoFade.text = mensagem ?? "";
        yield return Fade(0f, 1f);

        if (!string.IsNullOrEmpty(mensagem))
        {
            yield return new WaitForSecondsRealtime(tempoMensagemQueda);
        }

        // Garante que o jogo não continue pausado na cena nova
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync(cena);

        // Clareia a tela na cena nova
        yield return Fade(1f, 0f);
        textoFade.text = "";
        trocandoCena = false;
    }

    /// <summary>
    /// Anima a opacidade da tela preta.
    /// </summary>
    /// <param name="de">Opacidade inicial (0 = transparente, 1 = preto).</param>
    /// <param name="para">Opacidade final.</param>
    /// <returns>Rotina da corrotina.</returns>
    private IEnumerator Fade(float de, float para)
    {
        // Bloqueia cliques durante o fade
        grupoFade.blocksRaycasts = true;

        for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
        {
            grupoFade.alpha = Mathf.Lerp(de, para, t / duracaoFade);
            yield return null;
        }

        grupoFade.alpha = para;
        grupoFade.blocksRaycasts = para > 0.5f;
    }

    /// <summary>
    /// Ao carregar uma cena, atualiza a fase atual se for uma cena de nível.
    /// </summary>
    /// <param name="cena">Cena carregada.</param>
    /// <param name="modo">Modo de carregamento (não usado).</param>
    private void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        AtualizarFasePelaCena(cena);
    }

    /// <summary>
    /// Procura o nome da cena na lista de fases e guarda o número dela.
    /// </summary>
    /// <param name="cena">Cena a verificar.</param>
    private void AtualizarFasePelaCena(Scene cena)
    {
        int indice = System.Array.IndexOf(CenasFases, cena.name);
        if (indice >= 0)
        {
            faseAtual = indice + 1;
        }
    }

    /// <summary>
    /// Cria por código o Canvas com a tela preta e o texto do fade.
    /// Fica dentro do GameManager para continuar visível durante a troca de cena.
    /// </summary>
    private void CriarTelaFade()
    {
        // Canvas acima de todos os outros
        GameObject objCanvas = new GameObject("TelaFade");
        objCanvas.transform.SetParent(transform);
        Canvas canvas = objCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler escala = objCanvas.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        objCanvas.AddComponent<GraphicRaycaster>();

        grupoFade = objCanvas.AddComponent<CanvasGroup>();
        grupoFade.alpha = 0f;
        grupoFade.blocksRaycasts = false;

        // Retângulo preto que cobre a tela toda
        GameObject objFundo = new GameObject("Preto", typeof(RectTransform), typeof(Image));
        objFundo.transform.SetParent(objCanvas.transform, false);
        EsticarNaTela(objFundo.GetComponent<RectTransform>());
        objFundo.GetComponent<Image>().color = Color.black;

        // Texto central (mensagem de queda)
        GameObject objTexto = new GameObject("Mensagem", typeof(RectTransform), typeof(Text));
        objTexto.transform.SetParent(objCanvas.transform, false);
        EsticarNaTela(objTexto.GetComponent<RectTransform>());
        textoFade = objTexto.GetComponent<Text>();
        textoFade.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textoFade.fontSize = 48;
        textoFade.alignment = TextAnchor.MiddleCenter;
        textoFade.color = new Color(0.95f, 0.9f, 0.75f);
        textoFade.text = "";
    }

    /// <summary>
    /// Faz um RectTransform ocupar a tela inteira.
    /// </summary>
    /// <param name="rect">RectTransform a esticar.</param>
    private static void EsticarNaTela(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
