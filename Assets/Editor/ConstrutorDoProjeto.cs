using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>
/// Monta o projeto do nível 1 a partir do GDD: tags, URP, materiais, prefabs,
/// Animator, cenas (menu, introdução e nível 1) e Build Settings.
/// Roda sozinho na primeira vez que o projeto é aberto e pode ser executado
/// de novo pelo menu "Semente da Aurora > Construir Projeto".
/// Materiais, prefabs e o Animator só são criados se ainda não existirem
/// (assim, modelos reais colocados nos prefabs não são apagados); as cenas
/// são sempre recriadas.
/// </summary>
[InitializeOnLoad]
public static class ConstrutorDoProjeto
{
    // ------------------------------------------------------------------
    // Caminhos
    // ------------------------------------------------------------------
    private const string PastaCenas = "Assets/Cenas";
    private const string PastaMateriais = "Assets/Materiais";
    private const string PastaPrefabs = "Assets/Prefabs";
    private const string PastaMudas = "Assets/Prefabs/Mudas";
    private const string PastaSettings = "Assets/Settings";
    private const string PastaTexturas = "Assets/Texturas";
    private const string PastaAnimacoes = "Assets/Animacoes";
    private const string PastaResources = "Assets/Resources";
    private const string PastaAudio = "Assets/Audio";

    private const string CenaMenu = PastaCenas + "/MenuInicial.unity";
    private const string CenaIntro = PastaCenas + "/Introducao.unity";
    private const string CenaNivel1 = PastaCenas + "/Nivel1.unity";

    private const string ChaveSessao = "SementeDaAurora.ConstrucaoAutomatica";

    // ------------------------------------------------------------------
    // Cores da interface
    // ------------------------------------------------------------------
    private static readonly Color CorPainel = new Color(0.12f, 0.09f, 0.06f, 0.85f);
    private static readonly Color CorBotao = new Color(0.42f, 0.29f, 0.16f, 1f);
    private static readonly Color CorTexto = new Color(0.96f, 0.91f, 0.78f);
    private static readonly Color CorTitulo = new Color(0.95f, 0.78f, 0.35f);

    // ------------------------------------------------------------------
    // Recursos criados durante a construção
    // ------------------------------------------------------------------
    private static readonly Dictionary<string, Material> materiais = new Dictionary<string, Material>();
    private static readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private static GameObject[] prefabsMudas;
    private static Material matParticulaAlpha;
    private static Material matParticulaAditiva;
    private static Material ceuCinza;
    private static Material ceuRestaurado;
    private static RuntimeAnimatorController controllerBran;
    private static Font fonte;
    private static Sprite spriteUI;

    /// <summary>
    /// Agenda a verificação de primeira abertura para depois que o editor carregar.
    /// </summary>
    static ConstrutorDoProjeto()
    {
        EditorApplication.delayCall += VerificarPrimeiraAbertura;
    }

