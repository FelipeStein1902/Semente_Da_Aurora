using UnityEngine;

/// <summary>
/// Plataforma seca que volta a florescer na primeira vez que o jogador pousa
/// nela: troca o material (cinza para verde) e pede ao spawner para criar
/// flores e mudas aleatórias em cima dela.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlataformaReflorescente : MonoBehaviour
{
    [Tooltip("Material da plataforma restaurada.")]
    public Material materialVerde;

    [Tooltip("Renderer que troca de material. Se vazio, usa o deste objeto.")]
    [SerializeField] private Renderer rendererPlataforma;

    [Tooltip("Spawner de mudas. Se vazio, procura um na cena.")]
    public SpawnerMudas spawner;

    [Tooltip("Quantidade mínima e máxima de flores criadas.")]
    [SerializeField] private int minimoMudas = 2;
    [SerializeField] private int maximoMudas = 5;

    [Tooltip("Árvore morta em cima da plataforma (opcional). Some quando a plataforma refloresce.")]
    [SerializeField] private GameObject arvoreSeca;

    [Tooltip("Árvore viva (começa desligada). Cresce no lugar da morta.")]
    [SerializeField] private GameObject arvoreViva;

    private bool restaurada;
    private Collider colisor;

    /// <summary>Verdadeiro depois que a plataforma refloresceu.</summary>
    public bool Restaurada => restaurada;

    /// <summary>
    /// Pega as referências que não foram definidas no Inspector.
    /// </summary>
    private void Awake()
    {
        colisor = GetComponent<Collider>();
        if (rendererPlataforma == null)
        {
            rendererPlataforma = GetComponent<Renderer>();
        }
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<SpawnerMudas>();
        }
    }

    /// <summary>
    /// Detecta o jogador pousando por cima da plataforma.
    /// </summary>
    /// <param name="colisao">Dados da colisão.</param>
    private void OnCollisionEnter(Collision colisao)
    {
        if (restaurada || !colisao.collider.CompareTag("Player"))
        {
            return;
        }

        // Só refloresce se os pés do jogador estão no topo (e não batendo de lado)
        if (colisao.transform.position.y >= colisor.bounds.max.y - 0.2f)
        {
            Reflorescer();
        }
    }

    /// <summary>
    /// Troca o material da plataforma para o verde e cria as flores.
    /// Só acontece uma vez por plataforma.
    /// </summary>
    public void Reflorescer()
    {
        if (restaurada)
        {
            return;
        }
        restaurada = true;

        // Troca a cor seca pela verde
        if (rendererPlataforma != null && materialVerde != null)
        {
            rendererPlataforma.sharedMaterial = materialVerde;
        }

        // A árvore morta que estava na plataforma volta à vida
        if (arvoreSeca != null)
        {
            arvoreSeca.SetActive(false);
        }
        if (arvoreViva != null)
        {
            CrescimentoDeArvores.Ativar(this, arvoreViva);
        }

        if (spawner == null)
        {
            return;
        }

        // Cria uma quantidade aleatória de mudas em pontos aleatórios do topo
        Bounds limites = colisor.bounds;
        int quantidade = Random.Range(minimoMudas, maximoMudas + 1);
        for (int i = 0; i < quantidade; i++)
        {
            // Deixa uma margem de 15% para as flores não ficarem na beirada
            float x = Random.Range(limites.min.x + limites.extents.x * 0.3f, limites.max.x - limites.extents.x * 0.3f);
            float z = Random.Range(limites.min.z + limites.extents.z * 0.3f, limites.max.z - limites.extents.z * 0.3f);
            spawner.SpawnarMuda(new Vector3(x, limites.max.y, z));
        }

    }
}
