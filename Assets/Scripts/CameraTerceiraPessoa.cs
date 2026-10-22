using UnityEngine;

/// <summary>
/// Câmera em terceira pessoa: fica atrás e um pouco acima do personagem,
/// gira em volta dele com o mouse, segue suavemente e se aproxima quando
/// algum objeto fica entre ela e o personagem.
/// </summary>
public class CameraTerceiraPessoa : MonoBehaviour
{
    [Tooltip("Personagem que a câmera segue.")]
    [SerializeField] private Transform alvo;

    [Tooltip("Altura do ponto observado acima dos pés do personagem.")]
    [SerializeField] private float alturaAlvo = 1.5f;

    [Tooltip("Distância normal entre a câmera e o personagem.")]
    [SerializeField] private float distancia = 6f;

    [Tooltip("Distância mínima quando há obstáculos.")]
    [SerializeField] private float distanciaMinima = 1f;

    [Tooltip("Sensibilidade do mouse.")]
    [SerializeField] private float sensibilidade = 3f;

    [Tooltip("Limites do ângulo vertical (graus).")]
    [SerializeField] private float anguloMinimo = -30f;
    [SerializeField] private float anguloMaximo = 70f;

    [Tooltip("Suavidade do seguimento (maior = mais rápido).")]
    [SerializeField] private float suavidade = 12f;

    [Tooltip("Raio usado para detectar obstáculos entre a câmera e o personagem.")]
    [SerializeField] private float raioColisao = 0.3f;

    private float anguloHorizontal;
    private float anguloVertical = 15f;
    private Vector3 pontoSeguido;

    /// <summary>
    /// Começa olhando na mesma direção do personagem.
    /// </summary>
    private void Start()
    {
        if (alvo != null)
        {
            anguloHorizontal = alvo.eulerAngles.y;
            pontoSeguido = alvo.position + Vector3.up * alturaAlvo;
        }
    }

    /// <summary>
    /// Lê o mouse e posiciona a câmera depois que o personagem se moveu.
    /// </summary>
    private void LateUpdate()
    {
        if (alvo == null)
        {
            return;
        }

        // Pausado (ou cursor solto em menus) a câmera não gira
        if (Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked)
        {
            RotacionarCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        // Segue o personagem com suavidade
        Vector3 alvoAtual = alvo.position + Vector3.up * alturaAlvo;
        pontoSeguido = Vector3.Lerp(pontoSeguido, alvoAtual, 1f - Mathf.Exp(-suavidade * Time.deltaTime));

        // Direção da câmera a partir dos ângulos
        Quaternion rotacao = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);
        Vector3 direcaoTras = rotacao * Vector3.back;

        // Se algo estiver no caminho, aproxima a câmera do personagem
        float distanciaFinal = distancia;
        if (Physics.SphereCast(pontoSeguido, raioColisao, direcaoTras, out RaycastHit hit, distancia,
                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            distanciaFinal = Mathf.Max(distanciaMinima, hit.distance);
        }

        transform.position = pontoSeguido + direcaoTras * distanciaFinal;
        transform.rotation = rotacao;
    }

    /// <summary>
    /// Gira a câmera em volta do personagem.
    /// </summary>
    /// <param name="deltaX">Movimento horizontal do mouse.</param>
    /// <param name="deltaY">Movimento vertical do mouse.</param>
    public void RotacionarCamera(float deltaX, float deltaY)
    {
        anguloHorizontal += deltaX * sensibilidade;

        // Mouse para cima olha para cima (inverte o Y) e limita o ângulo
        anguloVertical = Mathf.Clamp(anguloVertical - deltaY * sensibilidade, anguloMinimo, anguloMaximo);
    }
}
