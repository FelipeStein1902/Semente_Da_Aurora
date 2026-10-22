using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de introdução (Cena 1): mostra as falas de Ysolde uma de cada vez.
/// O botão "Continuar" avança as falas e, depois da última, começa o nível 1.
/// </summary>
public class TelaIntroducao : MonoBehaviour
{
    [Tooltip("Texto onde as falas aparecem.")]
    [SerializeField] private Text textoFala;

    [Tooltip("Falas da introdução, em ordem.")]
    [TextArea(2, 5)]
    public string[] falas =
    {
        "Ysolde: \"Bran, a Árvore-Mãe se foi. Tudo o que restou dela está nas suas mãos. Esta semente ainda guarda a vida de Verdália.\"",
        "Ysolde: \"Suba, meu jovem. Suba até a Forja do Barão, onde tudo começou, e plante a semente lá. Por onde você passar, a terra vai se lembrar de como era viver.\"",
        "Ysolde: \"Mas cuidado: se cair, a semente volta para o lugar onde nasceu, e você terá de recomeçar do vale.\""
    };

    private int indiceFala;

    /// <summary>
    /// Solta o cursor e mostra a primeira fala.
    /// </summary>
    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instancia.TocarMusica();
        MostrarFala(0);
    }

    /// <summary>
    /// Botão "Continuar": avança para a próxima fala ou começa o jogo.
    /// </summary>
    public void BotaoContinuar()
    {
        if (indiceFala < falas.Length - 1)
        {
            MostrarFala(indiceFala + 1);
        }
        else
        {
            GameManager.Instancia.IniciarJogo();
        }
    }

    /// <summary>
    /// Mostra uma fala da lista.
    /// </summary>
    /// <param name="indice">Posição da fala no vetor.</param>
    private void MostrarFala(int indice)
    {
        indiceFala = indice;
        if (textoFala != null && falas.Length > 0)
        {
            textoFala.text = falas[indice];
        }
    }
}
