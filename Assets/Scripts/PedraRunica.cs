using UnityEngine;

/// <summary>
/// Pedra rúnica do sistema de ajuda (Tag "Runa"). Quando o jogador entra no
/// trigger em volta dela, o texto aparece na parte inferior da tela; quando
/// ele sai, o texto some.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PedraRunica : MonoBehaviour
{
    [Tooltip("Texto mostrado ao se aproximar.")]
    [TextArea(2, 5)]
    public string texto = "Texto da pedra rúnica.";

    [Tooltip("Luz ou brilho que acende enquanto o jogador está perto (opcional).")]
    [SerializeField] private Light brilho;

    /// <summary>
    /// Mostra o texto quando o jogador se aproxima.
    /// </summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    private void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player"))
        {
            MostrarTexto();
        }
    }

    /// <summary>
    /// Esconde o texto quando o jogador se afasta.
    /// </summary>
    /// <param name="outro">Collider que saiu do trigger.</param>
    private void OnTriggerExit(Collider outro)
    {
        if (outro.CompareTag("Player"))
        {
            EsconderTexto();
        }
    }

    /// <summary>
    /// Mostra o texto desta pedra na interface.
    /// </summary>
    public void MostrarTexto()
    {
        if (UIManager.Instancia != null)
        {
            UIManager.Instancia.MostrarTextoRuna(texto);
        }
        if (brilho != null)
        {
            brilho.enabled = true;
        }
    }

    /// <summary>
    /// Esconde o texto da interface (se ainda for o desta pedra).
    /// </summary>
    public void EsconderTexto()
    {
        if (UIManager.Instancia != null)
        {
            UIManager.Instancia.EsconderTextoRuna(texto);
        }
        if (brilho != null)
        {
            brilho.enabled = false;
        }
    }
}
