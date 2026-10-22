using UnityEngine;

/// <summary>
/// Zona de morte abaixo do nível (Tag "Abismo"). Quando o jogador toca nela,
/// toca o som de queda e o jogo recomeça no nível 1.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaAbismo : MonoBehaviour
{
    private bool acionada;

    /// <summary>
    /// Garante que o collider seja um trigger.
    /// </summary>
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    /// <summary>
    /// Detecta a entrada do jogador na zona de queda.
    /// </summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    private void OnTriggerEnter(Collider outro)
    {
        // Só reage ao jogador, e apenas uma vez
        if (acionada || !outro.CompareTag("Player"))
        {
            return;
        }
        acionada = true;

        AudioManager.Instancia.TocarQueda();
        GameManager.Instancia.ReiniciarJogo(porQueda: true);
    }
}
