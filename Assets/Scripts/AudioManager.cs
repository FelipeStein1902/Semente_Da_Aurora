using UnityEngine;

/// <summary>
/// Toca a música de fundo e os efeitos sonoros do jogo.
/// Existe uma única instância, que sobrevive à troca de cenas para a música
/// não recomeçar a cada queda. Os sons ficam no prefab Resources/AudioManager.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager instancia;

    [Header("Música")]
    [Tooltip("Trilha medieval tocada em loop.")]
    public AudioClip musica;

    [Range(0f, 1f)] public float volumeMusica = 0.45f;

    [Header("Efeitos sonoros")]
    [Tooltip("Quando o personagem cai no abismo.")]
    public AudioClip somQueda;

    [Tooltip("Quando o personagem aterrissa em uma plataforma.")]
    public AudioClip somImpacto;

    [Tooltip("Quando o jogador toca o ponto final do nível.")]
    public AudioClip somRestauracao;

    [Range(0f, 1f)] public float volumeEfeitos = 0.8f;

    private AudioSource fonteMusica;
    private AudioSource fonteEfeitos;

    /// <summary>
    /// Instância única do AudioManager. Se não existir, é criada a partir do
    /// prefab Resources/AudioManager (ou vazia, caso o prefab não exista).
    /// </summary>
    public static AudioManager Instancia
    {
        get
        {
            if (instancia == null)
            {
                // Tenta usar o prefab com os sons já configurados
                AudioManager prefab = Resources.Load<AudioManager>("AudioManager");
                if (prefab != null)
                {
                    Instantiate(prefab).name = "AudioManager";
                }
                else
                {
                    new GameObject("AudioManager").AddComponent<AudioManager>();
                }
            }
            return instancia;
        }
    }

    /// <summary>
    /// Garante uma única instância e cria as fontes de áudio.
    /// </summary>
    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);

        // Uma fonte para a música (em loop) e outra para os efeitos
        fonteMusica = gameObject.AddComponent<AudioSource>();
        fonteMusica.loop = true;
        fonteMusica.playOnAwake = false;
        fonteMusica.volume = volumeMusica;

        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;
        fonteEfeitos.volume = volumeEfeitos;
    }

    /// <summary>
    /// Começa a música assim que o AudioManager aparece.
    /// </summary>
    private void Start()
    {
        TocarMusica();
    }

    /// <summary>
    /// Toca a música de fundo, se ela existir e ainda não estiver tocando.
    /// </summary>
    public void TocarMusica()
    {
        if (musica == null || fonteMusica.isPlaying)
        {
            return;
        }
        fonteMusica.clip = musica;
        fonteMusica.Play();
    }

    /// <summary>
    /// Toca um efeito sonoro uma vez, sem interromper outros efeitos.
    /// </summary>
    /// <param name="clip">Som a tocar. Se for null, nada acontece.</param>
    public void TocarSom(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }
        fonteEfeitos.PlayOneShot(clip, volumeEfeitos);
    }

    /// <summary>Atalho para o som de queda no abismo.</summary>
    public void TocarQueda() => TocarSom(somQueda);

    /// <summary>Atalho para o som de impacto ao aterrissar.</summary>
    public void TocarImpacto() => TocarSom(somImpacto);

    /// <summary>Atalho para o som de restauração do ponto final.</summary>
    public void TocarRestauracao() => TocarSom(somRestauracao);
}
