using System.Collections;
using UnityEngine;

/// <summary>
/// Ponto final do nível (Tag "Final"), a Nascente do Vale. Ao ser tocada,
/// dispara a restauração: explosão de luz, água correndo, fumaça da forja
/// apagando, roda d'água girando, árvores secas virando verdes e o mundo
/// ganhando cor. Depois conclui o nível.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NascenteFinal : MonoBehaviour
{
    [Header("Efeitos")]
    [Tooltip("Explosão de partículas de luz.")]
    [SerializeField] private ParticleSystem explosaoLuz;

    [Tooltip("Água que volta a correr na fonte.")]
    [SerializeField] private ParticleSystem agua;

    [Tooltip("Fumaça da chaminé da forja, que se apaga.")]
    [SerializeField] private ParticleSystem fumacaForja;

    [Tooltip("Roda d'água da vila.")]
    [SerializeField] private RodaDagua rodaDagua;

    [Tooltip("Superfície de água da fonte, ativada na restauração (opcional).")]
    [SerializeField] private GameObject espelhoDagua;

    [Header("Árvores")]
    [Tooltip("Árvores secas ao redor da Nascente (são desativadas).")]
    [SerializeField] private GameObject[] arvoresSecas;

    [Tooltip("Árvores verdes que aparecem no lugar das secas.")]
    [SerializeField] private GameObject[] arvoresVerdes;

    [Header("Terreno")]
    [Tooltip("Renderers do chão e das colinas que estão mortos (um para cada material vivo).")]
    [SerializeField] private Renderer[] terrenoMorto;

    [Tooltip("Material verde de cada renderer de 'Terreno Morto', na mesma ordem.")]
    [SerializeField] private Material[] terrenoVivo;

    [Header("Cores do mundo restaurado")]
    [SerializeField] private Light luzSol;
    [SerializeField] private Color corSolRestaurado = new Color(1f, 0.95f, 0.8f);
    [SerializeField] private Color corCeuRestaurado = new Color(0.55f, 0.7f, 0.9f);
    [SerializeField] private Color corHorizonteRestaurado = new Color(0.6f, 0.7f, 0.55f);
    [SerializeField] private Color corChaoRestaurado = new Color(0.3f, 0.35f, 0.2f);
    [SerializeField] private Color corNevoaRestaurada = new Color(0.7f, 0.8f, 0.85f);
    [SerializeField] private float densidadeNevoaRestaurada = 0.006f;
    [SerializeField] private Material skyboxRestaurado;

    [Tooltip("Duração da transição de cores, em segundos.")]
    [SerializeField] private float duracaoTransicao = 3f;

    [Header("História")]
    [Tooltip("Texto da cena de restauração mostrado na tela de nível concluído.")]
    [TextArea(2, 5)]
    [SerializeField] private string textoRestauracao =
        "A semente brilha. A água volta a correr pelas pedras e, lá embaixo, a fumaça da velha forja finalmente se apaga. O vale respira de novo.";

    private bool ativada;

    /// <summary>
    /// Detecta o jogador tocando a Nascente.
    /// </summary>
    /// <param name="outro">Collider que entrou no trigger.</param>
    private void OnTriggerEnter(Collider outro)
    {
        if (outro.CompareTag("Player"))
        {
            AtivarRestauracao();
        }
    }

    /// <summary>
    /// Dispara todos os efeitos de restauração e, ao final, conclui o nível.
    /// Só acontece uma vez.
    /// </summary>
    public void AtivarRestauracao()
    {
        if (ativada)
        {
            return;
        }
        ativada = true;

        // 1. Explosão de luz e som de restauração
        if (explosaoLuz != null) explosaoLuz.Play();
        AudioManager.Instancia.TocarRestauracao();

        // 2. A água volta a correr
        if (agua != null) agua.Play();
        if (espelhoDagua != null) espelhoDagua.SetActive(true);

        // 3. A fumaça da forja se apaga (as partículas que já existem somem aos poucos)
        if (fumacaForja != null) fumacaForja.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 4. A roda d'água volta a girar
        if (rodaDagua != null) rodaDagua.Ativar();

        // 5. Árvores secas dão lugar às verdes
        foreach (GameObject arvore in arvoresSecas)
        {
            if (arvore != null) arvore.SetActive(false);
        }
        foreach (GameObject arvore in arvoresVerdes)
        {
            // As árvores e arbustos vivos crescem do chão
            CrescimentoDeArvores.Ativar(this, arvore, 2.5f);
        }

        // 5b. O chão e as colinas ficam verdes
        if (terrenoMorto != null && terrenoVivo != null)
        {
            int total = Mathf.Min(terrenoMorto.Length, terrenoVivo.Length);
            for (int i = 0; i < total; i++)
            {
                if (terrenoMorto[i] != null && terrenoVivo[i] != null)
                {
                    terrenoMorto[i].sharedMaterial = terrenoVivo[i];
                }
            }
        }

        // 6. O mundo ganha cor e, em seguida, aparece a tela de nível concluído
        StartCoroutine(RotinaRestaurarCores());
    }

    /// <summary>
    /// Faz a transição das cores do céu, da luz e da névoa e depois conclui o nível.
    /// </summary>
    /// <returns>Rotina da corrotina.</returns>
    private IEnumerator RotinaRestaurarCores()
    {
        // Guarda as cores atuais (mundo destruído)
        Color solInicial = luzSol != null ? luzSol.color : Color.white;
        Color ceuInicial = RenderSettings.ambientSkyColor;
        Color horizonteInicial = RenderSettings.ambientEquatorColor;
        Color chaoInicial = RenderSettings.ambientGroundColor;
        Color nevoaInicial = RenderSettings.fogColor;
        float densidadeInicial = RenderSettings.fogDensity;

        if (skyboxRestaurado != null)
        {
            RenderSettings.skybox = skyboxRestaurado;
        }

        // Interpola tudo ao longo da duração da transição
        for (float t = 0f; t < 1f; t += Time.deltaTime / duracaoTransicao)
        {
            if (luzSol != null) luzSol.color = Color.Lerp(solInicial, corSolRestaurado, t);
            RenderSettings.ambientSkyColor = Color.Lerp(ceuInicial, corCeuRestaurado, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(horizonteInicial, corHorizonteRestaurado, t);
            RenderSettings.ambientGroundColor = Color.Lerp(chaoInicial, corChaoRestaurado, t);
            RenderSettings.fogColor = Color.Lerp(nevoaInicial, corNevoaRestaurada, t);
            RenderSettings.fogDensity = Mathf.Lerp(densidadeInicial, densidadeNevoaRestaurada, t);
            yield return null;
        }

        // Um pequeno tempo para o jogador apreciar o vale restaurado
        yield return new WaitForSeconds(1f);

        GameManager.Instancia.CompletarFase(textoRestauracao);
    }
}
