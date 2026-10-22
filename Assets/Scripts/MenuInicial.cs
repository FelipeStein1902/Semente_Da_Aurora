using UnityEngine;

/// <summary>
/// Botões do menu inicial: "Jogar" e "Sair".
/// </summary>
public class MenuInicial : MonoBehaviour
{
    /// <summary>
    /// Solta o cursor e começa a música.
    /// </summary>
    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instancia.TocarMusica();
    }

    /// <summary>
    /// Botão "Jogar": vai para a tela de introdução.
    /// </summary>
    public void BotaoJogar()
    {
        GameManager.Instancia.IrParaIntroducao();
    }

    /// <summary>
    /// Botão "Sair": fecha o jogo.
    /// </summary>
    public void BotaoSair()
    {
        GameManager.Instancia.SairDoJogo();
    }
}