    /// <summary>
    /// Se as cenas ainda não existem, constrói o projeto automaticamente (uma vez por sessão).
    /// </summary>
    private static void VerificarPrimeiraAbertura()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        // Espera o editor terminar de compilar/importar
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += VerificarPrimeiraAbertura;
            return;
        }

        if (File.Exists(CenaNivel1) || SessionState.GetBool(ChaveSessao, false))
        {
            return;
        }

        SessionState.SetBool(ChaveSessao, true);
        Debug.Log("[Semente da Aurora] Primeira abertura: construindo cenas, prefabs e materiais...");
        Construir();
    }

    /// <summary>
    /// Item de menu para reconstruir o projeto.
    /// </summary>
    [MenuItem("Semente da Aurora/Construir Projeto")]
    public static void ConstruirPeloMenu()
    {
        bool confirmar = EditorUtility.DisplayDialog(
            "Construir Projeto",
            "As cenas MenuInicial, Introducao e Nivel1 serão recriadas (alterações feitas nelas serão perdidas).\n\n" +
            "Materiais, prefabs e o Animator que já existem são mantidos.",
            "Construir", "Cancelar");

        if (confirmar && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Construir();
        }
    }

    /// <summary>
    /// Abre a cena do nível 1.
    /// </summary>
    [MenuItem("Semente da Aurora/Abrir Nível 1")]
    public static void AbrirNivel1()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(CenaNivel1);
        }
    }

    /// <summary>
    /// Executa todas as etapas da construção.
    /// </summary>
    private static void Construir()
    {
        try
        {
            materiais.Clear();
            prefabs.Clear();
            fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            spriteUI = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            Progresso("Pastas e configurações", 0.05f);
            CriarPastas();
            ConfigurarTags();
            ConfigurarPlayerSettings();
            ConfigurarURP();

            // Cena vazia temporária para montar os prefabs
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Progresso("Materiais", 0.15f);
            CriarMateriais();
            CriarMateriaisParticula();
            CriarCeus();

            Progresso("Prefabs", 0.3f);
            CriarPrefabs();
            CriarAudioManager();
            CriarAnimatorController();

            Progresso("Cena: Menu inicial", 0.5f);
            CriarCenaMenu();

            Progresso("Cena: Introdução", 0.6f);
            CriarCenaIntroducao();

            Progresso("Cena: Nível 1", 0.75f);
            CriarCenaNivel1();

            Progresso("Build Settings", 0.95f);
            ConfigurarBuildSettings();
            AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(CenaMenu);
            Debug.Log("[Semente da Aurora] Projeto construído. Abra Assets/Cenas/MenuInicial e aperte Play.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Semente da Aurora] Erro ao construir o projeto: " + e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// Mostra a barra de progresso.
    /// </summary>
    /// <param name="etapa">Texto da etapa atual.</param>
    /// <param name="valor">Progresso de 0 a 1.</param>
    private static void Progresso(string etapa, float valor)
    {
        EditorUtility.DisplayProgressBar("Semente da Aurora", etapa, valor);
    }

    // ==================================================================
    // Configurações do projeto
    // ==================================================================

    /// <summary>
    /// Cria as pastas usadas pelo projeto.
    /// </summary>
    private static void CriarPastas()
    {
        string[] pastas =
        {
            PastaCenas, PastaMateriais, PastaPrefabs, PastaMudas, PastaSettings,
            PastaTexturas, PastaAnimacoes, PastaResources, PastaAudio
        };
        foreach (string pasta in pastas)
        {
            Directory.CreateDirectory(pasta);
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Adiciona as tags do GDD ao TagManager (Player já existe por padrão).
    /// </summary>
    private static void ConfigurarTags()
    {
        Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject so = new SerializedObject(tagManager);
        SerializedProperty tags = so.FindProperty("tags");

        foreach (string tag in new[] { "Plataforma", "Abismo", "Final", "Runa" })
        {
            bool existe = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    existe = true;
                    break;
                }
            }
            if (!existe)
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Nome do produto, empresa e espaço de cor linear (padrão do template URP).
    /// </summary>
    private static void ConfigurarPlayerSettings()
    {
        PlayerSettings.productName = "Semente da Aurora";
        PlayerSettings.companyName = "Grupo Semente da Aurora";
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }
    }

    /// <summary>
    /// Cria o asset do Universal Render Pipeline e o define em Graphics e Quality.
    /// </summary>
    private static void ConfigurarURP()
    {
        try
        {
            string caminhoAsset = PastaSettings + "/URP_Pipeline.asset";
            string caminhoRenderer = PastaSettings + "/URP_Renderer.asset";

            UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(caminhoAsset);
            if (urp == null)
            {
                UniversalRendererData dados = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(dados, caminhoRenderer);
                urp = UniversalRenderPipelineAsset.Create(dados);
                urp.shadowDistance = 120f;
                AssetDatabase.CreateAsset(urp, caminhoAsset);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = urp;

            // Usa o mesmo pipeline em todos os níveis de qualidade
            int nivelAtual = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(nivelAtual, false);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Semente da Aurora] Não foi possível configurar o URP automaticamente. " +
                             "Crie um em Assets > Create > Rendering > URP Asset e defina em Project Settings > Graphics.\n" + e);
        }
    }

    /// <summary>
    /// Coloca as três cenas no Build Settings, na ordem do fluxo do jogo.
    /// </summary>
    private static void ConfigurarBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(CenaMenu, true),
            new EditorBuildSettingsScene(CenaIntro, true),
            new EditorBuildSettingsScene(CenaNivel1, true)
        };
    }

    // ==================================================================
    // Materiais
    // ==================================================================

    /// <summary>
    /// Cria (ou carrega) os materiais URP/Lit usados nos placeholders.
    /// </summary>
    private static void CriarMateriais()
    {
        // Mundo destruído
        Mat("PedraSeca", new Color(0.42f, 0.40f, 0.38f));
        Mat("ChaoSeco", new Color(0.36f, 0.33f, 0.28f));
        Mat("MadeiraSeca", new Color(0.38f, 0.30f, 0.22f));
        Mat("MadeiraEscura", new Color(0.22f, 0.16f, 0.11f));
        Mat("Palha", new Color(0.50f, 0.43f, 0.30f));
        Mat("TelhadoEscuro", new Color(0.30f, 0.25f, 0.22f));
        Mat("RochaEscura", new Color(0.25f, 0.24f, 0.23f));
        Mat("Montanha", new Color(0.33f, 0.33f, 0.36f));
        Mat("TroncoSeco", new Color(0.32f, 0.28f, 0.25f));
        Mat("ArbustoSeco", new Color(0.45f, 0.38f, 0.25f));
        Mat("Metal", new Color(0.20f, 0.20f, 0.22f), 0.5f, 0.8f);
        Mat("EstandarteNegro", new Color(0.08f, 0.08f, 0.09f));
        Mat("Janela", new Color(0.08f, 0.07f, 0.06f));

        // Mundo restaurado
        Mat("PedraVerde", new Color(0.33f, 0.50f, 0.25f));
        Mat("MadeiraMusgo", new Color(0.35f, 0.45f, 0.22f));
        Mat("TroncoVivo", new Color(0.35f, 0.24f, 0.14f));
        Mat("FolhaVerde", new Color(0.25f, 0.55f, 0.20f));
        Mat("Agua", new Color(0.25f, 0.50f, 0.80f), 0.9f);

        // Flores
        Mat("FlorVermelha", new Color(0.85f, 0.15f, 0.15f));
        Mat("FlorAmarela", new Color(0.95f, 0.80f, 0.15f));
        Mat("FlorBranca", new Color(0.95f, 0.95f, 0.92f));
        Mat("FlorRoxa", new Color(0.55f, 0.30f, 0.75f));

        // Personagem
        Mat("CapaVerde", new Color(0.25f, 0.42f, 0.22f));
        Mat("Pele", new Color(0.85f, 0.68f, 0.55f));
        Mat("Linho", new Color(0.75f, 0.70f, 0.60f));
        Mat("Couro", new Color(0.35f, 0.22f, 0.12f));

        // Emissivos
        Mat("Semente", new Color(1f, 0.85f, 0.4f), 0.5f, 0f, new Color(2.5f, 2f, 0.8f));
        Mat("Dourado", new Color(0.9f, 0.7f, 0.2f), 0.6f, 0.8f, new Color(0.6f, 0.4f, 0.05f));
        Mat("RunaPedra", new Color(0.30f, 0.32f, 0.35f));
        Mat("RunaBrilho", new Color(0.3f, 0.9f, 1f), 0.5f, 0f, new Color(0.6f, 1.8f, 2f));
        Mat("RunaDourada", new Color(1f, 0.85f, 0.3f), 0.5f, 0f, new Color(2f, 1.5f, 0.3f));
        Mat("Fogo", new Color(1f, 0.5f, 0.1f), 0.2f, 0f, new Color(3f, 1.2f, 0.2f));
    }

    /// <summary>
    /// Cria um material URP/Lit, ou carrega se ele já existir.
    /// </summary>
    /// <param name="nome">Nome do material (também é o nome do arquivo).</param>
    /// <param name="cor">Cor base.</param>
    /// <param name="suavidade">Smoothness (0 a 1).</param>
    /// <param name="metalico">Metallic (0 a 1).</param>
    /// <param name="emissao">Cor de emissão (null para nenhuma).</param>
    /// <returns>O material.</returns>
    private static Material Mat(string nome, Color cor, float suavidade = 0.1f, float metalico = 0f, Color? emissao = null)
    {
        string caminho = $"{PastaMateriais}/{nome}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = nome };
            mat.SetColor("_BaseColor", cor);
            mat.color = cor;
            mat.SetFloat("_Smoothness", suavidade);
            mat.SetFloat("_Metallic", metalico);
            if (emissao.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissao.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, caminho);
        }
        materiais[nome] = mat;
        return mat;
    }

    /// <summary>Atalho para pegar um material já criado.</summary>
    private static Material M(string nome) => materiais[nome];

    /// <summary>
    /// Cria a textura redonda das partículas e os materiais de partícula (alpha e aditivo).
    /// </summary>
    private static void CriarMateriaisParticula()
    {
        // Textura: círculo branco com borda suave
        string caminhoTextura = PastaTexturas + "/Particula.png";
        if (!File.Exists(caminhoTextura))
        {
            const int tamanho = 64;
            Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
            for (int y = 0; y < tamanho; y++)
            {
                for (int x = 0; x < tamanho; x++)
                {
                    float dx = (x + 0.5f) / tamanho * 2f - 1f;
                    float dy = (y + 0.5f) / tamanho * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            File.WriteAllBytes(caminhoTextura, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(caminhoTextura);

            TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(caminhoTextura);
            importador.alphaIsTransparency = true;
            importador.wrapMode = TextureWrapMode.Clamp;
            importador.mipmapEnabled = false;
            importador.SaveAndReimport();
        }
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(caminhoTextura);

        matParticulaAlpha = MatParticula("ParticulaAlpha", textura, false);
        matParticulaAditiva = MatParticula("ParticulaAditiva", textura, true);
    }

    /// <summary>
    /// Cria um material transparente de partícula do URP.
    /// </summary>
    /// <param name="nome">Nome do material.</param>
    /// <param name="textura">Textura da partícula.</param>
    /// <param name="aditivo">Verdadeiro para mistura aditiva (brilho), falso para alpha.</param>
    /// <returns>O material.</returns>
    private static Material MatParticula(string nome, Texture2D textura, bool aditivo)
    {
        string caminho = $"{PastaMateriais}/{nome}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        mat = new Material(shader) { name = nome };
        mat.SetTexture("_BaseMap", textura);
        mat.mainTexture = textura;
        mat.SetColor("_BaseColor", Color.white);

        // Configura a transparência manualmente (o que o Inspector do URP faria)
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", aditivo ? 2f : 0f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)(aditivo ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, caminho);
        return mat;
    }

    /// <summary>
    /// Cria os dois céus procedurais: cinza (destruído) e azul (restaurado).
    /// </summary>
    private static void CriarCeus()
    {
        ceuCinza = Ceu("CeuCinza", new Color(0.42f, 0.40f, 0.38f), 2.4f, 0.85f);
        ceuRestaurado = Ceu("CeuRestaurado", new Color(0.45f, 0.6f, 0.85f), 1.0f, 1.3f);
    }

    /// <summary>
    /// Cria um material de skybox procedural.
    /// </summary>
    /// <param name="nome">Nome do material.</param>
    /// <param name="tom">Cor do céu.</param>
    /// <param name="atmosfera">Espessura da atmosfera (maior = mais nublado/avermelhado).</param>
    /// <param name="exposicao">Brilho do céu.</param>
    /// <returns>O material.</returns>
    private static Material Ceu(string nome, Color tom, float atmosfera, float exposicao)
    {
        string caminho = $"{PastaMateriais}/{nome}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (mat != null)
        {
            return mat;
        }

        mat = new Material(Shader.Find("Skybox/Procedural")) { name = nome };
        mat.SetColor("_SkyTint", tom);
        mat.SetColor("_GroundColor", new Color(0.2f, 0.19f, 0.18f));
        mat.SetFloat("_AtmosphereThickness", atmosfera);
        mat.SetFloat("_Exposure", exposicao);
        mat.SetFloat("_SunSize", 0.03f);
        AssetDatabase.CreateAsset(mat, caminho);
        return mat;
    }

    // ==================================================================
    // Prefabs
    // ==================================================================

    /// <summary>
    /// Cria os prefabs de flores/mudas e de objetos de cena.
    /// </summary>
    private static void CriarPrefabs()
    {
        // Vetor de mudas usado pelo SpawnerMudas
        prefabsMudas = new[]
        {
            Prefab(PastaMudas, "Muda_Tulipa", ConstruirTulipa),
            Prefab(PastaMudas, "Muda_Margarida", ConstruirMargarida),
            Prefab(PastaMudas, "Muda_Broto", ConstruirBroto),
            Prefab(PastaMudas, "Muda_ArbustoFlorido", ConstruirArbustoFlorido),
            Prefab(PastaMudas, "Muda_Lavanda", ConstruirLavanda)
        };

        Prefab(PastaPrefabs, "Casa", ConstruirCasa);
        Prefab(PastaPrefabs, "Moinho", () => ConstruirMoinho("Moinho", 6f, 3.2f, 1.2f, 1f));
        Prefab(PastaPrefabs, "Poco", ConstruirPoco);
        Prefab(PastaPrefabs, "Barril", ConstruirBarril);
        Prefab(PastaPrefabs, "Caixote", ConstruirCaixote);
        Prefab(PastaPrefabs, "Cerca", ConstruirCerca);
        Prefab(PastaPrefabs, "Tocha", ConstruirTocha);
        Prefab(PastaPrefabs, "Estandarte", ConstruirEstandarte);
        Prefab(PastaPrefabs, "Carroca", ConstruirCarroca);
        Prefab(PastaPrefabs, "ArvoreSeca", ConstruirArvoreSeca);
        Prefab(PastaPrefabs, "ArvoreVerde", ConstruirArvoreVerde);
        Prefab(PastaPrefabs, "ArbustoSeco", ConstruirArbustoSeco);
        Prefab(PastaPrefabs, "Rocha", ConstruirRocha);
        Prefab(PastaPrefabs, "PedraRunica", () => ConstruirPedraRunica(false));
        Prefab(PastaPrefabs, "PedraRunicaDourada", () => ConstruirPedraRunica(true));
    }

    /// <summary>
    /// Cria um prefab a partir de uma função de construção, ou carrega o existente.
    /// </summary>
    /// <param name="pasta">Pasta onde o prefab é salvo.</param>
    /// <param name="nome">Nome do prefab.</param>
    /// <param name="construir">Função que monta o objeto na cena.</param>
    /// <returns>O asset do prefab.</returns>
    private static GameObject Prefab(string pasta, string nome, Func<GameObject> construir)
    {
        string caminho = $"{pasta}/{nome}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
        if (prefab == null)
        {
            GameObject temp = construir();
            temp.name = nome;
            prefab = PrefabUtility.SaveAsPrefabAsset(temp, caminho);
            Object.DestroyImmediate(temp);
        }
        prefabs[nome] = prefab;
        return prefab;
    }

    /// <summary>
    /// Coloca uma instância de prefab na cena.
    /// </summary>
    /// <param name="nome">Nome do prefab criado em <see cref="CriarPrefabs"/>.</param>
    /// <param name="pai">Objeto pai.</param>
    /// <param name="posicao">Posição no mundo.</param>
    /// <param name="rotacaoY">Rotação em Y, em graus.</param>
    /// <param name="escala">Escala uniforme.</param>
    /// <returns>A instância criada.</returns>
    private static GameObject Colocar(string nome, Transform pai, Vector3 posicao, float rotacaoY = 0f, float escala = 1f)
    {
        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[nome], pai);
        inst.transform.SetPositionAndRotation(posicao, Quaternion.Euler(0f, rotacaoY, 0f));
        inst.transform.localScale = Vector3.one * escala;
        PrefabUtility.RecordPrefabInstancePropertyModifications(inst.transform);
        return inst;
    }

    /// <summary>
    /// Cria uma primitiva com posição, escala, rotação e material.
    /// </summary>
    /// <param name="tipo">Tipo de primitiva.</param>
    /// <param name="nome">Nome do objeto.</param>
    /// <param name="pai">Objeto pai (pode ser null).</param>
    /// <param name="pos">Posição local.</param>
    /// <param name="escala">Escala local.</param>
    /// <param name="mat">Material.</param>
    /// <param name="colisor">Se falso, remove o collider.</param>
    /// <param name="rot">Rotação local em graus (opcional).</param>
    /// <returns>O objeto criado.</returns>
    private static GameObject Prim(PrimitiveType tipo, string nome, Transform pai, Vector3 pos, Vector3 escala,
                                   Material mat, bool colisor = true, Vector3? rot = null)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        go.name = nome;
        if (pai != null)
        {
            go.transform.SetParent(pai, false);
        }
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot ?? Vector3.zero;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!colisor)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        return go;
    }

    /// <summary>
    /// Troca o CapsuleCollider de um cilindro achatado por um BoxCollider
    /// (a cápsula vira uma esfera quando o cilindro é mais largo que alto).
    /// </summary>
    /// <param name="go">Cilindro.</param>
    private static void ColisorCaixa(GameObject go)
    {
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
    }

    /// <summary>Cria um objeto vazio.</summary>
    private static GameObject Vazio(string nome, Transform pai = null, Vector3? pos = null)
    {
        GameObject go = new GameObject(nome);
        if (pai != null)
        {
            go.transform.SetParent(pai, false);
        }
        go.transform.localPosition = pos ?? Vector3.zero;
        return go;
    }

    // --- Mudas e flores (sem collider, para não atrapalhar o pulo) ---

    private static GameObject ConstruirTulipa()
    {
        GameObject raiz = Vazio("Muda_Tulipa");
        Prim(PrimitiveType.Cylinder, "Caule", raiz.transform, new Vector3(0, 0.2f, 0), new Vector3(0.04f, 0.2f, 0.04f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Sphere, "Folha", raiz.transform, new Vector3(0.05f, 0.1f, 0), new Vector3(0.12f, 0.05f, 0.06f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Sphere, "Flor", raiz.transform, new Vector3(0, 0.44f, 0), new Vector3(0.14f, 0.18f, 0.14f), M("FlorVermelha"), false);
        return raiz;
    }

    private static GameObject ConstruirMargarida()
    {
        GameObject raiz = Vazio("Muda_Margarida");
        Prim(PrimitiveType.Cylinder, "Caule", raiz.transform, new Vector3(0, 0.17f, 0), new Vector3(0.03f, 0.17f, 0.03f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Cylinder, "Petalas", raiz.transform, new Vector3(0, 0.35f, 0), new Vector3(0.24f, 0.01f, 0.24f), M("FlorBranca"), false);
        Prim(PrimitiveType.Sphere, "Miolo", raiz.transform, new Vector3(0, 0.37f, 0), new Vector3(0.08f, 0.05f, 0.08f), M("FlorAmarela"), false);
        return raiz;
    }

    private static GameObject ConstruirBroto()
    {
        GameObject raiz = Vazio("Muda_Broto");
        Prim(PrimitiveType.Cylinder, "Tronco", raiz.transform, new Vector3(0, 0.25f, 0), new Vector3(0.05f, 0.25f, 0.05f), M("TroncoVivo"), false);
        Prim(PrimitiveType.Sphere, "Copa1", raiz.transform, new Vector3(0, 0.55f, 0), new Vector3(0.3f, 0.3f, 0.3f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Sphere, "Copa2", raiz.transform, new Vector3(0.1f, 0.45f, 0.08f), new Vector3(0.2f, 0.2f, 0.2f), M("FolhaVerde"), false);
        return raiz;
    }

    private static GameObject ConstruirArbustoFlorido()
    {
        GameObject raiz = Vazio("Muda_ArbustoFlorido");
        Prim(PrimitiveType.Sphere, "Arbusto", raiz.transform, new Vector3(0, 0.15f, 0), new Vector3(0.5f, 0.35f, 0.5f), M("FolhaVerde"), false);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            Prim(PrimitiveType.Sphere, "Flor" + i, raiz.transform, new Vector3(Mathf.Cos(a) * 0.18f, 0.27f, Mathf.Sin(a) * 0.18f),
                 Vector3.one * 0.08f, i % 2 == 0 ? M("FlorAmarela") : M("FlorRoxa"), false);
        }
        return raiz;
    }

    private static GameObject ConstruirLavanda()
    {
        GameObject raiz = Vazio("Muda_Lavanda");
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(a) * 0.06f, 0f, Mathf.Sin(a) * 0.06f);
            Prim(PrimitiveType.Cylinder, "Haste" + i, raiz.transform, p + new Vector3(0, 0.15f, 0), new Vector3(0.02f, 0.15f, 0.02f), M("FolhaVerde"), false);
            Prim(PrimitiveType.Capsule, "Flor" + i, raiz.transform, p + new Vector3(0, 0.36f, 0), new Vector3(0.05f, 0.08f, 0.05f), M("FlorRoxa"), false);
        }
        return raiz;
    }

    // --- Construções e objetos de cena ---

    private static GameObject ConstruirCasa()
    {
        GameObject raiz = Vazio("Casa");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cube, "Paredes", t, new Vector3(0, 1.75f, 0), new Vector3(5f, 3.5f, 4f), M("PedraSeca"));
        Prim(PrimitiveType.Cube, "Telhado", t, new Vector3(0, 3.5f, 0), new Vector3(3.3f, 3.3f, 5.4f), M("Palha"), true, new Vector3(0, 90, 45));
        Prim(PrimitiveType.Cube, "Porta", t, new Vector3(0, 1.0f, 2.01f), new Vector3(1.1f, 2f, 0.1f), M("MadeiraEscura"), false);
        Prim(PrimitiveType.Cube, "Janela1", t, new Vector3(-1.6f, 2.0f, 2.01f), new Vector3(0.7f, 0.7f, 0.1f), M("Janela"), false);
        Prim(PrimitiveType.Cube, "Janela2", t, new Vector3(1.6f, 2.0f, 2.01f), new Vector3(0.7f, 0.7f, 0.1f), M("Janela"), false);
        Prim(PrimitiveType.Cube, "Viga", t, new Vector3(0, 3.45f, 2.05f), new Vector3(5.1f, 0.2f, 0.15f), M("MadeiraEscura"), false);
        Prim(PrimitiveType.Cube, "Chamine", t, new Vector3(1.6f, 4.6f, -0.8f), new Vector3(0.6f, 1.6f, 0.6f), M("RochaEscura"));
        return raiz;
    }

    /// <summary>
    /// Monta um moinho de vento com pás que giram por torque (Rigidbody + HingeJoint).
    /// A frente (pás) fica virada para +Z.
    /// </summary>
    /// <param name="nome">Nome do objeto.</param>
    /// <param name="alturaEixo">Altura do eixo das pás em relação à base.</param>
    /// <param name="comprimentoPa">Comprimento de cada pá.</param>
    /// <param name="profundidadePa">Largura da face superior da pá (onde o jogador pisa).</param>
    /// <param name="sentido">1 ou -1: sentido do giro.</param>
    /// <returns>O objeto raiz do moinho.</returns>
    private static GameObject ConstruirMoinho(string nome, float alturaEixo, float comprimentoPa, float profundidadePa, float sentido)
    {
        GameObject raiz = Vazio(nome);
        Transform t = raiz.transform;
        float alturaTorre = alturaEixo + 1f;

        Prim(PrimitiveType.Cylinder, "Torre", t, new Vector3(0, alturaTorre / 2f, 0), new Vector3(3f, alturaTorre / 2f, 3f), M("PedraSeca"));
        Prim(PrimitiveType.Sphere, "Teto", t, new Vector3(0, alturaTorre, 0), new Vector3(3.6f, 2.4f, 3.6f), M("Palha"), false);
        Prim(PrimitiveType.Cube, "Porta", t, new Vector3(0, 1f, 1.45f), new Vector3(1f, 2f, 0.2f), M("MadeiraEscura"), false);
        Prim(PrimitiveType.Cylinder, "Eixo", t, new Vector3(0, alturaEixo, 1.6f), new Vector3(0.25f, 1.2f, 0.25f), M("MadeiraEscura"), false, new Vector3(90, 0, 0));

        // Pás: corpo rígido preso ao mundo por uma dobradiça no eixo Z
        GameObject pas = Vazio("Pas", t, new Vector3(0, alturaEixo, 1.5f + profundidadePa / 2f + 0.4f));
        Rigidbody corpo = pas.AddComponent<Rigidbody>();
        corpo.mass = 200f;
        corpo.useGravity = false;
        corpo.interpolation = RigidbodyInterpolation.Interpolate;
        HingeJoint dobradica = pas.AddComponent<HingeJoint>();
        dobradica.axis = Vector3.forward;
        dobradica.anchor = Vector3.zero;
        MoinhoDeVento moinho = pas.AddComponent<MoinhoDeVento>();
        Definir(moinho, "sentido", p => p.floatValue = sentido);

        GameObject cubo = Prim(PrimitiveType.Cylinder, "Cubo", pas.transform, Vector3.zero,
                               new Vector3(1.4f, profundidadePa / 2f + 0.1f, 1.4f), M("MadeiraEscura"), true, new Vector3(90, 0, 0));
        cubo.tag = "Plataforma";

        for (int i = 0; i < 4; i++)
        {
            Quaternion r = Quaternion.Euler(0, 0, 45f + 90f * i);
            GameObject pa = Prim(PrimitiveType.Cube, "Pa" + i, pas.transform, r * new Vector3(0.6f + comprimentoPa / 2f, 0, 0),
                                 new Vector3(comprimentoPa, 0.6f, profundidadePa), M("MadeiraSeca"));
            pa.transform.localRotation = r;
            pa.tag = "Plataforma";
        }
        return raiz;
    }

    private static GameObject ConstruirPoco()
    {
        GameObject raiz = Vazio("Poco");
        Transform t = raiz.transform;
        GameObject borda = Prim(PrimitiveType.Cylinder, "Borda", t, new Vector3(0, 0.4f, 0), new Vector3(2f, 0.4f, 2f), M("PedraSeca"));
        ColisorCaixa(borda);
        Prim(PrimitiveType.Cylinder, "Fundo", t, new Vector3(0, 0.81f, 0), new Vector3(1.6f, 0.01f, 1.6f), M("Janela"), false);
        Prim(PrimitiveType.Cube, "Poste1", t, new Vector3(-0.9f, 1.4f, 0), new Vector3(0.15f, 2.2f, 0.15f), M("MadeiraEscura"));
        Prim(PrimitiveType.Cube, "Poste2", t, new Vector3(0.9f, 1.4f, 0), new Vector3(0.15f, 2.2f, 0.15f), M("MadeiraEscura"));
        Prim(PrimitiveType.Cube, "Teto", t, new Vector3(0, 2.6f, 0), new Vector3(1.6f, 1.6f, 2.4f), M("Palha"), false, new Vector3(0, 90, 45));
        Prim(PrimitiveType.Cylinder, "Sarilho", t, new Vector3(0, 1.9f, 0), new Vector3(0.12f, 0.9f, 0.12f), M("MadeiraSeca"), false, new Vector3(0, 0, 90));
        return raiz;
    }

    private static GameObject ConstruirBarril()
    {
        GameObject raiz = Vazio("Barril");
        Prim(PrimitiveType.Cylinder, "Corpo", raiz.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.5f, 0.8f), M("MadeiraSeca"));
        Prim(PrimitiveType.Cylinder, "Aro1", raiz.transform, new Vector3(0, 0.25f, 0), new Vector3(0.83f, 0.03f, 0.83f), M("Metal"), false);
        Prim(PrimitiveType.Cylinder, "Aro2", raiz.transform, new Vector3(0, 0.75f, 0), new Vector3(0.83f, 0.03f, 0.83f), M("Metal"), false);
        return raiz;
    }

    private static GameObject ConstruirCaixote()
    {
        GameObject raiz = Vazio("Caixote");
        Prim(PrimitiveType.Cube, "Caixa", raiz.transform, new Vector3(0, 0.45f, 0), Vector3.one * 0.9f, M("MadeiraSeca"));
        Prim(PrimitiveType.Cube, "Tabua", raiz.transform, new Vector3(0, 0.45f, 0), new Vector3(0.95f, 0.12f, 0.95f), M("MadeiraEscura"), false);
        return raiz;
    }

    private static GameObject ConstruirCerca()
    {
        GameObject raiz = Vazio("Cerca");
        Transform t = raiz.transform;
        for (int i = 0; i < 3; i++)
        {
            Prim(PrimitiveType.Cube, "Poste" + i, t, new Vector3(-1.4f + i * 1.4f, 0.55f, 0), new Vector3(0.15f, 1.1f, 0.15f), M("MadeiraEscura"));
        }
        Prim(PrimitiveType.Cube, "Trave1", t, new Vector3(0, 0.45f, 0), new Vector3(3f, 0.1f, 0.08f), M("MadeiraSeca"));
        Prim(PrimitiveType.Cube, "Trave2", t, new Vector3(0, 0.85f, 0), new Vector3(3f, 0.1f, 0.08f), M("MadeiraSeca"));
        return raiz;
    }

    private static GameObject ConstruirTocha()
    {
        GameObject raiz = Vazio("Tocha");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cylinder, "Haste", t, new Vector3(0, 0.9f, 0), new Vector3(0.12f, 0.9f, 0.12f), M("MadeiraEscura"));
        Prim(PrimitiveType.Cylinder, "Suporte", t, new Vector3(0, 1.85f, 0), new Vector3(0.25f, 0.08f, 0.25f), M("Metal"), false);
        Prim(PrimitiveType.Sphere, "Brasa", t, new Vector3(0, 1.95f, 0), Vector3.one * 0.2f, M("Fogo"), false);

        Light luz = Vazio("Luz", t, new Vector3(0, 2.2f, 0)).AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.6f, 0.25f);
        luz.range = 7f;
        luz.intensity = 2.5f;

        // Chama: partículas aditivas subindo
        ParticleSystem chama = NovaParticula("Chama", t, new Vector3(0, 2f, 0), matParticulaAditiva);
        ConfigurarParticula(chama, true, true, 0.6f, 0.6f, 0.3f, new Color(1f, 0.55f, 0.15f, 0.9f), 0f, 100);
        Emissao(chama, 18f);
        Forma(chama, ParticleSystemShapeType.Cone, 8f, 0.08f);
        chama.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
        TamanhoAoLongoDaVida(chama, 1f, 0f);
        return raiz;
    }

    private static GameObject ConstruirEstandarte()
    {
        GameObject raiz = Vazio("Estandarte");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cylinder, "Mastro", t, new Vector3(0, 1.75f, 0), new Vector3(0.12f, 1.75f, 0.12f), M("MadeiraEscura"));
        Prim(PrimitiveType.Cylinder, "Trave", t, new Vector3(0, 3.3f, 0.1f), new Vector3(0.06f, 0.6f, 0.06f), M("MadeiraEscura"), false, new Vector3(0, 0, 90));
        Prim(PrimitiveType.Cube, "Pano", t, new Vector3(0, 2.4f, 0.12f), new Vector3(1.1f, 1.8f, 0.04f), M("EstandarteNegro"), false);
        Prim(PrimitiveType.Cube, "Brasao", t, new Vector3(0, 2.6f, 0.15f), new Vector3(0.45f, 0.45f, 0.02f), M("Dourado"), false, new Vector3(0, 0, 45));
        return raiz;
    }

    private static GameObject ConstruirCarroca()
    {
        GameObject raiz = Vazio("Carroca");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cube, "Caixa", t, new Vector3(0, 0.9f, 0), new Vector3(1.6f, 0.6f, 2.6f), M("MadeiraSeca"));
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.9f : 0.9f;
            float z = i < 2 ? -0.8f : 0.8f;
            Prim(PrimitiveType.Cylinder, "Roda" + i, t, new Vector3(x, 0.5f, z), new Vector3(1f, 0.06f, 1f), M("MadeiraEscura"), false, new Vector3(0, 0, 90));
        }
        Prim(PrimitiveType.Cube, "Varal1", t, new Vector3(-0.4f, 0.7f, 2f), new Vector3(0.08f, 0.08f, 1.6f), M("MadeiraEscura"), false);
        Prim(PrimitiveType.Cube, "Varal2", t, new Vector3(0.4f, 0.7f, 2f), new Vector3(0.08f, 0.08f, 1.6f), M("MadeiraEscura"), false);
        return raiz;
    }

    private static GameObject ConstruirArvoreSeca()
    {
        GameObject raiz = Vazio("ArvoreSeca");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cylinder, "Tronco", t, new Vector3(0, 1.6f, 0), new Vector3(0.35f, 1.6f, 0.35f), M("TroncoSeco"), true, new Vector3(0, 0, 4));
        Prim(PrimitiveType.Cylinder, "Galho1", t, new Vector3(0.5f, 2.8f, 0), new Vector3(0.12f, 0.8f, 0.12f), M("TroncoSeco"), false, new Vector3(0, 0, -45));
        Prim(PrimitiveType.Cylinder, "Galho2", t, new Vector3(-0.45f, 2.5f, 0.2f), new Vector3(0.1f, 0.7f, 0.1f), M("TroncoSeco"), false, new Vector3(20, 0, 50));
        Prim(PrimitiveType.Cylinder, "Galho3", t, new Vector3(0.1f, 3.3f, -0.4f), new Vector3(0.08f, 0.6f, 0.08f), M("TroncoSeco"), false, new Vector3(-40, 0, 0));
        return raiz;
    }

    private static GameObject ConstruirArvoreVerde()
    {
        GameObject raiz = Vazio("ArvoreVerde");
        Transform t = raiz.transform;
        Prim(PrimitiveType.Cylinder, "Tronco", t, new Vector3(0, 1.6f, 0), new Vector3(0.4f, 1.6f, 0.4f), M("TroncoVivo"));
        Prim(PrimitiveType.Sphere, "Copa1", t, new Vector3(0, 3.8f, 0), new Vector3(2.8f, 2.4f, 2.8f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Sphere, "Copa2", t, new Vector3(0.9f, 3.2f, 0.4f), new Vector3(1.8f, 1.6f, 1.8f), M("FolhaVerde"), false);
        Prim(PrimitiveType.Sphere, "Copa3", t, new Vector3(-0.8f, 3.4f, -0.5f), new Vector3(1.9f, 1.7f, 1.9f), M("FolhaVerde"), false);
        return raiz;
    }

    private static GameObject ConstruirArbustoSeco()
    {
        GameObject raiz = Vazio("ArbustoSeco");
        Prim(PrimitiveType.Sphere, "Tufo1", raiz.transform, new Vector3(0, 0.2f, 0), new Vector3(0.9f, 0.45f, 0.8f), M("ArbustoSeco"), false);
        Prim(PrimitiveType.Sphere, "Tufo2", raiz.transform, new Vector3(0.4f, 0.15f, 0.3f), new Vector3(0.6f, 0.35f, 0.6f), M("ArbustoSeco"), false);
        return raiz;
    }

    private static GameObject ConstruirRocha()
    {
        GameObject raiz = Vazio("Rocha");
        Prim(PrimitiveType.Sphere, "Pedra", raiz.transform, new Vector3(0, 0.3f, 0), new Vector3(1.4f, 0.9f, 1.1f), M("RochaEscura"), true, new Vector3(10, 30, 5));
        Prim(PrimitiveType.Sphere, "Pedrinha", raiz.transform, new Vector3(0.7f, 0.15f, 0.4f), new Vector3(0.5f, 0.35f, 0.45f), M("PedraSeca"), false);
        return raiz;
    }

    /// <summary>
    /// Pedra rúnica: trigger (Tag Runa) com o script PedraRunica, pedra sólida
    /// e um símbolo brilhante. A dourada é a do segredo atrás do moinho.
    /// </summary>
    /// <param name="dourada">Se verdadeiro, usa os materiais dourados.</param>
    /// <returns>O objeto raiz.</returns>
    private static GameObject ConstruirPedraRunica(bool dourada)
    {
        GameObject raiz = Vazio(dourada ? "PedraRunicaDourada" : "PedraRunica");
        raiz.tag = "Runa";
        SphereCollider gatilho = raiz.AddComponent<SphereCollider>();
        gatilho.isTrigger = true;
        gatilho.radius = 2.8f;
        gatilho.center = new Vector3(0, 1f, 0);

        Transform t = raiz.transform;
        Prim(PrimitiveType.Cube, "Pedra", t, new Vector3(0, 0.8f, 0), new Vector3(0.8f, 1.6f, 0.4f),
             dourada ? M("Dourado") : M("RunaPedra"), true, new Vector3(0, 0, 3));
        Prim(PrimitiveType.Cube, "Simbolo", t, new Vector3(0, 1.0f, 0.21f), new Vector3(0.3f, 0.6f, 0.02f),
             dourada ? M("RunaDourada") : M("RunaBrilho"), false);
        Prim(PrimitiveType.Cube, "Simbolo2", t, new Vector3(0, 1.0f, -0.21f), new Vector3(0.3f, 0.6f, 0.02f),
             dourada ? M("RunaDourada") : M("RunaBrilho"), false);

        Light brilho = Vazio("Brilho", t, new Vector3(0, 1.2f, 0)).AddComponent<Light>();
        brilho.type = LightType.Point;
        brilho.color = dourada ? new Color(1f, 0.8f, 0.3f) : new Color(0.4f, 0.9f, 1f);
        brilho.range = 4f;
        brilho.intensity = 2f;
        brilho.enabled = false;

        PedraRunica runa = raiz.AddComponent<PedraRunica>();
        Definir(runa, "brilho", p => p.objectReferenceValue = brilho);
        return raiz;
    }

    /// <summary>
    /// Cria o prefab Resources/AudioManager com os sons de Assets/Audio.
    /// </summary>
    private static void CriarAudioManager()
    {
        string caminho = PastaResources + "/AudioManager.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(caminho) != null)
        {
            return;
        }

        GameObject temp = new GameObject("AudioManager");
        AudioManager audio = temp.AddComponent<AudioManager>();
        audio.musica = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaAudio + "/musica.wav");
        audio.somQueda = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaAudio + "/queda.wav");
        audio.somImpacto = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaAudio + "/impacto.wav");
        audio.somRestauracao = AssetDatabase.LoadAssetAtPath<AudioClip>(PastaAudio + "/restauracao.wav");
        PrefabUtility.SaveAsPrefabAsset(temp, caminho);
        Object.DestroyImmediate(temp);
    }

    /// <summary>
    /// Cria o Animator Controller do Bran com os estados do GDD (sem clipes;
    /// basta arrastar as animações do Mixamo para cada estado).
    /// </summary>
    private static void CriarAnimatorController()
    {
        string caminho = PastaAnimacoes + "/Bran.controller";
        controllerBran = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(caminho);
        if (controllerBran != null)
        {
            return;
        }

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(caminho);
        ctrl.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
        ctrl.AddParameter(new AnimatorControllerParameter
        {
            name = "NoChao",
            type = AnimatorControllerParameterType.Bool,
            defaultBool = true
        });
        ctrl.AddParameter("VelocidadeVertical", AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState parado = sm.AddState("Parado", new Vector3(300, 0));
        AnimatorState andando = sm.AddState("Andando", new Vector3(300, 100));
        AnimatorState correndo = sm.AddState("Correndo", new Vector3(300, 200));
        AnimatorState pulando = sm.AddState("Pulando", new Vector3(600, 50));
        AnimatorState caindo = sm.AddState("Caindo", new Vector3(600, 150));
        sm.defaultState = parado;

        // Locomoção no chão pela velocidade
        Transicao(parado, andando, AnimatorConditionMode.Greater, 0.1f, "Velocidade");
        Transicao(andando, parado, AnimatorConditionMode.Less, 0.1f, "Velocidade");
        Transicao(andando, correndo, AnimatorConditionMode.Greater, 5f, "Velocidade");
        Transicao(correndo, andando, AnimatorConditionMode.Less, 5f, "Velocidade");

        // Pulo e queda a partir de qualquer estado
        AnimatorStateTransition paraPulo = sm.AddAnyStateTransition(pulando);
        paraPulo.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
        paraPulo.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocidadeVertical");
        paraPulo.canTransitionToSelf = false;
        paraPulo.hasExitTime = false;
        paraPulo.duration = 0.1f;

        AnimatorStateTransition paraQueda = sm.AddAnyStateTransition(caindo);
        paraQueda.AddCondition(AnimatorConditionMode.IfNot, 0f, "NoChao");
        paraQueda.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocidadeVertical");
        paraQueda.canTransitionToSelf = false;
        paraQueda.hasExitTime = false;
        paraQueda.duration = 0.1f;

        // Aterrissar volta para "Parado" (e a locomoção assume dali)
        Transicao(caindo, parado, AnimatorConditionMode.If, 0f, "NoChao");
        Transicao(pulando, parado, AnimatorConditionMode.If, 0f, "NoChao");

        AssetDatabase.SaveAssets();
        controllerBran = ctrl;
    }

    /// <summary>
    /// Cria uma transição sem tempo de saída com uma condição.
    /// </summary>
    private static void Transicao(AnimatorState de, AnimatorState para, AnimatorConditionMode modo, float limite, string parametro)
    {
        AnimatorStateTransition tr = de.AddTransition(para);
        tr.AddCondition(modo, limite, parametro);
        tr.hasExitTime = false;
        tr.duration = 0.1f;
    }

    // ==================================================================
    // Partículas
    // ==================================================================

    /// <summary>
    /// Cria um objeto com ParticleSystem parado.
    /// </summary>
    private static ParticleSystem NovaParticula(string nome, Transform pai, Vector3 pos, Material mat)
    {
        GameObject go = Vazio(nome, pai, pos);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    /// <summary>
    /// Configura o módulo principal de um sistema de partículas.
    /// </summary>
    private static void ConfigurarParticula(ParticleSystem ps, bool loop, bool tocarAoIniciar, float vida, float velocidade,
                                            float tamanho, Color cor, float gravidade, int maximo)
    {
        ParticleSystem.MainModule main = ps.main;
        main.duration = loop ? 5f : 1f;
        main.loop = loop;
        main.playOnAwake = tocarAoIniciar;
        main.startLifetime = vida;
        main.startSpeed = velocidade;
        main.startSize = tamanho;
        main.startColor = cor;
        main.gravityModifier = gravidade;
        main.maxParticles = maximo;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    /// <summary>Define a emissão contínua (partículas por segundo).</summary>
    private static void Emissao(ParticleSystem ps, float porSegundo)
    {
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = porSegundo;
    }

    /// <summary>Define uma emissão única (explosão) de várias partículas.</summary>
    private static void Rajada(ParticleSystem ps, short quantidade)
    {
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, quantidade) });
    }

    /// <summary>Define a forma de emissão.</summary>
    private static void Forma(ParticleSystem ps, ParticleSystemShapeType tipo, float angulo, float raio)
    {
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = tipo;
        forma.angle = angulo;
        forma.radius = raio;
    }

    /// <summary>Faz o tamanho variar linearmente durante a vida da partícula.</summary>
    private static void TamanhoAoLongoDaVida(ParticleSystem ps, float inicio, float fim)
    {
        ParticleSystem.SizeOverLifetimeModule tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, inicio, 1f, fim));
    }

    /// <summary>Faz a partícula sumir aos poucos (alpha de 1 para 0).</summary>
    private static void SumirAoLongoDaVida(ParticleSystem ps)
    {
        ParticleSystem.ColorOverLifetimeModule cor = ps.colorOverLifetime;
        cor.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        cor.color = g;
    }

    // ==================================================================
    // Ambiente e interface (compartilhados pelas cenas)
    // ==================================================================

    /// <summary>
    /// Configura céu, luz ambiente e névoa do mundo destruído e cria o sol.
    /// </summary>
    /// <returns>A luz direcional (sol).</returns>
    private static Light ConfigurarAmbienteCinza()
    {
        RenderSettings.skybox = ceuCinza;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.40f, 0.38f, 0.36f);
        RenderSettings.ambientEquatorColor = new Color(0.32f, 0.30f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.14f, 0.13f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.42f, 0.40f, 0.38f);
        RenderSettings.fogDensity = 0.012f;

        GameObject objSol = Vazio("Sol");
        objSol.transform.rotation = Quaternion.Euler(40f, -40f, 0f);
        Light sol = objSol.AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.color = new Color(0.78f, 0.70f, 0.62f);
        sol.intensity = 1.1f;
        sol.shadows = LightShadows.Soft;
        RenderSettings.sun = sol;
        return sol;
    }

    /// <summary>
    /// Cria a câmera principal.
    /// </summary>
    private static Camera CriarCamera(Vector3 pos, Vector3 rot)
    {
        GameObject obj = Vazio("Main Camera");
        obj.tag = "MainCamera";
        obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(rot));
        Camera cam = obj.AddComponent<Camera>();
        cam.farClipPlane = 1000f;
        cam.nearClipPlane = 0.1f;
        obj.AddComponent<AudioListener>();
        return cam;
    }

    /// <summary>
    /// Cria um Canvas em tela cheia que se adapta à resolução.
    /// </summary>
    private static Canvas CriarCanvas(string nome)
    {
        GameObject obj = new GameObject(nome);
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler escala = obj.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        obj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// Cria o EventSystem (necessário para os botões).
    /// </summary>
    private static void CriarEventSystem()
    {
        GameObject obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
    }

    /// <summary>
    /// Cria um RectTransform filho, ancorado num ponto da tela.
    /// </summary>
    /// <param name="nome">Nome do objeto.</param>
    /// <param name="pai">Pai na interface.</param>
    /// <param name="ancora">Ponto de âncora (0,0 = canto inferior esquerdo; 1,1 = superior direito).</param>
    /// <param name="pos">Posição relativa à âncora.</param>
    /// <param name="tamanho">Tamanho em pixels de referência (Vector2.zero + âncora null = tela toda).</param>
    /// <returns>O RectTransform.</returns>
    private static RectTransform NovoRect(string nome, Transform pai, Vector2? ancora, Vector2 pos, Vector2 tamanho)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        RectTransform r = (RectTransform)go.transform;
        if (ancora.HasValue)
        {
            r.anchorMin = r.anchorMax = r.pivot = ancora.Value;
            r.anchoredPosition = pos;
            r.sizeDelta = tamanho;
        }
        else
        {
            // Sem âncora: ocupa todo o pai
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
        return r;
    }

    /// <summary>Cria um painel (imagem colorida).</summary>
    private static Image CriarPainel(string nome, Transform pai, Vector2? ancora, Vector2 pos, Vector2 tamanho, Color cor)
    {
        Image img = NovoRect(nome, pai, ancora, pos, tamanho).gameObject.AddComponent<Image>();
        img.sprite = spriteUI;
        img.type = Image.Type.Sliced;
        img.color = cor;
        return img;
    }

    /// <summary>Cria um texto com sombra.</summary>
    private static Text CriarTexto(string nome, Transform pai, string conteudo, int tamanhoFonte, Vector2? ancora, Vector2 pos,
                                   Vector2 tamanho, TextAnchor alinhamento, Color cor, FontStyle estilo = FontStyle.Normal)
    {
        RectTransform r = NovoRect(nome, pai, ancora, pos, tamanho);
        Text t = r.gameObject.AddComponent<Text>();
        t.font = fonte;
        t.text = conteudo;
        t.fontSize = tamanhoFonte;
        t.fontStyle = estilo;
        t.alignment = alinhamento;
        t.color = cor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        Shadow sombra = r.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.7f);
        sombra.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    /// <summary>Cria um botão com texto e liga o clique a um método.</summary>
    private static Button CriarBotao(Transform pai, string rotulo, Vector2 ancora, Vector2 pos, Vector2 tamanho, UnityAction acao)
    {
        Image img = CriarPainel("Botao" + rotulo.Replace(" ", ""), pai, ancora, pos, tamanho, CorBotao);
        Button botao = img.gameObject.AddComponent<Button>();
        ColorBlock cores = botao.colors;
        cores.highlightedColor = new Color(1.25f, 1.15f, 0.95f);
        cores.pressedColor = new Color(0.75f, 0.7f, 0.6f);
        botao.colors = cores;
        CriarTexto("Texto", img.transform, rotulo, 36, null, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, CorTexto, FontStyle.Bold);
        UnityEventTools.AddPersistentListener(botao.onClick, acao);
        return botao;
    }

    /// <summary>
    /// Altera um campo serializado (inclusive privado) de um componente.
    /// </summary>
    /// <param name="alvo">Componente.</param>
    /// <param name="campo">Nome do campo.</param>
    /// <param name="alterar">Função que altera a propriedade.</param>
    private static void Definir(Object alvo, string campo, Action<SerializedProperty> alterar)
    {
        SerializedObject so = new SerializedObject(alvo);
        SerializedProperty prop = so.FindProperty(campo);
        if (prop == null)
        {
            Debug.LogWarning($"[Semente da Aurora] Campo '{campo}' não encontrado em {alvo.GetType().Name}.");
            return;
        }
        alterar(prop);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Atalho para definir uma referência a objeto.</summary>
    private static void Ref(Object alvo, string campo, Object valor)
    {
        Definir(alvo, campo, p => p.objectReferenceValue = valor);
    }

    /// <summary>Atalho para definir um vetor de referências a objetos.</summary>
    private static void RefArray(Object alvo, string campo, IList<GameObject> valores)
    {
        Definir(alvo, campo, p =>
        {
            p.arraySize = valores.Count;
            for (int i = 0; i < valores.Count; i++)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            }
        });
    }

    // ==================================================================
    // Cena: Menu inicial
    // ==================================================================

    /// <summary>
    /// Menu inicial: vila do nível 1 ao fundo, título e botões "Jogar" e "Sair".
    /// </summary>
    private static void CriarCenaMenu()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigurarAmbienteCinza();

        // Vila ao fundo
        Transform vila = Vazio("Vila").transform;
        GameObject chao = Prim(PrimitiveType.Cube, "Chao", vila, new Vector3(0, -1.5f, 0), new Vector3(40f, 3f, 40f), M("ChaoSeco"));
        chao.isStatic = true;
        Colocar("Casa", vila, new Vector3(-6, 0, 5), 160f);
        Colocar("Casa", vila, new Vector3(3, 0, 7), 190f);
        Colocar("Casa", vila, new Vector3(8, 0, -2), 250f);
        Colocar("Moinho", vila, new Vector3(-9, 0, -6), 40f);
        Colocar("Poco", vila, new Vector3(0, 0, 0));
        Colocar("ArvoreSeca", vila, new Vector3(-3, 0, -5), 20f);
        Colocar("ArvoreSeca", vila, new Vector3(6, 0, 6), 110f);
        Colocar("Tocha", vila, new Vector3(-2, 0, 2.5f));
        Colocar("Tocha", vila, new Vector3(2.5f, 0, -2));
        Colocar("Barril", vila, new Vector3(-3.5f, 0, 2));
        Colocar("Estandarte", vila, new Vector3(4, 0, 2), 200f);

        Camera cam = CriarCamera(new Vector3(16f, 7f, -16f), Vector3.zero);
        CameraOrbita orbita = cam.gameObject.AddComponent<CameraOrbita>();
        Definir(orbita, "centro", p => p.vector3Value = new Vector3(0f, 2f, 0f));
        cam.transform.LookAt(new Vector3(0f, 2f, 0f));

        // Interface
        Canvas canvas = CriarCanvas("Interface");
        MenuInicial menu = canvas.gameObject.AddComponent<MenuInicial>();
        Transform c = canvas.transform;
        CriarTexto("Titulo", c, "Semente da Aurora", 110, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 160),
                   TextAnchor.MiddleCenter, CorTitulo, FontStyle.Bold);
        CriarTexto("Subtitulo", c, "Escale as ruínas do reino e devolva a vida à terra", 40, new Vector2(0.5f, 0.5f),
                   new Vector2(0, 160), new Vector2(1600, 60), TextAnchor.MiddleCenter, CorTexto, FontStyle.Italic);
        CriarBotao(c, "Jogar", new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(380, 90), menu.BotaoJogar);
        CriarBotao(c, "Sair", new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(380, 90), menu.BotaoSair);
        CriarEventSystem();

        EditorSceneManager.SaveScene(cena, CenaMenu);
    }

    // ==================================================================
    // Cena: Introdução
    // ==================================================================

    /// <summary>
    /// Introdução (Cena 1): cabana de Ysolde ao amanhecer com a semente
    /// brilhando sobre a mesa, e as falas com o botão "Continuar".
    /// </summary>
    private static void CriarCenaIntroducao()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.14f, 0.11f);
        RenderSettings.fog = false;

        // Cabana
        Transform cabana = Vazio("Cabana").transform;
        Prim(PrimitiveType.Cube, "Piso", cabana, new Vector3(0, -0.05f, 0), new Vector3(8f, 0.1f, 8f), M("MadeiraEscura"));
        Prim(PrimitiveType.Cube, "ParedeFundo", cabana, new Vector3(0, 2f, 3f), new Vector3(8f, 4f, 0.2f), M("MadeiraSeca"));
        Prim(PrimitiveType.Cube, "ParedeEsq", cabana, new Vector3(-3f, 2f, 0), new Vector3(0.2f, 4f, 8f), M("MadeiraSeca"));
        Prim(PrimitiveType.Cube, "Janela", cabana, new Vector3(1.6f, 2.3f, 2.88f), new Vector3(1.2f, 1f, 0.05f), M("FlorAmarela"));
        Prim(PrimitiveType.Cube, "Mesa", cabana, new Vector3(0, 0.75f, 0.5f), new Vector3(2.2f, 0.1f, 1.2f), M("MadeiraSeca"));
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.95f : 0.95f;
            float z = i < 2 ? 0.0f : 1.0f;
            Prim(PrimitiveType.Cube, "Perna" + i, cabana, new Vector3(x, 0.35f, z), new Vector3(0.1f, 0.7f, 0.1f), M("MadeiraEscura"));
        }
        Prim(PrimitiveType.Sphere, "SementeDaAurora", cabana, new Vector3(0, 0.92f, 0.5f), Vector3.one * 0.22f, M("Semente"));
        Prim(PrimitiveType.Cylinder, "Cajado", cabana, new Vector3(-2.2f, 1.2f, 2.2f), new Vector3(0.08f, 1.2f, 0.08f), M("TroncoVivo"), true, new Vector3(0, 0, 8));
        Prim(PrimitiveType.Sphere, "FolhasCajado", cabana, new Vector3(-2.05f, 2.4f, 2.2f), Vector3.one * 0.3f, M("FolhaVerde"));

        Light luzSemente = Vazio("LuzSemente", cabana, new Vector3(0, 1.1f, 0.5f)).AddComponent<Light>();
        luzSemente.type = LightType.Point;
        luzSemente.color = new Color(1f, 0.85f, 0.45f);
        luzSemente.range = 5f;
        luzSemente.intensity = 3f;

        Light amanhecer = Vazio("LuzAmanhecer", cabana, new Vector3(1.6f, 2.3f, 2.5f)).AddComponent<Light>();
        amanhecer.type = LightType.Spot;
        amanhecer.transform.rotation = Quaternion.Euler(35f, 200f, 0f);
        amanhecer.color = new Color(1f, 0.7f, 0.45f);
        amanhecer.range = 10f;
        amanhecer.spotAngle = 60f;
        amanhecer.intensity = 6f;

        Camera cam = CriarCamera(new Vector3(0.6f, 1.6f, -2.2f), new Vector3(18f, -8f, 0f));
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.04f, 0.03f);

        // Interface
        Canvas canvas = CriarCanvas("Interface");
        TelaIntroducao intro = canvas.gameObject.AddComponent<TelaIntroducao>();
        Transform c = canvas.transform;
        CriarTexto("Local", c, "Cabana de Ysolde, ao amanhecer", 34, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1200, 60),
                   TextAnchor.MiddleCenter, CorTitulo, FontStyle.Italic);
        Image painel = CriarPainel("PainelFala", c, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1500, 300), CorPainel);
        Text fala = CriarTexto("TextoFala", painel.transform, "", 36, new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(1400, 170),
                               TextAnchor.UpperLeft, CorTexto);
        CriarBotao(painel.transform, "Continuar", new Vector2(1f, 0f), new Vector2(-25, 25), new Vector2(320, 80), intro.BotaoContinuar);
        Ref(intro, "textoFala", fala);
        CriarEventSystem();

        EditorSceneManager.SaveScene(cena, CenaIntro);
    }

    // ==================================================================
    // Cena: Nível 1 — Vale das Cinzas
    // ==================================================================

    /// <summary>
    /// Nível 1 com os 5 trechos do GDD: Praça da Vila, Ponte Partida,
    /// Colina dos Moinhos, Chaminé da Forja e Nascente do Vale.
    /// O caminho vai de -X para +X, sempre subindo.
    /// </summary>
    private static void CriarCenaNivel1()
    {
        Scene cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Random.InitState(2026);
        Light sol = ConfigurarAmbienteCinza();

        // Spawner de mudas com o vetor de prefabs
        GameObject objSpawner = Vazio("SpawnerMudas");
        SpawnerMudas spawner = objSpawner.AddComponent<SpawnerMudas>();
        spawner.prefabsMudas = prefabsMudas;
        Ref(spawner, "grupoMudas", Vazio("MudasCriadas", objSpawner.transform).transform);

        // Abismo: trigger gigante abaixo de todo o nível
        GameObject abismo = Vazio("Abismo", null, new Vector3(40f, -18f, 0f));
        abismo.tag = "Abismo";
        BoxCollider caixaAbismo = abismo.AddComponent<BoxCollider>();
        caixaAbismo.isTrigger = true;
        caixaAbismo.size = new Vector3(600f, 4f, 600f);
        abismo.AddComponent<ZonaAbismo>();

        // Trechos do nível
        CriarTrechoPraca(spawner, out RodaDagua roda);
        CriarTrechoPonte(spawner);
        CriarTrechoMoinhos(spawner);
        ParticleSystem fumaca = CriarTrechoForja(spawner);
        CriarTrechoNascente(spawner, sol, fumaca, roda);
        CriarCenarioDeFundo();

        // Jogador e câmera
        GameObject bran = CriarJogador(new Vector3(-17.5f, 0.05f, 0f));
        Camera cam = CriarCamera(new Vector3(-23.5f, 2.5f, 0f), new Vector3(15f, 90f, 0f));
        CameraTerceiraPessoa camTp = cam.gameObject.AddComponent<CameraTerceiraPessoa>();
        Ref(camTp, "alvo", bran.transform);
        Ref(bran.GetComponent<PlayerController>(), "cameraReferencia", cam.transform);

        CriarInterfaceNivel();
        CriarEventSystem();

        EditorSceneManager.SaveScene(cena, CenaNivel1);
    }

    /// <summary>
    /// Cria uma plataforma seca que refloresce (Tag Plataforma).
    /// </summary>
    /// <param name="pai">Objeto que agrupa o trecho.</param>
    /// <param name="nome">Nome da plataforma.</param>
    /// <param name="topo">Centro da face de cima, no mundo.</param>
    /// <param name="tamanho">Tamanho (largura, espessura, profundidade).</param>
    /// <param name="madeira">Madeira (tábuas) ou pedra.</param>
    /// <param name="spawner">Spawner de mudas.</param>
    /// <returns>A plataforma.</returns>
    private static GameObject CriarPlataforma(Transform pai, string nome, Vector3 topo, Vector3 tamanho, bool madeira, SpawnerMudas spawner)
    {
        GameObject plat = Prim(PrimitiveType.Cube, nome, pai, topo - new Vector3(0, tamanho.y / 2f, 0), tamanho,
                               madeira ? M("MadeiraSeca") : M("PedraSeca"));
        plat.tag = "Plataforma";
        plat.isStatic = true;
        PlataformaReflorescente refl = plat.AddComponent<PlataformaReflorescente>();
        refl.materialVerde = madeira ? M("MadeiraMusgo") : M("PedraVerde");
        refl.spawner = spawner;

        float baseY = topo.y - tamanho.y;
        if (madeira)
        {
            // Vigas de apoio embaixo das tábuas
            Prim(PrimitiveType.Cube, nome + "_Viga1", pai, new Vector3(topo.x, baseY - 0.1f, topo.z - tamanho.z * 0.3f),
                 new Vector3(tamanho.x * 1.1f, 0.2f, 0.25f), M("MadeiraEscura"), false);
            Prim(PrimitiveType.Cube, nome + "_Viga2", pai, new Vector3(topo.x, baseY - 0.1f, topo.z + tamanho.z * 0.3f),
                 new Vector3(tamanho.x * 1.1f, 0.2f, 0.25f), M("MadeiraEscura"), false);
        }
        else
        {
            // Bloco de rocha flutuante embaixo da pedra (só visual)
            Prim(PrimitiveType.Sphere, nome + "_Rocha", pai, new Vector3(topo.x, baseY - tamanho.x * 0.3f, topo.z),
                 new Vector3(tamanho.x * 0.95f, tamanho.x * 0.8f, tamanho.z * 0.95f), M("RochaEscura"), false);
        }
        return plat;
    }

    /// <summary>
    /// Coloca uma pedra rúnica com o texto informado.
    /// </summary>
    private static void ColocarRuna(Transform pai, Vector3 pos, float rotY, string texto, bool dourada = false)
    {
        GameObject runa = Colocar(dourada ? "PedraRunicaDourada" : "PedraRunica", pai, pos, rotY);
        Definir(runa.GetComponent<PedraRunica>(), "texto", p => p.stringValue = texto);
    }

    /// <summary>
    /// Trecho 1 — Praça da Vila: chão firme, casas, objetos de cena e as
    /// pedras rúnicas do tutorial. Sem risco de queda.
    /// </summary>
    private static void CriarTrechoPraca(SpawnerMudas spawner, out RodaDagua roda)
    {
        Transform t = Vazio("1_PracaDaVila").transform;

        GameObject chao = Prim(PrimitiveType.Cube, "ChaoPraca", t, new Vector3(-5f, -1.5f, 0f), new Vector3(30f, 3f, 30f), M("ChaoSeco"));
        chao.tag = "Plataforma";
        chao.isStatic = true;
        Prim(PrimitiveType.Sphere, "ChaoPraca_Rocha", t, new Vector3(-5f, -6f, 0f), new Vector3(30f, 12f, 30f), M("RochaEscura"), false);

        // Casas (a da roda d'água fica na beira do rio seco)
        Colocar("Casa", t, new Vector3(-16f, 0, 11f), 180f);
        Colocar("Casa", t, new Vector3(-9f, 0, 12f), 180f);
        Colocar("Casa", t, new Vector3(-2f, 0, 11f), 180f);
        Colocar("Casa", t, new Vector3(5f, 0, 11f), 180f);
        Colocar("Casa", t, new Vector3(-6f, 0, -12f), 0f);
        Colocar("Casa", t, new Vector3(3f, 0, -11f), 0f);
        Colocar("Moinho", t, new Vector3(-15f, 0, -11f), 30f);

        Colocar("Poco", t, new Vector3(-5f, 0, 5f));
        Colocar("Carroca", t, new Vector3(-10f, 0, 6.5f), 30f);

        Colocar("Barril", t, new Vector3(-12.5f, 0, 8f));
        Colocar("Barril", t, new Vector3(-11.6f, 0, 8.5f));
        Colocar("Barril", t, new Vector3(4.5f, 0, -7.5f));
        Colocar("Barril", t, new Vector3(-1.5f, 0, 8f));
        Colocar("Caixote", t, new Vector3(-13f, 0, -7f), 10f);
        Colocar("Caixote", t, new Vector3(-12f, 0, -7.2f), -15f);
        Colocar("Caixote", t, new Vector3(-12.5f, 0.9f, -7.1f), 25f);
        Colocar("Caixote", t, new Vector3(6f, 0, -7f), 40f);

        Colocar("Tocha", t, new Vector3(-18f, 0, 3f));
        Colocar("Tocha", t, new Vector3(-18f, 0, -3f));
        Colocar("Tocha", t, new Vector3(8.5f, 0, 3f));
        Colocar("Tocha", t, new Vector3(8.5f, 0, -3f));

        Colocar("Estandarte", t, new Vector3(-19.3f, 0, 0f), 90f);
        Colocar("Estandarte", t, new Vector3(-9f, 0, -8f), 0f);
        Colocar("Estandarte", t, new Vector3(1f, 0, 8f), 180f);

        // Cerca na beira da praça, com uma abertura no caminho
        foreach (float z in new[] { -13f, -10f, -7f, -4.3f, 4.3f, 7f })
        {
            Colocar("Cerca", t, new Vector3(9.5f, 0, z), 90f);
        }

        Colocar("ArvoreSeca", t, new Vector3(-18.5f, 0, 14f), 30f);
        Colocar("ArvoreSeca", t, new Vector3(7.5f, 0, -13.5f), 200f);
        Colocar("ArbustoSeco", t, new Vector3(-17f, 0, 7f), 0f);
        Colocar("ArbustoSeco", t, new Vector3(-3f, 0, -7f), 70f);
        Colocar("ArbustoSeco", t, new Vector3(6.5f, 0, 6f), 140f);
        Colocar("ArbustoSeco", t, new Vector3(-14f, 0, -4.5f), 200f);
        Colocar("Rocha", t, new Vector3(-19f, 0, -8f), 0f);
        Colocar("Rocha", t, new Vector3(7.5f, 0, 9.5f), 120f);

        // Pedras rúnicas do tutorial (textos do GDD)
        ColocarRuna(t, new Vector3(-15.5f, 0, -2.5f), -20f, "Use W, A, S, D para andar.");
        ColocarRuna(t, new Vector3(-11f, 0, 2.5f), 200f, "Mova o mouse para olhar ao redor.");
        ColocarRuna(t, new Vector3(-6.5f, 0, -2.5f), -20f, "Pressione Espaço para pular.");
        ColocarRuna(t, new Vector3(-1.5f, 0, 2.5f), 200f, "Segure Shift para correr. Correr antes de pular faz você ir mais longe.");
        ColocarRuna(t, new Vector3(6.5f, 0, -2.5f), -20f, "Cuidado: se cair no abismo, você recomeça do vale.");

        // Roda d'água parada na beira do rio seco
        roda = CriarRodaDagua(t, new Vector3(10.9f, 0.6f, 11f));
    }

    /// <summary>
    /// Roda d'água: a parte que gira (RodaDagua) e a água que cai nela quando
    /// a Nascente é despertada.
    /// </summary>
    private static RodaDagua CriarRodaDagua(Transform pai, Vector3 pos)
    {
        Transform grupo = Vazio("RodaDagua", pai, pos).transform;
        const float raio = 2.2f;

        GameObject objRoda = Vazio("Roda", grupo);
        Transform r = objRoda.transform;
        Prim(PrimitiveType.Cylinder, "Eixo", r, Vector3.zero, new Vector3(0.6f, 0.45f, 0.6f), M("MadeiraEscura"), false, new Vector3(0, 0, 90));
        for (int k = 0; k < 8; k++)
        {
            float a = k * 45f;
            Quaternion q = Quaternion.Euler(a, 0, 0);
            GameObject aro = Prim(PrimitiveType.Cube, "Aro" + k, r, q * new Vector3(0, 0, raio), new Vector3(0.5f, 1.8f, 0.15f), M("MadeiraSeca"), false);
            aro.transform.localRotation = q;
            GameObject pa = Prim(PrimitiveType.Cube, "Pa" + k, r, q * new Vector3(0, 0, raio + 0.35f), new Vector3(0.6f, 0.1f, 0.7f), M("MadeiraEscura"), false);
            pa.transform.localRotation = q;
            if (k % 2 == 0)
            {
                GameObject raioRoda = Prim(PrimitiveType.Cylinder, "Raio" + k, r, q * new Vector3(0, 0, raio / 2f),
                                           new Vector3(0.12f, raio / 2f, 0.12f), M("MadeiraEscura"), false);
                raioRoda.transform.localRotation = Quaternion.Euler(a + 90f, 0, 0);
            }
        }
        RodaDagua roda = objRoda.AddComponent<RodaDagua>();

        // Calha de madeira por cima e água (desligada) caindo nas pás
        Prim(PrimitiveType.Cube, "Calha", grupo, new Vector3(0, raio + 1.2f, -1.5f), new Vector3(0.6f, 0.2f, 3.5f), M("MadeiraSeca"), false);
        ParticleSystem agua = NovaParticula("AguaRoda", grupo, new Vector3(0, raio + 1.1f, 0.2f), matParticulaAlpha);
        ConfigurarParticula(agua, true, false, 1.2f, 0.5f, 0.25f, new Color(0.55f, 0.75f, 1f, 0.8f), 1f, 300);
        Emissao(agua, 50f);
        Forma(agua, ParticleSystemShapeType.Cone, 5f, 0.2f);
        agua.transform.rotation = Quaternion.Euler(90f, 0, 0);
        Ref(roda, "agua", agua);
        return roda;
    }

    /// <summary>
    /// Trecho 2 — Ponte Partida: plataformas largas e próximas sobre o rio seco.
    /// </summary>
    private static void CriarTrechoPonte(SpawnerMudas spawner)
    {
        Transform t = Vazio("2_PontePartida").transform;
        CriarPlataforma(t, "Ponte_1", new Vector3(13.5f, 0.3f, 0f), new Vector3(3f, 0.8f, 3.5f), false, spawner);
        CriarPlataforma(t, "Ponte_2", new Vector3(18f, 0.6f, 0.5f), new Vector3(3f, 0.4f, 2.5f), true, spawner);
        CriarPlataforma(t, "Ponte_3", new Vector3(22.5f, 1.0f, 1f), new Vector3(3f, 0.8f, 3f), false, spawner);
        CriarPlataforma(t, "Ponte_4", new Vector3(27f, 1.3f, -0.5f), new Vector3(3f, 0.4f, 2.5f), true, spawner);
        CriarPlataforma(t, "Ponte_5", new Vector3(32f, 1.6f, 0f), new Vector3(4f, 1f, 4f), false, spawner);

        // Restos da ponte antiga e leito do rio seco lá embaixo (só visuais)
        Prim(PrimitiveType.Cube, "PilarQuebrado1", t, new Vector3(16f, -6f, -2.5f), new Vector3(1.5f, 10f, 1.5f), M("PedraSeca"), false, new Vector3(0, 0, 6));
        Prim(PrimitiveType.Cube, "PilarQuebrado2", t, new Vector3(25f, -7f, 2.8f), new Vector3(1.5f, 9f, 1.5f), M("PedraSeca"), false, new Vector3(-5, 0, 0));
        Prim(PrimitiveType.Cube, "LeitoDoRioSeco", t, new Vector3(21f, -12f, 0f), new Vector3(16f, 1f, 80f), M("ChaoSeco"), false);
        for (int i = 0; i < 6; i++)
        {
            Prim(PrimitiveType.Sphere, "PedraDoRio" + i, t, new Vector3(Random.Range(14f, 28f), -11.5f, Random.Range(-30f, 30f)),
                 new Vector3(Random.Range(1f, 2.5f), 0.8f, Random.Range(1f, 2f)), M("RochaEscura"), false);
        }

        ColocarRuna(t, new Vector3(33.2f, 1.6f, 1.3f), 200f,
                    "Os druidas diziam: o que se tira da terra deve ser devolvido a ela. O Barão Morvak só tirou.");
    }

    /// <summary>
    /// Trecho 3 — Colina dos Moinhos: plataformas mais afastadas e subindo,
    /// um moinho grande cujas pás servem de atalho e o caminho secreto até a
    /// pedra rúnica dourada atrás dele.
    /// </summary>
    private static void CriarTrechoMoinhos(SpawnerMudas spawner)
    {
        Transform t = Vazio("3_ColinaDosMoinhos").transform;

        // Caminho principal
        CriarPlataforma(t, "Colina_1", new Vector3(38f, 2.3f, 1.5f), new Vector3(2.5f, 0.8f, 2.5f), false, spawner);
        CriarPlataforma(t, "Colina_2", new Vector3(42.5f, 3.0f, 3.5f), new Vector3(2.5f, 0.4f, 2.5f), true, spawner);
        CriarPlataforma(t, "Colina_3", new Vector3(47f, 3.8f, 2f), new Vector3(2.5f, 0.8f, 2.5f), false, spawner);
        CriarPlataforma(t, "Colina_4", new Vector3(51.5f, 4.5f, 0f), new Vector3(2.5f, 0.4f, 2.5f), true, spawner);
        GameObject topo = CriarPlataforma(t, "Colina_Topo", new Vector3(57.5f, 5.2f, 0f), new Vector3(5f, 1.5f, 5f), false, spawner);
        Colocar("ArvoreSeca", t, topo.transform.position + new Vector3(1.8f, 0.75f, 1.8f), 60f);
        ColocarRuna(t, new Vector3(59f, 5.2f, -1.6f), 200f,
                    "Aqui giravam os moinhos do vale. O vento move as pás e não pede nada em troca.");

        // Moinho grande: pás giram com torque (eixo em x=45, y=4.6, z=-6)
        // Sem collider: quem cair do atalho vai para o abismo em vez de ficar preso lá embaixo
        Prim(PrimitiveType.Cube, "IlhaDoMoinho", t, new Vector3(45f, -4.4f, -9.5f), new Vector3(6f, 2f, 5f), M("PedraSeca"), false);
        Prim(PrimitiveType.Sphere, "IlhaDoMoinho_Rocha", t, new Vector3(45f, -7f, -9.5f), new Vector3(6f, 5f, 5f), M("RochaEscura"), false);
        GameObject moinho = ConstruirMoinho("MoinhoGrande", 8f, 5f, 2f, -1f);
        moinho.transform.SetParent(t, false);
        moinho.transform.position = new Vector3(45f, -3.4f, -8.8f);

        // Atalho pelas pás: degraus até a ponta da pá e saída perto do eixo
        CriarPlataforma(t, "Atalho_Degrau1", new Vector3(37f, 3.2f, -2.5f), new Vector3(2f, 0.6f, 2f), false, spawner);
        CriarPlataforma(t, "Atalho_Degrau2", new Vector3(38f, 4.0f, -6f), new Vector3(2.5f, 0.6f, 2.5f), false, spawner);
        CriarPlataforma(t, "Atalho_Saida", new Vector3(48.5f, 5.0f, -3f), new Vector3(2.5f, 0.4f, 2.5f), true, spawner);

        // Caminho secreto atrás do moinho até a pedra rúnica dourada
        CriarPlataforma(t, "Segredo_1", new Vector3(40f, 3.4f, -10.5f), new Vector3(2f, 0.6f, 2f), false, spawner);
        CriarPlataforma(t, "Segredo_2", new Vector3(44.5f, 3.0f, -13.5f), new Vector3(2f, 0.6f, 2f), false, spawner);
        CriarPlataforma(t, "Segredo_Runa", new Vector3(49f, 3.0f, -13f), new Vector3(3f, 0.8f, 3f), false, spawner);
        ColocarRuna(t, new Vector3(49.5f, 3.0f, -13.5f), 0f,
                    "Segredo do Círculo Verde: a Árvore-Mãe não morreu de todo. Toda semente se lembra da floresta de onde veio.",
                    true);
    }

    /// <summary>
    /// Trecho 4 — Chaminé da Forja: plataformas pequenas e afastadas que exigem
    /// correr antes de pular, ao lado da velha forja que solta fumaça.
    /// </summary>
    /// <returns>As partículas de fumaça da chaminé.</returns>
    private static ParticleSystem CriarTrechoForja(SpawnerMudas spawner)
    {
        Transform t = Vazio("4_ChamineDaForja").transform;
        Vector3 tam = new Vector3(1.8f, 0.6f, 1.8f);
        CriarPlataforma(t, "Forja_1", new Vector3(65.4f, 5.6f, 0f), tam, false, spawner);
        CriarPlataforma(t, "Forja_2", new Vector3(71.4f, 6.0f, 1.5f), tam, false, spawner);
        CriarPlataforma(t, "Forja_3", new Vector3(77.4f, 6.4f, -0.5f), tam, false, spawner);
        CriarPlataforma(t, "Forja_4", new Vector3(83.4f, 6.8f, 0.5f), tam, false, spawner);

        // A velha forja do Barão
        Prim(PrimitiveType.Cube, "IlhaDaForja", t, new Vector3(74f, 3f, 10f), new Vector3(12f, 4f, 9f), M("PedraSeca"));
        Prim(PrimitiveType.Sphere, "IlhaDaForja_Rocha", t, new Vector3(74f, -1f, 10f), new Vector3(12f, 8f, 9f), M("RochaEscura"), false);
        Prim(PrimitiveType.Cube, "PredioDaForja", t, new Vector3(73f, 8f, 11f), new Vector3(8f, 6f, 5f), M("RochaEscura"));
        Prim(PrimitiveType.Cube, "TelhadoDaForja", t, new Vector3(73f, 11.2f, 11f), new Vector3(8.6f, 0.5f, 5.6f), M("TelhadoEscuro"));
        Prim(PrimitiveType.Cube, "PortaDaForja", t, new Vector3(73f, 6.5f, 8.45f), new Vector3(2.2f, 3f, 0.1f), M("Fogo"), false);
        Prim(PrimitiveType.Cube, "Bigorna", t, new Vector3(70f, 5.4f, 7.2f), new Vector3(1f, 0.8f, 0.5f), M("Metal"));
        Prim(PrimitiveType.Cylinder, "Chamine", t, new Vector3(76.5f, 12f, 12f), new Vector3(1.6f, 7f, 1.6f), M("RochaEscura"));
        Colocar("ArvoreSeca", t, new Vector3(68.8f, 5f, 6.5f), 10f);
        Colocar("Estandarte", t, new Vector3(78.5f, 5f, 7f), 180f);

        Light brasa = Vazio("LuzDaForja", t, new Vector3(73f, 6.5f, 7.5f)).AddComponent<Light>();
        brasa.type = LightType.Point;
        brasa.color = new Color(1f, 0.45f, 0.15f);
        brasa.range = 9f;
        brasa.intensity = 4f;

        // Fumaça escura saindo da chaminé (apagada ao despertar a Nascente)
        ParticleSystem fumaca = NovaParticula("FumacaDaChamine", t, new Vector3(76.5f, 19.3f, 12f), matParticulaAlpha);
        ConfigurarParticula(fumaca, true, true, 7f, 1.6f, 2.2f, new Color(0.12f, 0.12f, 0.12f, 0.75f), -0.02f, 250);
        Emissao(fumaca, 10f);
        Forma(fumaca, ParticleSystemShapeType.Cone, 12f, 0.5f);
        fumaca.transform.rotation = Quaternion.Euler(-90f, 0, 0);
        TamanhoAoLongoDaVida(fumaca, 0.6f, 3f);
        SumirAoLongoDaVida(fumaca);
        ParticleSystem.VelocityOverLifetimeModule vento = fumaca.velocityOverLifetime;
        vento.enabled = true;
        vento.space = ParticleSystemSimulationSpace.World;
        vento.x = new ParticleSystem.MinMaxCurve(0.8f);
        vento.y = new ParticleSystem.MinMaxCurve(0f);
        vento.z = new ParticleSystem.MinMaxCurve(0.3f);
        return fumaca;
    }

    /// <summary>
    /// Trecho 5 — Nascente do Vale: fonte de pedra seca cercada de árvores
    /// mortas. Tocar nela (Tag Final) restaura o vale e conclui o nível.
    /// </summary>
    private static void CriarTrechoNascente(SpawnerMudas spawner, Light sol, ParticleSystem fumaca, RodaDagua roda)
    {
        Transform t = Vazio("5_NascenteDoVale").transform;
        GameObject colina = Prim(PrimitiveType.Cube, "ColinaDaNascente", t, new Vector3(91f, 6.2f, 0f), new Vector3(9f, 2f, 9f), M("ChaoSeco"));
        colina.tag = "Plataforma";
        colina.isStatic = true;
        Prim(PrimitiveType.Sphere, "ColinaDaNascente_Rocha", t, new Vector3(91f, 1.5f, 0f), new Vector3(9f, 9f, 9f), M("RochaEscura"), false);

        // Fonte de pedra
        Vector3 posFonte = new Vector3(92f, 7.2f, 0f);
        GameObject nascente = Vazio("Nascente", t, posFonte);
        nascente.tag = "Final";
        BoxCollider gatilho = nascente.AddComponent<BoxCollider>();
        gatilho.isTrigger = true;
        gatilho.center = new Vector3(0, 1.5f, 0);
        gatilho.size = new Vector3(3.8f, 3f, 3.8f);

        Transform n = nascente.transform;
        ColisorCaixa(Prim(PrimitiveType.Cylinder, "Bacia", n, new Vector3(0, 0.4f, 0), new Vector3(3f, 0.4f, 3f), M("PedraSeca")));
        GameObject espelho = Prim(PrimitiveType.Cylinder, "EspelhoDagua", n, new Vector3(0, 0.82f, 0), new Vector3(2.6f, 0.02f, 2.6f), M("Agua"), false);
        espelho.SetActive(false);
        Prim(PrimitiveType.Cylinder, "Coluna", n, new Vector3(0, 1.4f, 0), new Vector3(0.5f, 0.6f, 0.5f), M("PedraSeca"));
        ColisorCaixa(Prim(PrimitiveType.Cylinder, "Taca", n, new Vector3(0, 2.05f, 0), new Vector3(1.4f, 0.1f, 1.4f), M("PedraSeca")));

        ParticleSystem agua = NovaParticula("AguaDaNascente", n, new Vector3(0, 2.2f, 0), matParticulaAlpha);
        ConfigurarParticula(agua, true, false, 1.1f, 3f, 0.15f, new Color(0.55f, 0.75f, 1f, 0.85f), 1f, 400);
        Emissao(agua, 70f);
        Forma(agua, ParticleSystemShapeType.Cone, 18f, 0.1f);
        agua.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

        ParticleSystem explosao = NovaParticula("ExplosaoDeLuz", n, new Vector3(0, 1.5f, 0), matParticulaAditiva);
        ConfigurarParticula(explosao, false, false, 1.6f, 9f, 0.4f, new Color(1f, 0.9f, 0.5f, 1f), -0.05f, 300);
        Rajada(explosao, 200);
        Forma(explosao, ParticleSystemShapeType.Sphere, 0f, 0.5f);
        TamanhoAoLongoDaVida(explosao, 1f, 0f);
        ParticleSystem.MainModule mainExplosao = explosao.main;
        mainExplosao.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);

        // Árvores secas ao redor e as versões verdes (escondidas) no mesmo lugar
        Vector3[] posArvores =
        {
            new Vector3(88.5f, 7.2f, 3.2f), new Vector3(88.5f, 7.2f, -3.2f),
            new Vector3(94.5f, 7.2f, 3.2f), new Vector3(94.5f, 7.2f, -3.2f)
        };
        List<GameObject> secas = new List<GameObject>();
        List<GameObject> verdes = new List<GameObject>();
        for (int i = 0; i < posArvores.Length; i++)
        {
            float rot = i * 85f;
            secas.Add(Colocar("ArvoreSeca", t, posArvores[i], rot));
            GameObject verde = Colocar("ArvoreVerde", t, posArvores[i], rot);
            verde.SetActive(false);
            verdes.Add(verde);
        }
        Colocar("ArbustoSeco", t, new Vector3(90f, 7.2f, -1.5f), 30f);
        Colocar("ArbustoSeco", t, new Vector3(94f, 7.2f, 1f), 120f);
        Colocar("Rocha", t, new Vector3(95f, 7.2f, -0.5f), 45f);

        NascenteFinal final = nascente.AddComponent<NascenteFinal>();
        Ref(final, "explosaoLuz", explosao);
        Ref(final, "agua", agua);
        Ref(final, "fumacaForja", fumaca);
        Ref(final, "rodaDagua", roda);
        Ref(final, "espelhoDagua", espelho);
        Ref(final, "luzSol", sol);
        Ref(final, "skyboxRestaurado", ceuRestaurado);
        RefArray(final, "arvoresSecas", secas);
        RefArray(final, "arvoresVerdes", verdes);
    }

    /// <summary>
    /// Montanhas ao longe, as muralhas do próximo nível no alto e pedras
    /// flutuando no abismo (só visuais, sem collider).
    /// </summary>
    private static void CriarCenarioDeFundo()
    {
        Transform t = Vazio("CenarioDeFundo").transform;

        Prim(PrimitiveType.Sphere, "Montanha1", t, new Vector3(170f, -10f, 70f), new Vector3(90f, 110f, 80f), M("Montanha"), false);
        Prim(PrimitiveType.Sphere, "Montanha2", t, new Vector3(120f, -20f, 120f), new Vector3(110f, 90f, 90f), M("Montanha"), false);
        Prim(PrimitiveType.Sphere, "Montanha3", t, new Vector3(200f, -15f, -60f), new Vector3(100f, 130f, 90f), M("Montanha"), false);
        Prim(PrimitiveType.Sphere, "Montanha4", t, new Vector3(-60f, -30f, 140f), new Vector3(120f, 80f, 90f), M("Montanha"), false);
        Prim(PrimitiveType.Sphere, "Montanha5", t, new Vector3(-90f, -30f, -120f), new Vector3(120f, 90f, 100f), M("Montanha"), false);

        // Muralhas de Pedra-Negra (destino do nível 2), lá no alto
        Transform muralhas = Vazio("MuralhasDePedraNegraAoLonge", t).transform;
        Prim(PrimitiveType.Sphere, "Ilha", muralhas, new Vector3(150f, 30f, -30f), new Vector3(40f, 20f, 30f), M("RochaEscura"), false);
        Prim(PrimitiveType.Cube, "Muralha", muralhas, new Vector3(150f, 44f, -30f), new Vector3(30f, 8f, 3f), M("RochaEscura"), false);
        Prim(PrimitiveType.Cube, "Torre1", muralhas, new Vector3(136f, 48f, -30f), new Vector3(5f, 16f, 5f), M("RochaEscura"), false);
        Prim(PrimitiveType.Cube, "Torre2", muralhas, new Vector3(164f, 46f, -30f), new Vector3(5f, 12f, 5f), M("RochaEscura"), false, new Vector3(0, 0, 8));

        // Pedras soltas flutuando no abismo
        for (int i = 0; i < 12; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10f, 110f), Random.Range(-14f, -4f), Random.Range(-35f, 35f));
            if (Mathf.Abs(pos.z) < 8f)
            {
                pos.z += Mathf.Sign(pos.z) * 10f;
            }
            Prim(PrimitiveType.Sphere, "PedraFlutuante" + i, t, pos, new Vector3(Random.Range(1f, 4f), Random.Range(1f, 3f), Random.Range(1f, 4f)),
                 M("RochaEscura"), false, new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f));
        }
    }

    /// <summary>
    /// Cria o Bran (placeholder em cápsula): Rigidbody, CapsuleCollider,
    /// PlayerController, PlayerAnimacao, Animator e a semente brilhando.
    /// </summary>
    private static GameObject CriarJogador(Vector3 pos)
    {
        GameObject bran = Vazio("Bran", null, pos);
        bran.tag = "Player";
        bran.layer = 2; // Ignore Raycast: a câmera e a detecção de chão não acertam o próprio jogador
        bran.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Rigidbody corpo = bran.AddComponent<Rigidbody>();
        corpo.mass = 1f;
        corpo.constraints = RigidbodyConstraints.FreezeRotation;
        corpo.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider capsula = bran.AddComponent<CapsuleCollider>();
        capsula.center = new Vector3(0f, 0.9f, 0f);
        capsula.height = 1.8f;
        capsula.radius = 0.35f;

        // Modelo (troque pelo modelo do Mixamo mantendo o Animator)
        GameObject modelo = Vazio("Modelo", bran.transform);
        Animator animador = modelo.AddComponent<Animator>();
        animador.runtimeAnimatorController = controllerBran;
        Transform m = modelo.transform;
        Prim(PrimitiveType.Capsule, "Capa", m, new Vector3(0, 0.85f, -0.03f), new Vector3(0.72f, 0.75f, 0.62f), M("CapaVerde"), false);
        Prim(PrimitiveType.Capsule, "Tunica", m, new Vector3(0, 0.85f, 0.04f), new Vector3(0.6f, 0.7f, 0.55f), M("Linho"), false);
        Prim(PrimitiveType.Sphere, "Cabeca", m, new Vector3(0, 1.6f, 0.02f), Vector3.one * 0.38f, M("Pele"), false);
        Prim(PrimitiveType.Sphere, "Capuz", m, new Vector3(0, 1.64f, -0.05f), new Vector3(0.44f, 0.42f, 0.42f), M("CapaVerde"), false);
        Prim(PrimitiveType.Cube, "BotaE", m, new Vector3(-0.13f, 0.12f, 0.05f), new Vector3(0.17f, 0.24f, 0.3f), M("Couro"), false);
        Prim(PrimitiveType.Cube, "BotaD", m, new Vector3(0.13f, 0.12f, 0.05f), new Vector3(0.17f, 0.24f, 0.3f), M("Couro"), false);
        Prim(PrimitiveType.Cube, "Bolsa", m, new Vector3(0.2f, 0.85f, 0.3f), new Vector3(0.2f, 0.22f, 0.12f), M("Couro"), false);
        Prim(PrimitiveType.Sphere, "Semente", m, new Vector3(0.2f, 1.0f, 0.33f), Vector3.one * 0.12f, M("Semente"), false);

        Light luzSemente = Vazio("LuzDaSemente", m, new Vector3(0.2f, 1.0f, 0.5f)).AddComponent<Light>();
        luzSemente.type = LightType.Point;
        luzSemente.color = new Color(1f, 0.85f, 0.45f);
        luzSemente.range = 3f;
        luzSemente.intensity = 1.5f;

        // Poeira e pedrinhas ao aterrissar
        ParticleSystem poeira = NovaParticula("PoeiraPouso", bran.transform, new Vector3(0, 0.05f, 0), matParticulaAlpha);
        ConfigurarParticula(poeira, false, false, 0.6f, 2.5f, 0.15f, new Color(0.55f, 0.5f, 0.42f, 0.9f), 1f, 50);
        Rajada(poeira, 18);
        Forma(poeira, ParticleSystemShapeType.Cone, 80f, 0.3f);
        poeira.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
        ParticleSystem.MainModule mainPoeira = poeira.main;
        mainPoeira.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
        SumirAoLongoDaVida(poeira);

        PlayerController controle = bran.AddComponent<PlayerController>();
        Ref(controle, "particulasPouso", poeira);
        PlayerAnimacao anim = bran.AddComponent<PlayerAnimacao>();
        Ref(anim, "animador", animador);
        return bran;
    }

    /// <summary>
    /// Interface do nível: HUD, texto das runas, menu de pausa e tela de
    /// nível concluído, todos ligados ao UIManager.
    /// </summary>
    private static void CriarInterfaceNivel()
    {
        Canvas canvas = CriarCanvas("Interface");
        UIManager ui = canvas.gameObject.AddComponent<UIManager>();
        Transform c = canvas.transform;
        Vector2 centro = new Vector2(0.5f, 0.5f);

        // HUD: "Fase X / 5" (canto superior esquerdo) e botão "Menu" (canto superior direito)
        Image fundoFase = CriarPainel("PainelFase", c, new Vector2(0f, 1f), new Vector2(30, -30), new Vector2(260, 70), CorPainel);
        Text textoFase = CriarTexto("TextoFase", fundoFase.transform, "Fase 1 / 5", 38, null, Vector2.zero, Vector2.zero,
                                    TextAnchor.MiddleCenter, CorTexto, FontStyle.Bold);
        CriarBotao(c, "Menu", new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(200, 70), ui.AbrirMenu);

        // Texto das pedras rúnicas (parte inferior)
        Image painelRuna = CriarPainel("PainelRuna", c, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1200, 150), CorPainel);
        Text textoRuna = CriarTexto("TextoRuna", painelRuna.transform, "", 36, null, Vector2.zero, Vector2.zero,
                                    TextAnchor.MiddleCenter, CorTexto);
        RectTransform rRuna = (RectTransform)textoRuna.transform;
        rRuna.offsetMin = new Vector2(30, 10);
        rRuna.offsetMax = new Vector2(-30, -10);

        // Menu de pausa
        Image painelPausa = CriarPainel("PainelPausa", c, null, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f));
        Image caixaPausa = CriarPainel("Caixa", painelPausa.transform, centro, Vector2.zero, new Vector2(620, 560), CorPainel);
        CriarTexto("Titulo", caixaPausa.transform, "Pausado", 64, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(560, 90),
                   TextAnchor.MiddleCenter, CorTitulo, FontStyle.Bold);
        CriarBotao(caixaPausa.transform, "Continuar", centro, new Vector2(0, 40), new Vector2(400, 85), ui.FecharMenu);
        CriarBotao(caixaPausa.transform, "Reiniciar", centro, new Vector2(0, -70), new Vector2(400, 85), ui.BotaoReiniciar);
        CriarBotao(caixaPausa.transform, "Sair do jogo", centro, new Vector2(0, -180), new Vector2(400, 85), ui.BotaoSair);

        // Tela de nível concluído
        Image painelConcluido = CriarPainel("PainelConcluido", c, null, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        Image caixaConcluido = CriarPainel("Caixa", painelConcluido.transform, centro, Vector2.zero, new Vector2(1200, 620), CorPainel);
        Transform cc = caixaConcluido.transform;
        CriarTexto("Titulo", cc, "Nível concluído!", 70, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1100, 100),
                   TextAnchor.MiddleCenter, CorTitulo, FontStyle.Bold);
        Text textoConcluido = CriarTexto("TextoRestauracao", cc, "", 38, centro, new Vector2(0, 50), new Vector2(1050, 200),
                                         TextAnchor.MiddleCenter, CorTexto, FontStyle.Italic);
        Text textoAviso = CriarTexto("TextoAviso", cc, "", 32, centro, new Vector2(0, -95), new Vector2(1050, 60),
                                     TextAnchor.MiddleCenter, new Color(0.8f, 0.9f, 0.7f));
        Button proximo = CriarBotao(cc, "Próximo nível", new Vector2(0.5f, 0f), new Vector2(-230, 50), new Vector2(400, 85), ui.BotaoProximoNivel);
        Button jogarNovamente = CriarBotao(cc, "Jogar novamente", new Vector2(0.5f, 0f), new Vector2(-230, 50), new Vector2(400, 85), ui.BotaoJogarNovamente);
        Button menuInicial = CriarBotao(cc, "Menu inicial", new Vector2(0.5f, 0f), new Vector2(230, 50), new Vector2(400, 85), ui.BotaoMenuInicial);

        // Liga tudo ao UIManager
        Ref(ui, "textoFase", textoFase);
        Ref(ui, "painelRuna", painelRuna.gameObject);
        Ref(ui, "textoRuna", textoRuna);
        Ref(ui, "painelPausa", painelPausa.gameObject);
        Ref(ui, "painelConcluido", painelConcluido.gameObject);
        Ref(ui, "textoConcluido", textoConcluido);
        Ref(ui, "textoAvisoConcluido", textoAviso);
        Ref(ui, "botaoProximoNivel", proximo.gameObject);
        Ref(ui, "botaoJogarNovamente", jogarNovamente.gameObject);
        Ref(ui, "botaoMenuInicial", menuInicial.gameObject);

        // Os painéis começam escondidos (o UIManager também garante isso no Awake)
        painelRuna.gameObject.SetActive(false);
        painelPausa.gameObject.SetActive(false);
        painelConcluido.gameObject.SetActive(false);
    }
}
