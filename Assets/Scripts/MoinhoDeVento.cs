using UnityEngine;

/// <summary>
/// Faz as pás de um moinho de vento girarem aplicando torque no Rigidbody.
/// As pás ficam presas ao eixo por um HingeJoint e podem ser usadas como
/// plataforma pelo jogador.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MoinhoDeVento : MonoBehaviour
{
    [Tooltip("Torque aplicado a cada passo de física.")]
    [SerializeField] private float torque = 400f;

    [Tooltip("Velocidade angular máxima, em graus por segundo.")]
    [SerializeField] private float velocidadeMaxima = 25f;

    [Tooltip("Eixo de rotação, no espaço local das pás.")]
    [SerializeField] private Vector3 eixoLocal = Vector3.forward;

    [Tooltip("Sentido do giro: 1 ou -1.")]
    [SerializeField] private float sentido = 1f;

    private Rigidbody corpo;

    /// <summary>
    /// Configura o Rigidbody das pás (sem gravidade, com limite de velocidade).
    /// </summary>
    private void Awake()
    {
        corpo = GetComponent<Rigidbody>();
        corpo.useGravity = false;
        corpo.maxAngularVelocity = velocidadeMaxima * Mathf.Deg2Rad;
        corpo.angularDamping = 0.5f;
    }

    /// <summary>
    /// Aplica o torque em todo passo de física.
    /// </summary>
    private void FixedUpdate()
    {
        AplicarTorque();
    }

    /// <summary>
    /// Empurra as pás em volta do eixo com Rigidbody.AddTorque.
    /// </summary>
    public void AplicarTorque()
    {
        // Converte o eixo local para o mundo e aplica o torque nele
        Vector3 eixoMundo = transform.TransformDirection(eixoLocal).normalized;
        corpo.AddTorque(eixoMundo * (torque * sentido), ForceMode.Force);
    }
}
