using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Troca o conteúdo dos prefabs placeholder (primitivas) pelos modelos do pack
/// Polytope Studio. Menu: Semente da Aurora → Trocar Assets (Polytope).
/// As cenas não mudam: elas só referenciam os prefabs.
/// </summary>
public static partial class TrocarAssets
{
    private const string Pack = "Assets/Polytope Studio/";
    private const string Arvores = Pack + "Lowpoly_Environments/Prefabs/Trees/";
    private const string Pedras = Pack + "Lowpoly_Environments/Prefabs/Rocks/";
    private const string Arbustos = Pack + "Lowpoly_Environments/Prefabs/Shrubs/";
    private const string Flores = Pack + "Lowpoly_Environments/Prefabs/";
    private const string Vila = Pack + "Lowpoly_Village/Prefabs/Modular/";
    private const string PastaMateriais = "Assets/Materiais/";

    private enum Colisor { Nenhum, Capsula, Caixa }

    /// <summary>Prefab do jogo, prefab novo, escala, colisor e rotação do modelo.</summary>
    private struct Troca
    {
        public string alvo, novo;
        public float escala;
        public Colisor colisor;
        // Se > 0, ajusta o modelo para ter essa largura (maior lado no chão) ou altura e o apoia na base.
        public float larguraAlvo, alturaAlvo;
        // Gira o modelo para o lado maior ficar no eixo X (cercas).
        public bool alinharX;

        public Troca(string alvo, string novo, float escala, Colisor colisor, float larguraAlvo = 0f, float alturaAlvo = 0f, bool alinharX = false)
        {
            this.alvo = alvo; this.novo = novo; this.escala = escala; this.colisor = colisor;
            this.larguraAlvo = larguraAlvo; this.alturaAlvo = alturaAlvo; this.alinharX = alinharX;
        }
    }

    private const string Rpg = "Assets/RPGPP_LT/Prefabs/";

