using UnityEngine;

/// <summary>
/// Roda d'água da vila. Fica parada enquanto o rio está seco e volta a girar
/// (com transform.Rotate) quando a Nascente é despertada.
/// </summary>
public class RodaDagua : MonoBehaviour
{
    [Tooltip("Velocidade de rotação, em graus por segundo.")]
    [SerializeField] private float velocidade = 40f;

    [Tooltip("Eixo de rotação no espaço local.")]
    [SerializeField] private Vector3 eixo = Vector3.right;

    [Tooltip("Tempo para chegar à velocidade máxima depois de ativada, em segundos.")]
    [SerializeField] private float tempoAceleracao = 2f;

    [Tooltip("Água que cai na roda (opcional), ligada junto com ela.")]
    [SerializeField] private ParticleSystem agua;

    private bool ativa;
    private float velocidadeAtual;

    /// <summary>
    /// Gira a roda se ela estiver ativa, acelerando aos poucos.
    /// </summary>
    private void Update()
    {
        if (!ativa)
        {
            return;
        }

        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidade, velocidade / tempoAceleracao * Time.deltaTime);
        transform.Rotate(eixo, velocidadeAtual * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Faz a roda voltar a girar.
    /// </summary>
    public void Ativar()
    {
        ativa = true;
        if (agua != null)
        {
            agua.Play();
        }
    }
}
