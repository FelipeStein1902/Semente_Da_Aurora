using UnityEngine;

/// <summary>
/// Envia o estado do personagem para o Animator (parado, caminhando,
/// correndo, pulando e caindo). Funciona mesmo sem Animator ou sem
/// animações, para o placeholder poder ser trocado pelo modelo do Mixamo.
/// </summary>
public class PlayerAnimacao : MonoBehaviour
{
    // Nomes dos parâmetros no Animator Controller (Assets/Animacoes/Bran.controller)
    private static readonly int ParamVelocidade = Animator.StringToHash("Velocidade");
    private static readonly int ParamNoChao = Animator.StringToHash("NoChao");
    private static readonly int ParamVelocidadeVertical = Animator.StringToHash("VelocidadeVertical");

    [Tooltip("Animator do modelo do personagem. Se vazio, procura nos filhos.")]
    [SerializeField] private Animator animador;

    /// <summary>
    /// Procura o Animator nos filhos caso não tenha sido definido.
    /// </summary>
    private void Awake()
    {
        if (animador == null)
        {
            animador = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Atualiza os parâmetros do Animator.
    /// </summary>
    /// <param name="velocidade">Velocidade horizontal em m/s (0 = parado).</param>
    /// <param name="noChao">Se o personagem está no chão.</param>
    /// <param name="velocidadeVertical">Velocidade vertical em m/s (positiva subindo, negativa caindo).</param>
    public void AtualizarAnimacao(float velocidade, bool noChao, float velocidadeVertical)
    {
        // Sem Animator ou sem controller não há o que atualizar
        if (animador == null || animador.runtimeAnimatorController == null)
        {
            return;
        }

        animador.SetFloat(ParamVelocidade, velocidade);
        animador.SetBool(ParamNoChao, noChao);
        animador.SetFloat(ParamVelocidadeVertical, velocidadeVertical);
    }
}
