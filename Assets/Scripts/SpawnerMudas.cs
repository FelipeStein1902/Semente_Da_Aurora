using UnityEngine;

/// <summary>
/// Cria flores e mudas aleatórias nas plataformas restauradas.
/// Usa um vetor de prefabs e sorteia qual deles será criado a cada vez.
/// </summary>
public class SpawnerMudas : MonoBehaviour
{
    [Tooltip("Vetor com os prefabs de flores e mudas.")]
    public GameObject[] prefabsMudas;

    [Tooltip("Objeto que agrupa as mudas criadas (opcional, para organizar a Hierarchy).")]
    [SerializeField] private Transform grupoMudas;

    /// <summary>
    /// Cria uma flor ou muda aleatória na posição informada.
    /// Usada quando o jogador pousa em uma plataforma seca.
    /// </summary>
    /// <param name="posicao">Posição no mundo onde a muda será criada.</param>
    /// <returns>O objeto da muda que foi criado (ou null se o vetor estiver vazio).</returns>
    public GameObject SpawnarMuda(Vector3 posicao)
    {
        // Sem prefabs no vetor não há o que criar
        if (prefabsMudas == null || prefabsMudas.Length == 0)
        {
            return null;
        }

        // Escolhe aleatoriamente um prefab do vetor de mudas
        GameObject prefab = EscolherPrefabAleatorio();

        // Gira a muda em um ângulo aleatório no eixo Y para variar o visual
        Quaternion rotacao = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Cria a muda na cena e a devolve para quem chamou
        GameObject muda = Instantiate(prefab, posicao, rotacao, grupoMudas);

        // Pequena variação de tamanho para nenhuma ficar igual
        muda.transform.localScale *= Random.Range(0.8f, 1.25f);
        return muda;
    }

    /// <summary>
    /// Sorteia um prefab do vetor de mudas.
    /// </summary>
    /// <returns>O prefab sorteado.</returns>
    private GameObject EscolherPrefabAleatorio()
    {
        // Random.Range com inteiros não inclui o valor máximo,
        // então o índice vai de 0 até prefabsMudas.Length - 1
        int indice = Random.Range(0, prefabsMudas.Length);
        return prefabsMudas[indice];
    }
}
