using UnityEngine;

/// <summary>
/// Gira a câmera lentamente em volta de um ponto. Usada no fundo do menu
/// inicial para mostrar a vila do nível 1.
/// </summary>
public class CameraOrbita : MonoBehaviour
{
    [Tooltip("Ponto em volta do qual a câmera gira.")]
    [SerializeField] private Vector3 centro = Vector3.zero;

    [Tooltip("Velocidade de giro, em graus por segundo.")]
    [SerializeField] private float velocidade = 4f;

    /// <summary>
    /// Gira a câmera em volta do centro e mantém o olhar nele.
    /// </summary>
    private void Update()
    {
        transform.RotateAround(centro, Vector3.up, velocidade * Time.deltaTime);
        transform.LookAt(centro);
    }
}