    private static readonly Troca[] Trocas =
    {
        new Troca("Assets/Prefabs/ArvoreSeca.prefab", Arvores + "PT_Pine_Tree_03_dead.prefab", 1f, Colisor.Capsula),
        new Troca("Assets/Prefabs/ArvoreVerde.prefab", Arvores + "PT_Fruit_Tree_01_green.prefab", 1f, Colisor.Capsula),
        new Troca("Assets/Prefabs/ArbustoSeco.prefab", Arbustos + "PT_Generic_Shrub_01_dead.prefab", 1f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Rocha.prefab", Pedras + "PT_Generic_Rock_01.prefab", 1f, Colisor.Caixa),
        new Troca("Assets/Prefabs/Cerca.prefab", Vila + "Fence/PT_Modular_Fence_Wood_01.prefab", 1f, Colisor.Caixa, 3f, 0f, true),
        new Troca("Assets/Prefabs/ArbustoVerde.prefab", Arbustos + "PT_Generic_Shrub_01_green.prefab", 1f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Mudas/Muda_Tulipa.prefab", Flores + "Flowers/PT_Poppy_02.prefab", 1f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Mudas/Muda_Margarida.prefab", Flores + "Mushrooms/PT_Caesars_Mushroom_01.prefab", 1f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Mudas/Muda_Broto.prefab", Flores + "Plants/PT_Grass_02.prefab", 1f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Mudas/Muda_ArbustoFlorido.prefab", Arbustos + "PT_Generic_Shrub_01_green.prefab", 0.6f, Colisor.Nenhum),
        new Troca("Assets/Prefabs/Mudas/Muda_Lavanda.prefab", Flores + "Flowers/PT_Poppy_02.prefab", 1.2f, Colisor.Nenhum),

        // Pack RPGPP_LT: construções e objetos. O pack não tem moinho; o da praça vira uma construção.
        new Troca("Assets/Prefabs/Casa.prefab", Rpg + "Buildings/Bld_closed/rpgpp_lt_building_01.prefab", 1f, Colisor.Caixa, 5.5f),
        new Troca("Assets/Prefabs/Moinho.prefab", Rpg + "Buildings/Bld_closed/rpgpp_lt_building_05.prefab", 1f, Colisor.Caixa, 6f),
        new Troca("Assets/Prefabs/Poco.prefab", Rpg + "Props/Misc/rpgpp_lt_well_01.prefab", 1f, Colisor.Caixa, 2.4f),
        new Troca("Assets/Prefabs/Barril.prefab", Rpg + "Props/Containers/rpgpp_lt_barrel_01.prefab", 1f, Colisor.Caixa, 0.9f),
        new Troca("Assets/Prefabs/Caixote.prefab", Rpg + "Props/Containers/rpgpp_lt_crate_01.prefab", 1f, Colisor.Caixa, 0.9f),
        new Troca("Assets/Prefabs/Carroca.prefab", Rpg + "Props/Misc/rpgpp_lt_wagon_01.prefab", 1f, Colisor.Caixa, 3f),
        new Troca("Assets/Prefabs/Estandarte.prefab", Rpg + "Props/Banners/rpgpp_lt_banner_01a.prefab", 1f, Colisor.Nenhum, 0f, 3.6f),
    };

    private const string Padre = Pack + "Lowpoly_Characters/Prefabs/PT_Priest_StPatrick.prefab";
    private const string PadreMalha = Pack + "Lowpoly_Characters/Sources/Meshes/PT_Priest_StPatrick.fbx";
    private const string PadrePose = Pack + "Lowpoly_Characters/Animations/PT_Priest_StPatrick_posed.fbx";
    private const string RochaMalha = Pack + "Lowpoly_Environments/Sources/Meshes/Rocks/PT_Generic_Rock_01.fbx";

    /// <summary>
    /// Troca o Bran pelo padre na cena Nivel1 e as esferas de rocha (chão, ilhas, pedrinhas)
    /// pela malha de rocha do pack, mantendo posição e escala de cada uma.
    /// </summary>
    private static void TrocarCenas()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Mesh rocha = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RochaMalha))
        {
            if (o is Mesh m && (rocha == null || m.name.EndsWith("LOD0"))) rocha = m;
        }
        Material matRocha = null;
        GameObject prefabRocha = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rocha.prefab");
        if (prefabRocha != null)
        {
            Renderer rr = prefabRocha.GetComponentInChildren<Renderer>();
            if (rr != null) matRocha = rr.sharedMaterial;
        }

        foreach (string cena in new[] { "Assets/Cenas/Nivel1.unity", "Assets/Cenas/MenuInicial.unity", "Assets/Cenas/Introducao.unity" })
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(cena) == null) continue;
            Scene aberta = EditorSceneManager.OpenScene(cena, OpenSceneMode.Single);

            int rochas = 0;
            if (rocha != null && matRocha != null)
            {
                foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mf.sharedMesh == null || mf.sharedMesh.name != "Sphere" || mr == null) continue;
                    string mat = mr.sharedMaterial != null ? mr.sharedMaterial.name : "";
                    if (mat != "RochaEscura" && mat != "PedraSeca") continue;

                    mf.sharedMesh = rocha;
                    mr.sharedMaterial = matRocha;
                    // O colisor de esfera não acompanha a malha nova; troca por caixa.
                    SphereCollider esfera = mf.GetComponent<SphereCollider>();
                    if (esfera != null)
                    {
                        Object.DestroyImmediate(esfera);
                        mf.gameObject.AddComponent<MeshCollider>();
                    }
                    rochas++;
                }
            }

            bool nivel1 = cena.EndsWith("Nivel1.unity");
            if (nivel1)
            {
                ConstruirMundo();
                AjustarPulo();
                ReformarCaminho();
                AplicarCeu(true);
            }
            else if (cena.EndsWith("Introducao.unity"))
            {
                TrocarIntroducao();
            }
            else
            {
                ConstruirMundo(true);
                AplicarCeu(false);
            }
            bool padre = nivel1 && TrocarJogador();
            Debug.Log("TrocarAssets: " + aberta.name + " - " + rochas + " rochas" + (padre ? ", jogador trocado pelo padre" : ""));
            EditorSceneManager.MarkSceneDirty(aberta);
            EditorSceneManager.SaveScene(aberta);
        }

        // Deixa o menu aberto: o Play começa pela cena aberta no editor
        EditorSceneManager.OpenScene("Assets/Cenas/MenuInicial.unity", OpenSceneMode.Single);
    }

    /// <summary>Altura do chão do mundo em volta, abaixo das plataformas (só visual, sem colisor).</summary>
    private const float ChaoMundo = -14f;

    /// <summary>
    /// Pulo e corrida um pouco mais longos (valores da cena, que valem mais que os do script).
    /// </summary>
    private static void AjustarPulo()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        SerializedObject so = new SerializedObject(pc);
        so.FindProperty("velocidadeAndar").floatValue = 5f;
        so.FindProperty("velocidadeCorrer").floatValue = 7f;
        so.FindProperty("forcaPulo").floatValue = 6.5f;
        so.FindProperty("controleNoAr").floatValue = 0.7f;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Enche o vazio em volta do nível: chão grande, árvores, arbustos e pedras espalhados, colinas e
    /// montanhas no horizonte. Tudo sem colisor, então quem cair ainda atravessa e leva ao abismo.
    /// </summary>
    private const string Moveis = "Assets/LowPolyMedievalPropsLite/Prefabs/";

    /// <summary>
    /// Instancia um prefab no cenário, ajustado a uma altura/largura e apoiado em (x, y, z).
    /// </summary>
    private static GameObject Mobilia(Transform pai, string prefab, Vector3 pos, float rotY, float largura, float altura)
    {
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
        if (modelo == null)
        {
            Debug.LogWarning("TrocarAssets: não achei " + prefab);
            return null;
        }
        GameObject raiz = new GameObject(modelo.name);
        raiz.transform.SetParent(pai, false);
        GameObject filho = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
        CorrigirMateriais(filho);
        SemColisor(filho);
        // Ajusta com o pai na origem e depois leva tudo para o lugar
        Ajustar(filho.transform, largura, altura);
        raiz.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
        return raiz;
    }

    /// <summary>
    /// Cutscene (cabana de Ysolde): troca mesa e cajado pelos modelos novos e mobília a cabana.
    /// </summary>
    private static void TrocarIntroducao()
    {
        GameObject cabana = GameObject.Find("Cabana");
        if (cabana == null) return;
        Transform t = cabana.transform;

        foreach (string nome in new[] { "Mesa", "Perna0", "Perna1", "Perna2", "Perna3", "Cajado", "FolhasCajado" })
        {
            Transform velho = t.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
        }
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            if (t.GetChild(i).name == "Mobilia") Object.DestroyImmediate(t.GetChild(i).gameObject);
        }
        Transform m = new GameObject("Mobilia").transform;
        m.SetParent(t, false);

        // Entre os 3 móveis do pack, usa o mais baixo e largo como mesa
        string mesa = Moveis + "Furniture_01.prefab";
        float melhor = float.MaxValue;
        foreach (string f in new[] { "Furniture_01", "Furniture_02", "Furniture_03" })
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(Moveis + f + ".prefab");
            if (g == null) continue;
            GameObject prova = (GameObject)PrefabUtility.InstantiatePrefab(g);
            bool ok = Limites(prova.transform, out Bounds b);
            Object.DestroyImmediate(prova);
            if (!ok) continue;
            float razao = b.size.y / Mathf.Max(b.size.x, b.size.z, 0.0001f);
            if (razao < melhor) { melhor = razao; mesa = Moveis + f + ".prefab"; }
        }

        GameObject tapete = Mobilia(m, Moveis + "Carpet_01.prefab", new Vector3(0f, 0.01f, 0.4f), 0f, 3.6f, 0f);
        GameObject mesaGo = Mobilia(m, mesa, new Vector3(0f, 0f, 0.5f), 0f, 0f, 0.8f);
        float topo = 0.8f;
        if (mesaGo != null && Limites(mesaGo.transform, out Bounds bm)) topo = bm.max.y;

        Mobilia(m, Moveis + "Plate_01.prefab", new Vector3(-0.5f, topo, 0.6f), 0f, 0.35f, 0f);
        Mobilia(m, Moveis + "Cup_01.prefab", new Vector3(0.55f, topo, 0.8f), 30f, 0.15f, 0f);
        Mobilia(m, Moveis + "Candle_01.prefab", new Vector3(-0.8f, topo, 0.3f), 0f, 0f, 0.25f);
        Mobilia(m, Moveis + "Jug_01.prefab", new Vector3(0.85f, topo, 0.3f), 0f, 0f, 0.3f);

        // A semente fica em cima da mesa
        Transform semente = t.Find("SementeDaAurora");
        if (semente != null) semente.position = new Vector3(0f, topo + 0.15f, 0.5f);
        Transform luz = t.Find("LuzSemente");
        if (luz != null) luz.position = new Vector3(0f, topo + 0.35f, 0.5f);

        // Cantos da cabana
        Mobilia(m, Moveis + "Barrel_01.prefab", new Vector3(-2.3f, 0f, 2.3f), 20f, 0f, 1f);
        Mobilia(m, Moveis + "Box_01.prefab", new Vector3(-1.4f, 0f, 2.45f), -10f, 0f, 0.7f);
        Mobilia(m, Moveis + "Bag_01.prefab", new Vector3(-2.4f, 0f, 1.3f), 40f, 0f, 0.6f);
        Mobilia(m, Moveis + "Firewood_01.prefab", new Vector3(2.4f, 0f, 2.3f), 0f, 0f, 0.6f);
        Mobilia(m, Moveis + "Bucket_01.prefab", new Vector3(2.3f, 0f, 0.8f), 0f, 0f, 0.5f);

        // Cajado do pack, encostado na parede
        GameObject cajado = Mobilia(m, Pack + "Lowpoly_Weapons/Prefabs/PT_Staff_StPatrick_01.prefab", new Vector3(-2.7f, 0f, 2.75f), 0f, 0f, 1.9f);
        if (cajado != null) cajado.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
    }

    /// <summary>
    /// Caminho do nível: a forja vira uma casa nova e as plataformas passam a criar uma árvore viva.
    /// </summary>
    private static void ReformarCaminho()
    {
        GameObject forja = GameObject.Find("4_ChamineDaForja");
        GameObject casa = AssetDatabase.LoadAssetAtPath<GameObject>(Rpg + "Buildings/Bld_closed/rpgpp_lt_building_03.prefab");
        if (forja != null && casa != null)
        {
            foreach (string nome in new[] { "PredioDaForja", "TelhadoDaForja", "PortaDaForja", "CasaDaForja" })
            {
                Transform velho = forja.transform.Find(nome);
                if (velho != null) Object.DestroyImmediate(velho.gameObject);
            }

            GameObject raiz = new GameObject("CasaDaForja");
            raiz.transform.SetParent(forja.transform, false);
            GameObject filho = (GameObject)PrefabUtility.InstantiatePrefab(casa, raiz.transform);
            CorrigirMateriais(filho);
            SemColisor(filho);
            Ajustar(filho.transform, 8f, 0f);
            AdicionarColisor(raiz, filho, Colisor.Caixa);
            // Em cima da ilha da forja (topo em y=5), com a porta virada para o caminho (-Z)
            raiz.transform.SetPositionAndRotation(new Vector3(73f, 5f, 11f), Quaternion.Euler(0f, 180f, 0f));
        }

        ReviverIlhaDaForja(forja);
        CriarMorros();
        LigarTerrenoVivo();

        // Novos assets: forja, fonte, enfeites da vila, aldeias distantes e rios
        ReformarForja(forja);
        ReformarFonte();
        EnfeitarVila();
        GameObject mundoRaiz = GameObject.Find("MundoAoRedor");
        if (mundoRaiz != null)
        {
            // Rios primeiro: as casas e os objetos do cenário desviam deles
            CriarRios(mundoRaiz.transform);
            CasasNoCenario(mundoRaiz.transform);
        }
        AjustarCaixotes();

        // Sem as pedras penduradas embaixo das plataformas e das ilhas
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name.EndsWith("_Rocha")) Object.DestroyImmediate(t.gameObject);
        }

        AjustarTamanhos();

        ArvoresDasPlataformas();
        ArbustosDoFinal();
    }

    /// <summary>
    /// Remove as árvores que o script criava sozinho nas plataformas e liga as árvores mortas que já
    /// existem em cima de cada plataforma a uma árvore viva, que cresce quando ela refloresce.
    /// </summary>
    private static void ArvoresDasPlataformas()
    {
        GameObject viva = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreVerde.prefab");
        GameObject seca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreSeca.prefab");
        if (viva == null) return;
        Random.State estado = Random.state;
        Random.InitState(23);

        // Limpa o que foi criado em rodadas anteriores (árvores avulsas e grupos)
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            if (t.name.EndsWith("_ArvoreSeca") || t.name.EndsWith("_ArvoreViva") || t.name.EndsWith("_VivasGemeas")
                || t.name.EndsWith("_ArvoresMortas"))
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        // Árvores mortas que o nível já tem (fora da ilha da forja, que tem as suas)
        List<Transform> mortas = new List<Transform>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "ArvoreSeca" || t.GetComponentInParent<PlataformaReflorescente>() != null) continue;
            if (t.parent != null && t.parent.name.StartsWith("ArvoresDaIlha")) continue;
            mortas.Add(t);
        }

        // Em ordem de nome, para o sorteio dar o mesmo resultado a cada rodada
        PlataformaReflorescente[] plataformas = Object.FindObjectsByType<PlataformaReflorescente>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(plataformas, (x, y) => string.CompareOrdinal(x.name, y.name));
        foreach (PlataformaReflorescente plat in plataformas)
        {
            if (plat.name == "IlhaDaForja") continue;
            SerializedObject so = new SerializedObject(plat);
            so.FindProperty("arvoreSeca").objectReferenceValue = null;
            so.FindProperty("arvoreViva").objectReferenceValue = null;

            // Árvores mortas em cima desta plataforma (a plataforma é um cubo escalado)
            Bounds b = new Bounds(plat.transform.position, plat.transform.lossyScale);
            List<Transform> daPlataforma = new List<Transform>();
            foreach (Transform m in mortas)
            {
                Vector3 p = m.position;
                if (Mathf.Abs(p.x - b.center.x) <= b.extents.x + 0.3f && Mathf.Abs(p.z - b.center.z) <= b.extents.z + 0.3f
                    && p.y >= b.max.y - 0.5f && p.y <= b.max.y + 1.5f)
                {
                    daPlataforma.Add(m);
                }
            }
            // As árvores de rodadas anteriores saem; só a árvore original da colina do topo é mantida
            if (plat.name != "Colina_Topo" && daPlataforma.Count > 0)
            {
                foreach (Transform m in daPlataforma)
                {
                    mortas.Remove(m);
                    if (m != null) Object.DestroyImmediate(m.gameObject);
                }
                daPlataforma.Clear();
            }

            if (daPlataforma.Count > 0)
            {
                Transform pai = plat.transform.parent;
                Transform vivas = new GameObject(plat.name + "_VivasGemeas").transform;
                vivas.SetParent(pai, false);
                foreach (Transform m in daPlataforma)
                {
                    GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(viva, vivas);
                    v.transform.SetPositionAndRotation(m.position, m.rotation);
                    v.transform.localScale = m.lossyScale;
                    SemColisor(v);
                }
                vivas.gameObject.SetActive(false);

                // Se houver mais de uma morta, o grupo delas fica sob uma mesma raiz para ligar/desligar junto
                GameObject mortasRaiz;
                if (daPlataforma.Count == 1)
                {
                    mortasRaiz = daPlataforma[0].gameObject;
                }
                else
                {
                    mortasRaiz = new GameObject(plat.name + "_ArvoresMortas");
                    mortasRaiz.transform.SetParent(pai, false);
                    foreach (Transform m in daPlataforma) m.SetParent(mortasRaiz.transform, true);
                }
                so.FindProperty("arvoreSeca").objectReferenceValue = mortasRaiz;
                so.FindProperty("arvoreViva").objectReferenceValue = vivas.gameObject;
            }
            else if (seca != null && plat.transform.parent != null && Random.value < 0.4f
                     && Mathf.Min(b.size.x, b.size.z) >= 2.4f)
            {
                // Algumas plataformas ganham uma árvore morta (que vira viva e cresce quando ela refloresce)
                Transform pai = plat.transform.parent;
                float lado = Random.value < 0.5f ? -1f : 1f;
                Vector3 baseArvore = new Vector3(b.center.x, b.max.y, b.center.z + lado * b.extents.z * 0.7f);
                float escala = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) / 4f, 0.55f, 0.9f);
                Quaternion giro = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(seca, pai);
                s.transform.SetPositionAndRotation(baseArvore, giro);
                s.transform.localScale = Vector3.one * escala;
                SemColisor(s);

                Transform vivas = new GameObject(plat.name + "_VivasGemeas").transform;
                vivas.SetParent(pai, false);
                GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(viva, vivas);
                v.transform.SetPositionAndRotation(baseArvore, giro);
                v.transform.localScale = Vector3.one * escala;
                SemColisor(v);
                vivas.gameObject.SetActive(false);

                so.FindProperty("arvoreSeca").objectReferenceValue = s;
                so.FindProperty("arvoreViva").objectReferenceValue = vivas.gameObject;
            }
            so.ApplyModifiedProperties();
        }
        Random.state = estado;
    }

    /// <summary>Árvore morta espalhada no chão do mundo em volta (filha direta de MundoAoRedor).</summary>
    private static bool EhArvoreDoMundo(Object o)
    {
        GameObject g = o as GameObject;
        return g != null && g.name == "ArvoreSeca" && g.transform.parent != null && g.transform.parent.name == "MundoAoRedor";
    }

    /// <summary>
    /// Cada arbusto morto do cenário ganha um arbusto vivo no mesmo lugar (desligado). Quando o jogador
    /// chega ao final, a Nascente troca os mortos pelos vivos.
    /// </summary>
    private static void ArbustosDoFinal()
    {
        NascenteFinal nascente = Object.FindFirstObjectByType<NascenteFinal>();
        GameObject vivo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArbustoVerde.prefab");
        if (nascente == null || vivo == null) return;

        // Limpa os gêmeos de rodadas anteriores
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && (t.name == "ArbustoVerde_Gemeo" || t.name == "ArvoreVerde_Gemeo")) Object.DestroyImmediate(t.gameObject);
        }

        SerializedObject so = new SerializedObject(nascente);
        SerializedProperty secas = so.FindProperty("arvoresSecas");
        SerializedProperty verdes = so.FindProperty("arvoresVerdes");

        // Mantém o que já estava ligado (as árvores), tirando só arbustos de rodadas anteriores
        List<Object> secasAtuais = new List<Object>();
        List<Object> verdesAtuais = new List<Object>();
        for (int i = 0; i < secas.arraySize; i++)
        {
            Object o = secas.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null && o.name != "ArbustoSeco" && !EhArvoreDoMundo(o)) secasAtuais.Add(o);
        }
        for (int i = 0; i < verdes.arraySize; i++)
        {
            Object o = verdes.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null && !o.name.EndsWith("_Gemeo")) verdesAtuais.Add(o);
        }

        GameObject arvoreViva = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreVerde.prefab");
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;

            if (t.name == "ArbustoSeco")
            {
                GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(vivo, t.parent);
                v.name = "ArbustoVerde_Gemeo";
                v.transform.SetPositionAndRotation(t.position, t.rotation);
                v.transform.localScale = t.localScale;
                SemColisor(v);
                v.SetActive(false);
                secasAtuais.Add(t.gameObject);
                verdesAtuais.Add(v);
            }
            else if (EhArvoreDoMundo(t.gameObject) && arvoreViva != null)
            {
                // Árvores do horizonte: a viva tem a mesma altura e o mesmo lugar da morta
                GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(arvoreViva, t.parent);
                v.name = "ArvoreVerde_Gemeo";
                v.transform.SetPositionAndRotation(t.position, t.rotation);
                SemColisor(v);
                if (Limites(t, out Bounds bm) && Limites(v.transform, out Bounds bv))
                {
                    v.transform.localScale *= bm.size.y / Mathf.Max(bv.size.y, 0.0001f);
                    Limites(v.transform, out bv);
                    v.transform.position += new Vector3(0f, bm.min.y - bv.min.y, 0f);
                }
                v.SetActive(false);
                secasAtuais.Add(t.gameObject);
                verdesAtuais.Add(v);
            }
        }

        secas.arraySize = secasAtuais.Count;
        verdes.arraySize = verdesAtuais.Count;
        for (int i = 0; i < secasAtuais.Count; i++) secas.GetArrayElementAtIndex(i).objectReferenceValue = secasAtuais[i];
        for (int i = 0; i < verdesAtuais.Count; i++) verdes.GetArrayElementAtIndex(i).objectReferenceValue = verdesAtuais[i];
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// A ilha da casa da forja reflorescerá ao ser pisada: o chão fica verde e as árvores mortas
    /// dão lugar a árvores vivas.
    /// </summary>
    private static void ReviverIlhaDaForja(GameObject forja)
    {
        if (forja == null) return;
        Transform ilha = forja.transform.Find("IlhaDaForja");
        GameObject seca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreSeca.prefab");
        GameObject viva = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreVerde.prefab");
        if (ilha == null || seca == null || viva == null) return;

        // Grupos de árvores: as mortas (a que já existia e uma nova) e as vivas nos mesmos lugares
        foreach (string nome in new[] { "ArvoresDaIlha_Mortas", "ArvoresDaIlha_Vivas" })
        {
            Transform velho = forja.transform.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
        }
        Transform mortas = new GameObject("ArvoresDaIlha_Mortas").transform;
        Transform vivas = new GameObject("ArvoresDaIlha_Vivas").transform;
        mortas.SetParent(forja.transform, false);
        vivas.SetParent(forja.transform, false);

        // Longe da forja e das ferramentas, que ocupam a frente e o lado direito da ilha
        Vector3[] lugares = { new Vector3(68.9f, 5f, 6.8f), new Vector3(68.9f, 5f, 13.8f), new Vector3(79.2f, 5f, 14.2f) };
        // A árvore morta que o construtor colocou na ilha sai: as novas ocupam o lugar
        Transform antiga = null;
        foreach (Transform c in forja.transform)
        {
            if (c.name == "ArvoreSeca" && Vector3.Distance(c.position, lugares[0]) < 0.5f) antiga = c;
        }
        if (antiga != null) Object.DestroyImmediate(antiga.gameObject);

        for (int i = 0; i < lugares.Length; i++)
        {
            float giro = 40f * i + 10f;
            GameObject s = (GameObject)PrefabUtility.InstantiatePrefab(seca, mortas);
            GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(viva, vivas);
            foreach (GameObject g in new[] { s, v })
            {
                g.transform.SetPositionAndRotation(lugares[i], Quaternion.Euler(0f, giro, 0f));
                SemColisor(g);
            }
        }
        vivas.gameObject.SetActive(false);

        PlataformaReflorescente refl = ilha.GetComponent<PlataformaReflorescente>();
        if (refl == null) refl = ilha.gameObject.AddComponent<PlataformaReflorescente>();
        refl.materialVerde = MaterialChao("GramaIlha", "PT_Ground_Grass_Green_01.png", 6f, Color.white);
        refl.spawner = Object.FindFirstObjectByType<SpawnerMudas>();
        SerializedObject so = new SerializedObject(refl);
        so.FindProperty("arvoreSeca").objectReferenceValue = mortas.gameObject;
        so.FindProperty("arvoreViva").objectReferenceValue = vivas.gameObject;
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Liga à Nascente o chão, o morro da vila, os morros das ilhas e as colinas, que ficam verdes
    /// quando o jogador chega ao final.
    /// </summary>
    private static void LigarTerrenoVivo()
    {
        NascenteFinal nascente = Object.FindFirstObjectByType<NascenteFinal>();
        GameObject mundo = GameObject.Find("MundoAoRedor");
        if (nascente == null || mundo == null) return;

        Material chaoVivo = MaterialChao("ChaoMundoVivo", "PT_Ground_Grass_Green_01.png", 160f, new Color(0.85f, 1f, 0.7f));
        Material morroVivo = MaterialChao("MorroGrama", "PT_Ground_Grass_Green_01.png", 1f, new Color(0.85f, 1f, 0.7f));
        Material montanhaViva = MaterialLiso("MontanhaVerde", new Color(0.30f, 0.62f, 0.20f));
        Material colinaViva = MaterialLiso("ColinaVerde", new Color(0.40f, 0.78f, 0.22f));

        List<Renderer> mortos = new List<Renderer>();
        List<Material> vivos = new List<Material>();
        foreach (Renderer r in mundo.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.transform.name;
            // Sobe até o filho direto de MundoAoRedor para saber de qual objeto o renderer faz parte
            Transform raiz = r.transform;
            while (raiz.parent != null && raiz.parent != mundo.transform) raiz = raiz.parent;
            string nr = raiz.name;

            Material vivo = null;
            if (nr == "ChaoDoVale") vivo = chaoVivo;
            else if (nr == "MontanhaDaVila" || nr.StartsWith("Morro_")) vivo = morroVivo;
            else if (nr.Contains("mountain")) vivo = montanhaViva;
            else if (nr.Contains("hill")) vivo = colinaViva;
            if (vivo == null || n == null) continue;
            mortos.Add(r);
            vivos.Add(vivo);
        }

        // O chão da praça e o bloco da fonte fazem parte dos morros (Morro_Praca e Morro_Nascente)

        SerializedObject so = new SerializedObject(nascente);
        SerializedProperty pm = so.FindProperty("terrenoMorto");
        SerializedProperty pv = so.FindProperty("terrenoVivo");
        pm.arraySize = mortos.Count;
        pv.arraySize = vivos.Count;
        for (int i = 0; i < mortos.Count; i++)
        {
            pm.GetArrayElementAtIndex(i).objectReferenceValue = mortos[i];
            pv.GetArrayElementAtIndex(i).objectReferenceValue = vivos[i];
        }
        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Morros de terra embaixo da praça, da ilha da casa, da ilha do moinho e do bloco da fonte.
    /// Cada morro é uma malha própria: o topo plano tem exatamente o contorno do bloco (que fica
    /// invisível, só com o colisor) e as laterais descem em rampas irregulares até o chão do mundo.
    /// </summary>
    private static void CriarMorros()
    {
        GameObject mundo = GameObject.Find("MundoAoRedor");
        if (mundo == null) return;

        // Antigos, de rodadas anteriores
        foreach (string velho in new[] { "MontanhaDaVila", "Morro_Forja", "Morro_Moinho", "Morro_Nascente", "Morro_Praca" })
        {
            Transform t = mundo.transform.Find(velho);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        Material terra = MaterialChao("MorroTerra", "PT_Ground_Generic_03.png", 1f, new Color(0.75f, 0.70f, 0.62f));

        // nome do morro, grupo da cena, nome do bloco
        string[][] alvos =
        {
            new[] { "Morro_Praca", "1_PracaDaVila", "ChaoPraca" },
            new[] { "Morro_Forja", "4_ChamineDaForja", "IlhaDaForja" },
            new[] { "Morro_Moinho", "3_ColinaDosMoinhos", "IlhaDoMoinho" },
            new[] { "Morro_Nascente", "5_NascenteDoVale", "ColinaDaNascente" },
        };
        int semente = 3;
        foreach (string[] alvo in alvos)
        {
            GameObject grupo = GameObject.Find(alvo[1]);
            Transform bloco = grupo != null ? grupo.transform.Find(alvo[2]) : null;
            if (bloco == null) continue;

            Vector3 c = bloco.position;
            Vector3 s = bloco.lossyScale;
            float topo = c.y + s.y * 0.5f;
            Mesh malha = MalhaDeMorro(alvo[0], new Vector3(c.x, topo, c.z), s.x * 0.5f, s.z * 0.5f, yChao, semente++);

            GameObject morro = new GameObject(alvo[0]);
            morro.transform.SetParent(mundo.transform, false);
            morro.AddComponent<MeshFilter>().sharedMesh = malha;
            MeshRenderer mr = morro.AddComponent<MeshRenderer>();
            mr.sharedMaterial = terra;

            // O bloco fica só com o colisor: quem aparece é o topo do morro
            MeshRenderer visualBloco = bloco.GetComponent<MeshRenderer>();
            if (visualBloco != null) visualBloco.enabled = false;

            // A ilha da casa reverdece ao ser pisada: o material que troca passa a ser o do morro
            PlataformaReflorescente refl = bloco.GetComponent<PlataformaReflorescente>();
            if (refl != null)
            {
                SerializedObject so = new SerializedObject(refl);
                so.FindProperty("rendererPlataforma").objectReferenceValue = mr;
                so.ApplyModifiedProperties();
                refl.materialVerde = MaterialChao("MorroGrama", "PT_Ground_Grass_Green_01.png", 1f, new Color(0.85f, 1f, 0.7f));
                EditorUtility.SetDirty(refl);
            }
        }
    }

    /// <summary>
    /// Malha de um morro: topo plano com o contorno do retângulo (hx, hz) e laterais que descem em
    /// rampas irregulares até o chão, com ruído nas distâncias para parecer rocha e terra.
    /// </summary>
    private static Mesh MalhaDeMorro(string nome, Vector3 centroTopo, float hx, float hz, float chaoY, int semente)
    {
        // Ângulos ao redor: uniformes mais os quatro cantos do retângulo (para o topo ficar exato)
        List<float> angulos = new List<float>();
        const int uniformes = 72;
        for (int i = 0; i < uniformes; i++) angulos.Add(i * Mathf.PI * 2f / uniformes);
        float ca = Mathf.Atan2(hz, hx);
        angulos.Add(ca);
        angulos.Add(Mathf.PI - ca);
        angulos.Add(Mathf.PI + ca);
        angulos.Add(Mathf.PI * 2f - ca);
        angulos.Sort();
        int n = angulos.Count;

        float altura = Mathf.Max(centroTopo.y - chaoY, 1f);
        float k = altura / 19f; // os deslocamentos foram pensados para 19 m de altura
        float[] deslocamento = { 0f, 0.4f, 2f, 4.5f, 8f, 12f, 16f };
        float[] descida = { 0f, 0.03f, 0.14f, 0.33f, 0.55f, 0.8f, 1.03f };
        int aneis = deslocamento.Length;

        Vector3[] v = new Vector3[1 + aneis * n];
        Vector2[] uv = new Vector2[v.Length];
        v[0] = centroTopo;
        for (int r = 0; r < aneis; r++)
        {
            for (int i = 0; i < n; i++)
            {
                float a = angulos[i];
                float cx = Mathf.Cos(a), sz = Mathf.Sin(a);
                // Distância do centro até a borda do retângulo nesse ângulo
                float d = Mathf.Min(hx / Mathf.Max(Mathf.Abs(cx), 0.0001f), hz / Mathf.Max(Mathf.Abs(sz), 0.0001f));

                float ruido = 0f, salto = 0f;
                if (r >= 2)
                {
                    ruido = (Mathf.PerlinNoise(cx * 1.9f + semente * 7.1f, sz * 1.9f + r * 0.43f) - 0.5f) * 2f;
                    salto = (Mathf.PerlinNoise(cx * 4.3f + semente * 3.3f, sz * 4.3f + r * 1.7f) - 0.5f) * 0.7f * k;
                }
                float dist = d + deslocamento[r] * k * (1f + 0.35f * ruido);
                float y = centroTopo.y - altura * descida[r] + salto;
                if (r == aneis - 1) y = chaoY - 0.4f; // enterra a base no chão

                v[1 + r * n + i] = new Vector3(centroTopo.x + cx * dist, y, centroTopo.z + sz * dist);
            }
        }
        for (int i = 0; i < v.Length; i++) uv[i] = new Vector2(v[i].x, v[i].z) / 5f;

        List<int> t = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            t.Add(0); t.Add(1 + j); t.Add(1 + i); // topo
        }
        for (int r = 0; r < aneis - 1; r++)
        {
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                int a = 1 + r * n + i, b = 1 + r * n + j, c = 1 + (r + 1) * n + i, dd = 1 + (r + 1) * n + j;
                t.Add(a); t.Add(b); t.Add(c);
                t.Add(b); t.Add(dd); t.Add(c);
            }
        }

        Mesh m = new Mesh { name = nome };
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.vertices = v;
        m.uv = uv;
        m.triangles = t.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();

        string caminho = PastaMateriais + "Mesh_" + nome + ".asset";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(m, caminho);
        return AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
    }

    /// <summary>
    /// Aumenta cada plataforma até 30% na largura e profundidade, mas só até onde ela não encosta
    /// em outra plataforma nem nas pás do moinho (que giram e travariam).
    /// </summary>
    private static void AjustarTamanhos()
    {
        PlataformaReflorescente[] todas = Object.FindObjectsByType<PlataformaReflorescente>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // 1) Volta tudo ao tamanho original (a marca guarda o fator aplicado em rodadas anteriores)
        foreach (PlataformaReflorescente plat in todas)
        {
            if (plat.name == "IlhaDaForja") continue;
            Transform pt = plat.transform;
            float fator = 1f;
            Transform marca = null;
            foreach (Transform filho in pt)
            {
                if (filho.name.StartsWith("_Ampliada")) { marca = filho; break; }
            }
            if (marca != null)
            {
                string[] partes = marca.name.Split('|');
                fator = partes.Length > 1 && float.TryParse(partes[1], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float f) ? f : 1.3f;
                Object.DestroyImmediate(marca.gameObject);
            }
            if (fator != 1f) EscalarPlataforma(plat, 1f / fator);
        }

        // 2) Caixas proibidas: as pás do moinho (toda a volta, pois giram) e a torre
        List<Bounds> fixos = new List<Bounds>();
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "Pas") continue;
            float raio = 0f;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
            {
                Vector3 d = r.bounds.center - t.position;
                raio = Mathf.Max(raio, Mathf.Sqrt(d.x * d.x + d.y * d.y) + Mathf.Max(r.bounds.extents.x, r.bounds.extents.y));
            }
            raio = Mathf.Max(raio, 6f) + 0.3f;
            fixos.Add(new Bounds(t.position, new Vector3(raio * 2f, raio * 2f, 3.2f)));
        }

        // 3) Cresce uma a uma, escolhendo o maior fator que não encosta em nada
        float[] normais = { 1.3f, 1.2f, 1.1f, 1.0f };
        float[] grandes = { 1.8f, 1.65f, 1.5f, 1.35f, 1.2f, 1.1f, 1.0f }; // quadrados da forja
        foreach (PlataformaReflorescente plat in todas)
        {
            if (plat.name == "IlhaDaForja") continue;
            Transform pt = plat.transform;
            float escolhido = 1f;
            foreach (float opcao in plat.name.StartsWith("Forja_") ? grandes : normais)
            {
                Bounds b = new Bounds(pt.position, new Vector3(pt.lossyScale.x * opcao + 0.6f, pt.lossyScale.y, pt.lossyScale.z * opcao + 0.6f));
                bool bate = false;
                foreach (Bounds fixo in fixos) if (fixo.Intersects(b)) { bate = true; break; }
                if (!bate)
                {
                    foreach (PlataformaReflorescente outra in todas)
                    {
                        if (outra == plat) continue;
                        Transform ot = outra.transform;
                        Bounds bo = new Bounds(ot.position, ot.lossyScale);
                        if (bo.Intersects(b)) { bate = true; break; }
                    }
                }
                if (!bate) { escolhido = opcao; break; }
            }

            if (escolhido != 1f)
            {
                EscalarPlataforma(plat, escolhido);
                new GameObject("_Ampliada|" + escolhido.ToString(System.Globalization.CultureInfo.InvariantCulture)).transform.SetParent(plat.transform, false);
            }
        }
    }

    /// <summary>Escala a plataforma e as vigas de apoio na largura e profundidade (a altura fica igual).</summary>
    private static void EscalarPlataforma(PlataformaReflorescente plat, float fator)
    {
        Transform pt = plat.transform;
        pt.localScale = new Vector3(pt.localScale.x * fator, pt.localScale.y, pt.localScale.z * fator);
        if (pt.parent == null) return;
        foreach (string viga in new[] { "_Viga1", "_Viga2" })
        {
            Transform v = pt.parent.Find(plat.name + viga);
            if (v != null) v.localScale = new Vector3(v.localScale.x * fator, v.localScale.y, v.localScale.z);
        }
    }

    // Onde e como o mundo é espalhado (muda entre o nível, que tem abismo, e o menu).
    private static float yChao = ChaoMundo;
    private static Vector3 centroMundo = new Vector3(60f, 0f, 0f);
    private static Rect proibido = new Rect(-35f, -40f, 160f, 80f); // x, z, largura, profundidade
    private static float raioMax = 220f;

    private static void ConstruirMundo(bool menu = false)
    {
        if (menu)
        {
            yChao = -0.06f;
            centroMundo = Vector3.zero;
            proibido = new Rect(-24f, -24f, 48f, 48f);
            raioMax = 140f;
        }
        else
        {
            yChao = ChaoMundo;
            centroMundo = new Vector3(60f, 0f, 0f);
            proibido = new Rect(-35f, -40f, 160f, 80f);
            raioMax = 220f;
        }

        GameObject velho = GameObject.Find("MundoAoRedor");
        if (velho != null) Object.DestroyImmediate(velho);
        GameObject mundo = new GameObject("MundoAoRedor");

        // Chão com textura repetida
        // Mundo morto em volta: terra seca, colinas áridas e árvores mortas
        Material matChao = MaterialChao("ChaoMundo", "PT_Ground_Generic_03.png", 160f, new Color(0.75f, 0.70f, 0.62f));
        Material matMontanha = MaterialLiso("MontanhaMorta", new Color(0.34f, 0.30f, 0.26f));
        Material matColina = MaterialLiso("ColinaMorta", new Color(0.44f, 0.39f, 0.31f));

        GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "ChaoDoVale";
        chao.transform.SetParent(mundo.transform);
        chao.transform.position = new Vector3(centroMundo.x, yChao, centroMundo.z);
        float lado = menu ? 60f : 90f;
        chao.transform.localScale = new Vector3(lado, 1f, lado);
        Object.DestroyImmediate(chao.GetComponent<Collider>());
        chao.GetComponent<MeshRenderer>().sharedMaterial = matChao;

        // Cenário que sai das montanhas esféricas do construtor
        GameObject fundo = GameObject.Find("CenarioDeFundo");
        if (fundo != null)
        {
            for (int i = fundo.transform.childCount - 1; i >= 0; i--)
            {
                Transform c = fundo.transform.GetChild(i);
                // Montanhas e pedras de esfera, e o muro flutuante ao longe (cubos soltos no céu)
                if (c.name.StartsWith("Montanha") || c.name.StartsWith("PedraFlutuante") || c.name.StartsWith("Muralhas"))
                {
                    Object.DestroyImmediate(c.gameObject);
                }
            }
        }

        // Névoa mais fraca para o relevo distante aparecer, na cor cinza-marrom do vale
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = new Color(0.42f, 0.40f, 0.38f);

        Random.State estado = Random.state;
        Random.InitState(7);

        // Espalha árvores, arbustos e rochas fora do corredor do nível (z entre -22 e 30 fica mais limpo)
        string[] arvores = { "Assets/Prefabs/ArvoreSeca.prefab" };
        string[] pequenos = { "Assets/Prefabs/ArbustoSeco.prefab" };

        // Sem pedras soltas no chão do mundo
        Espalhar(mundo.transform, arvores, menu ? 90 : 130, 6f, 11f);
        Espalhar(mundo.transform, pequenos, menu ? 60 : 90, 1.2f, 1.8f);

        // Ruínas: só entulho de pedra do pack de masmorra (sem colunas soltas no meio do nada)
        Espalhar(mundo.transform, new[] { Masmorra + "Individuals/Edgerubble Top.prefab", Masmorra + "Individuals/Edgerubble Bottom.prefab" },
                 menu ? 6 : 10, 1.5f, 3f, true);

        // Anel de colinas e montanhas no horizonte
        string[] relevo =
        {
            Rpg + "Nature/Terrain/rpgpp_lt_mountain_01.prefab", Rpg + "Nature/Terrain/rpgpp_lt_hill_small_01.prefab",
            Rpg + "Nature/Terrain/rpgpp_lt_hill_small_02.prefab"
        };
        for (int i = 0; i < 22; i++)
        {
            float ang = i / 22f * Mathf.PI * 2f;
            float raio = menu ? Random.Range(150f, 190f) : Random.Range(210f, 260f);
            Vector3 pos = new Vector3(centroMundo.x + Mathf.Cos(ang) * raio * (menu ? 1f : 1.3f), yChao, centroMundo.z + Mathf.Sin(ang) * raio);
            string caminho = relevo[i % 3 == 0 ? 0 : 1 + i % 2];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            if (prefab == null) continue;
            GameObject o = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mundo.transform);
            o.transform.position = pos;
            o.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            SemColisor(o);
            // Relevo árido, de cor lisa
            Material verde = i % 3 == 0 ? matMontanha : matColina;
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                Material[] ms = r.sharedMaterials;
                for (int k = 0; k < ms.Length; k++) ms[k] = verde;
                r.sharedMaterials = ms;
            }
            if (Limites(o.transform, out Bounds b))
            {
                float largura = i % 3 == 0 ? Random.Range(120f, 170f) : Random.Range(70f, 110f);
                if (menu) largura *= 0.7f;
                o.transform.localScale *= largura / Mathf.Max(b.size.x, b.size.z, 0.0001f);
                Limites(o.transform, out b);
                o.transform.position += new Vector3(0f, yChao - b.min.y - 2f, 0f);
            }
        }

        Random.state = estado;
        if (menu) ReformarMenu();
        else ReformarVila(mundo.transform);
    }

    /// <summary>
    /// Menu: chão da vila seco e árvores mortas, como no jogo (o verde só vem com a restauração).
    /// </summary>
    private static void ReformarMenu()
    {
        GameObject vila = GameObject.Find("Vila");
        if (vila == null) return;

        Material seco = MaterialChao("ChaoTerraMorta", "PT_Ground_Generic_03.png", 10f, new Color(0.8f, 0.75f, 0.68f));
        Transform chao = vila.transform.Find("Chao");
        if (chao != null && seco != null) chao.GetComponent<MeshRenderer>().sharedMaterial = seco;

        // Desfaz a troca por árvores vivas de uma rodada anterior
        GameObject seca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArvoreSeca.prefab");
        if (seca != null)
        {
            for (int i = vila.transform.childCount - 1; i >= 0; i--)
            {
                Transform viva = vila.transform.GetChild(i);
                if (viva.name != "ArvoreVerde") continue;
                GameObject nova = (GameObject)PrefabUtility.InstantiatePrefab(seca, vila.transform);
                nova.transform.SetPositionAndRotation(viva.position, viva.rotation);
                Object.DestroyImmediate(viva.gameObject);
            }
        }
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.fogColor = new Color(0.42f, 0.40f, 0.38f);
    }

    private static Material CriarOuPegarMaterial(string nome)
    {
        string caminho = PastaMateriais + nome + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, caminho);
        }
        return m;
    }

    private static Material MaterialChao(string nome, string textura, float repeticoes, Color cor)
    {
        Material m = CriarOuPegarMaterial(nome);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "Lowpoly_Environments/Sources/Textures/" + textura);
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", new Vector2(repeticoes, repeticoes));
        m.SetColor("_BaseColor", cor);
        m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static Material MaterialLiso(string nome, Color cor)
    {
        Material m = CriarOuPegarMaterial(nome);
        m.SetColor("_BaseColor", cor);
        m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>
    /// Vila no alto de uma montanha: sem roda d'água, calha, leito do rio e pilares embaixo;
    /// chão da praça verde; cerca em volta de toda a vila (com a saída para a ponte).
    /// </summary>
    private static void ReformarVila(Transform mundo)
    {
        GameObject praca = GameObject.Find("1_PracaDaVila");
        if (praca == null) return;
        Transform p = praca.transform;

        // Roda d'água (com a calha de madeira em cima), rocha flutuante embaixo da praça
        foreach (string nome in new[] { "RodaDagua", "ChaoPraca_Rocha" })
        {
            Transform t = p.Find(nome);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // Chão voador lá embaixo: leito do rio seco, pilares e pedras do rio
        GameObject ponte = GameObject.Find("2_PontePartida");
        if (ponte != null)
        {
            for (int i = ponte.transform.childCount - 1; i >= 0; i--)
            {
                Transform c = ponte.transform.GetChild(i);
                if (c.name == "LeitoDoRioSeco" || c.name.StartsWith("PilarQuebrado") || c.name.StartsWith("PedraDoRio"))
                {
                    Object.DestroyImmediate(c.gameObject);
                }
            }
        }

        // Chão da praça seco e destruído, como antes: ele só reverdece quando o jogo restaura o vale
        Material seco = MaterialChao("ChaoTerraMorta", "PT_Ground_Generic_03.png", 10f, new Color(0.8f, 0.75f, 0.68f));
        Transform chao = p.Find("ChaoPraca");
        if (chao != null && seco != null) chao.GetComponent<MeshRenderer>().sharedMaterial = seco;

        // O morro embaixo da vila é criado em CriarMorros, junto com os das ilhas

        // Cerca em volta da praça (x de -20 a 10, z de -15 a 15), aberta no lado leste onde fica a saída
        for (int i = p.childCount - 1; i >= 0; i--)
        {
            if (p.GetChild(i).name == "Cerca") Object.DestroyImmediate(p.GetChild(i).gameObject);
        }
        GameObject cerca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cerca.prefab");
        if (cerca == null) return;

        for (float x = -18.5f; x < 10f; x += 3f)
        {
            ColocarCerca(cerca, p, new Vector3(x, 0f, 14.6f), 0f);
            ColocarCerca(cerca, p, new Vector3(x, 0f, -14.6f), 0f);
        }
        for (float z = -13.5f; z < 15f; z += 3f)
        {
            ColocarCerca(cerca, p, new Vector3(-19.6f, 0f, z), 90f);
            if (Mathf.Abs(z) > 6f) ColocarCerca(cerca, p, new Vector3(9.6f, 0f, z), 90f);
        }
    }

    private static void ColocarCerca(GameObject prefab, Transform pai, Vector3 pos, float rotY)
    {
        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
        c.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
    }

    /// <summary>
    /// Coloca instâncias aleatórias dos prefabs no chão do mundo, fora do espaço do nível.
    /// </summary>
    private static void Espalhar(Transform pai, string[] caminhos, int quantidade, float alturaMin, float alturaMax, bool maiorLado = false)
    {
        int colocados = 0, tentativas = 0;
        while (colocados < quantidade && tentativas++ < quantidade * 20)
        {
            Vector2 d = Random.insideUnitCircle * raioMax;
            Vector3 pos = new Vector3(centroMundo.x + d.x, yChao, centroMundo.z + d.y);
            // Mantém livre a área do nível (ou da vila no menu).
            if (proibido.Contains(new Vector2(pos.x, pos.z))) continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhos[Random.Range(0, caminhos.Length)]);
            if (prefab == null) continue;
            GameObject o = (GameObject)PrefabUtility.InstantiatePrefab(prefab, pai);
            o.transform.position = pos;
            o.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            CorrigirMateriais(o);
            SemColisor(o);
            if (Limites(o.transform, out Bounds b))
            {
                // Entulho achatado: dimensiona pelo maior lado, senão cresce até virar uma laje enorme
                float referencia = maiorLado ? Mathf.Max(b.size.x, b.size.y, b.size.z) : b.size.y;
                o.transform.localScale *= Random.Range(alturaMin, alturaMax) / Mathf.Max(referencia, 0.0001f);
                Limites(o.transform, out b);
                o.transform.position += new Vector3(0f, yChao - b.min.y, 0f);
            }
            colocados++;
        }
    }

    private static void SemColisor(GameObject go)
    {
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
    }

    private static bool TrocarJogador()
    {
        GameObject bran = GameObject.Find("Bran");
        GameObject padre = AssetDatabase.LoadAssetAtPath<GameObject>(Padre);
        if (bran == null || padre == null) return false;

        Transform modelo = bran.transform.Find("Modelo");
        if (modelo == null) return false;

        for (int i = modelo.childCount - 1; i >= 0; i--)
        {
            Transform filho = modelo.GetChild(i);
            if (filho.name == "LuzDaSemente" || filho.name == "Padre") { if (filho.name == "Padre") Object.DestroyImmediate(filho.gameObject); continue; }
            Object.DestroyImmediate(filho.gameObject);
        }

        GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(padre, modelo);
        p.name = "Padre";
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;
        p.transform.localScale = Vector3.one;
        foreach (Animator a in p.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(a);
        foreach (Collider c in p.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        CorrigirMateriais(p);

        // O Animator do Bran passa a usar o esqueleto humanoide do padre.
        Animator animador = modelo.GetComponent<Animator>();
        if (animador != null)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(PadreMalha))
            {
                if (o is Avatar av) { animador.avatar = av; break; }
            }
            animador.applyRootMotion = false;
            PosePadrao(animador.runtimeAnimatorController);
        }
        return true;
    }

    /// <summary>
    /// Enquanto não há animações do Mixamo, usa a pose do pack nos estados que estão sem clipe.
    /// </summary>
    private static void PosePadrao(RuntimeAnimatorController rc)
    {
        UnityEditor.Animations.AnimatorController ctrl = rc as UnityEditor.Animations.AnimatorController;
        if (ctrl == null) return;

        AnimationClip pose = PrimeiroClipe(PadrePose, false);

        // Animações do pack Kevin Iglesias (humanoides, servem no padre), por estado do Animator
        string ki = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";
        Dictionary<string, AnimationClip> clipes = new Dictionary<string, AnimationClip>
        {
            { "Parado", PrimeiroClipe(ki + "Idles/HumanM@Idle01.fbx", true) },
            { "Andando", PrimeiroClipe(ki + "Movement/Walk/HumanM@Walk01_Forward.fbx", true) },
            { "Correndo", PrimeiroClipe(ki + "Movement/Sprint/HumanM@Sprint01_Forward.fbx", true) },
            { "Pulando", PrimeiroClipe(ki + "Movement/Jump/HumanM@Jump01.fbx", false) },
            { "Caindo", PrimeiroClipe(ki + "Movement/Jump/HumanM@Fall01.fbx", true) },
        };

        foreach (var estado in ctrl.layers[0].stateMachine.states)
        {
            if (clipes.TryGetValue(estado.state.name, out AnimationClip clipe) && clipe != null)
            {
                estado.state.motion = clipe;
            }
            else if (estado.state.motion == null)
            {
                estado.state.motion = pose;
            }

            // Andar agora é 5 m/s: o limite andar/correr sobe de 5 para 6 para o andar não virar corrida
            foreach (AnimatorStateTransition tr in estado.state.transitions)
            {
                AnimatorCondition[] conds = tr.conditions;
                for (int i = 0; i < conds.Length; i++)
                {
                    if (conds[i].parameter == "Velocidade" && Mathf.Approximately(conds[i].threshold, 5f))
                    {
                        tr.RemoveCondition(conds[i]);
                        tr.AddCondition(conds[i].mode, 6f, "Velocidade");
                    }
                }
            }
        }
        EditorUtility.SetDirty(ctrl);
    }

    /// <summary>
    /// Primeiro clipe de um FBX de animação. Se pedido, liga o loop no importador.
    /// </summary>
    private static AnimationClip PrimeiroClipe(string caminho, bool repetir)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(caminho) == null)
        {
            Debug.LogWarning("TrocarAssets: não achei " + caminho);
            return null;
        }

        if (repetir)
        {
            ModelImporter mi = AssetImporter.GetAtPath(caminho) as ModelImporter;
            if (mi != null)
            {
                ModelImporterClipAnimation[] clips = mi.clipAnimations;
                if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
                bool mudou = false;
                foreach (ModelImporterClipAnimation c in clips)
                {
                    if (!c.loopTime) { c.loopTime = true; mudou = true; }
                }
                if (mudou)
                {
                    mi.clipAnimations = clips;
                    mi.SaveAndReimport();
                }
            }
        }

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
        {
            if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
        }
        return null;
    }

    private const string Menhir =Pedras + "PT_Menhir_Rock_02.prefab";

    /// <summary>
    /// Pedras de fala: mantém a raiz (trigger, script PedraRunica e luz Brilho)
    /// e troca só a pedra e os símbolos pelo menhir com a runa.
    /// </summary>
    private static void TrocarPedrasRunicas()
    {
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Menhir);
        if (modelo == null)
        {
            Debug.LogWarning("TrocarAssets: não achei " + Menhir);
            return;
        }

        foreach (string alvo in new[] { "Assets/Prefabs/PedraRunica.prefab", "Assets/Prefabs/PedraRunicaDourada.prefab" })
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(alvo);
            foreach (string nome in new[] { "Pedra", "Simbolo", "Simbolo2" })
            {
                Transform velho = raiz.transform.Find(nome);
                if (velho != null) Object.DestroyImmediate(velho.gameObject);
            }

            GameObject filho = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
            filho.name = "Modelo";
            filho.transform.localPosition = Vector3.zero;
            filho.transform.localRotation = Quaternion.identity;
            filho.transform.localScale = Vector3.one;
            CorrigirMateriais(filho);

            Bounds b = default;
            bool primeiro = true;
            foreach (Renderer r in filho.GetComponentsInChildren<Renderer>())
            {
                if (primeiro) { b = r.bounds; primeiro = false; }
                else b.Encapsulate(r.bounds);
            }
            if (!primeiro)
            {
                BoxCollider c = filho.AddComponent<BoxCollider>();
                c.center = filho.transform.InverseTransformPoint(b.center);
                c.size = b.size;

                Transform luz = raiz.transform.Find("Brilho");
                if (luz != null) luz.localPosition = new Vector3(0, b.size.y * 0.6f, 0);
            }

            PrefabUtility.SaveAsPrefabAsset(raiz, alvo);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    [MenuItem("Semente da Aurora/Trocar Assets (Polytope)")]
    public static void Executar()
    {
        Aplicar();
    }

    private static void Aplicar()
    {
        int feitos = 0;
        // Prefab novo do arbusto vivo: nasce como cópia do seco e depois recebe o modelo verde
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ArbustoVerde.prefab") == null)
        {
            AssetDatabase.CopyAsset("Assets/Prefabs/ArbustoSeco.prefab", "Assets/Prefabs/ArbustoVerde.prefab");
        }
        foreach (Troca t in Trocas)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(t.novo);
            if (modelo == null || AssetDatabase.LoadAssetAtPath<GameObject>(t.alvo) == null)
            {
                Debug.LogWarning("TrocarAssets: não achei " + (modelo == null ? t.novo : t.alvo));
                continue;
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(t.alvo);
            for (int i = raiz.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(raiz.transform.GetChild(i).gameObject);
            }
            foreach (Collider c in raiz.GetComponents<Collider>())
            {
                Object.DestroyImmediate(c);
            }

            GameObject filho = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
            filho.name = "Modelo";
            filho.transform.localPosition = Vector3.zero;
            filho.transform.localRotation = Quaternion.identity;
            filho.transform.localScale = Vector3.one * t.escala;

            CorrigirMateriais(filho);
            if (t.alinharX && Limites(filho.transform, out Bounds bf) && bf.size.z > bf.size.x)
            {
                filho.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            if (t.larguraAlvo > 0f || t.alturaAlvo > 0f) Ajustar(filho.transform, t.larguraAlvo, t.alturaAlvo);
            AdicionarColisor(raiz, filho, t.colisor);

            PrefabUtility.SaveAsPrefabAsset(raiz, t.alvo);
            PrefabUtility.UnloadPrefabContents(raiz);
            feitos++;
        }

        TrocarPedrasRunicas();
        TrocarTocha();
        TrocarCenas();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("TrocarAssets: " + feitos + "/" + Trocas.Length + " prefabs trocados.");
    }

    private static bool Limites(Transform t, out Bounds b)
    {
        b = default;
        bool primeiro = true;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
        {
            if (primeiro) { b = r.bounds; primeiro = false; }
            else b.Encapsulate(r.bounds);
        }
        return !primeiro;
    }

    /// <summary>
    /// Escala o modelo (uniforme) para a largura ou altura pedida e o apoia no chão, centrado no pai.
    /// O pai precisa estar na origem, sem rotação nem escala.
    /// </summary>
    private static void Ajustar(Transform modelo, float larguraAlvo, float alturaAlvo)
    {
        if (!Limites(modelo, out Bounds b)) return;
        float fator = 1f;
        if (larguraAlvo > 0f) fator = larguraAlvo / Mathf.Max(b.size.x, b.size.z, 0.0001f);
        else if (alturaAlvo > 0f) fator = alturaAlvo / Mathf.Max(b.size.y, 0.0001f);
        modelo.localScale *= fator;

        Limites(modelo, out b);
        modelo.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
    }

    /// <summary>
    /// Se algum material ainda usa um shader sem passe URP, cria um URP/Lit com a mesma textura.
    /// </summary>
    private static void CorrigirMateriais(GameObject go)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool mudou = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null || m.shader == null) continue;
                // Os shaders do pack não compilam no URP deste Unity (OUTPUT_SH4 / ApplyShadowClamping).
                bool doPack = m.shader.name.StartsWith("Polytope Studio");
                if (!doPack && m.FindPass("UniversalForward") >= 0) continue;

                string caminho = PastaMateriais + "URP_" + m.name + ".mat";
                Material novo = AssetDatabase.LoadAssetAtPath<Material>(caminho);
                bool criar = novo == null;
                if (criar)
                {
                    novo = new Material(lit);
                    AssetDatabase.CreateAsset(novo, caminho);
                }
                novo.SetTexture("_BaseMap", TexturaDe(m));
                novo.SetFloat("_Smoothness", 0.1f);

                string sh = m.shader.name;
                bool folha = sh.Contains("Foliage") || sh.Contains("Flowers") || sh.Contains("Plants");
                if (folha)
                {
                    // Folhas e flores têm transparência na textura: recorte + dois lados.
                    novo.SetFloat("_AlphaClip", 1f);
                    novo.SetFloat("_Cutoff", 0.5f);
                    novo.SetFloat("_Cull", 0f);
                    novo.EnableKeyword("_ALPHATEST_ON");
                    novo.renderQueue = 2450;
                }
                // A textura das folhas é cinza (o shader do pack colore); pinta de verde aqui
                novo.SetColor("_BaseColor", CorDaFolha(m.name, sh));
                EditorUtility.SetDirty(novo);
                mats[i] = novo;
                mudou = true;
            }
            if (mudou) r.sharedMaterials = mats;
        }
    }

    private static Color CorDaFolha(string material, string shader)
    {
        string n = material.ToLowerInvariant();
        // Só folhas e gramas; troncos, pedras e flores ficam com a cor da textura
        if (!shader.Contains("Foliage") && !shader.Contains("Plants")) return Color.white;
        if (n.Contains("pine")) return new Color(0.16f, 0.42f, 0.22f);
        if (n.Contains("fruit")) return new Color(0.38f, 0.65f, 0.20f);
        if (n.Contains("grass") || shader.Contains("Plants")) return new Color(0.45f, 0.70f, 0.25f);
        if (n.Contains("leaf") || n.Contains("leaves") || shader.Contains("Foliage")) return new Color(0.32f, 0.62f, 0.22f);
        return Color.white;
    }

    /// <summary>
    /// Textura principal do material: _MainTex ou a primeira textura preenchida.
    /// </summary>
    private static Texture TexturaDe(Material m)
    {
        if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) return m.GetTexture("_MainTex");
        foreach (string prop in m.GetTexturePropertyNames())
        {
            Texture t = m.GetTexture(prop);
            if (t != null) return t;
        }
        return null;
    }

    private static void AdicionarColisor(GameObject raiz, GameObject modelo, Colisor tipo)
    {
        if (tipo == Colisor.Nenhum) return;

        Bounds b = default;
        bool primeiro = true;
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
        {
            if (primeiro) { b = r.bounds; primeiro = false; }
            else b.Encapsulate(r.bounds);
        }
        if (primeiro) return;

        Vector3 centro = raiz.transform.InverseTransformPoint(b.center);
        if (tipo == Colisor.Capsula)
        {
            CapsuleCollider c = raiz.AddComponent<CapsuleCollider>();
            c.radius = 0.4f;
            c.height = Mathf.Min(b.size.y, 3.5f);
            c.center = new Vector3(0, c.height * 0.5f, 0);
        }
        else
        {
            BoxCollider c = raiz.AddComponent<BoxCollider>();
            c.center = centro;
            c.size = b.size;
        }
    }
}
