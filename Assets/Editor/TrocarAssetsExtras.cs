using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Segunda parte do TrocarAssets: céu com nuvens, tocha, forja, fonte, enfeites da vila,
/// casas e ruínas no cenário e rios. Roda junto com o menu Semente da Aurora → Trocar Assets.
/// </summary>
public static partial class TrocarAssets
{
    private const string Ronin = "Assets/PolyRonin/Medieval Blacksmith Props/Prefabs/";
    private const string Masmorra = "Assets/RPG Dungeon Pack Sample/Prefabs/";
    private const string FontePasta = "Assets/GVOZDY/Round Four-Tier Water Fountain/Prefabs/";
    private const string CeuBase = "Assets/Farland Skies/Low Poly/Skyboxes/LowPolySkybox.mat";

    // ------------------------------------------------------------------
    // Céu
    // ------------------------------------------------------------------

    /// <summary>
    /// Céu do pack Farland Skies: um morto (nuvens cinzas, pôr do sol) e um vivo (azul) para a vitória.
    /// </summary>
    private static void AplicarCeu(bool nivel1)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(CeuBase) == null)
        {
            Debug.LogWarning("TrocarAssets: não achei " + CeuBase);
            return;
        }

        Material morto = CopiaDoCeu("CeuMorto");
        morto.SetColor("_TopColor", new Color(0.36f, 0.34f, 0.37f));
        morto.SetColor("_MiddleColor", new Color(0.95f, 0.55f, 0.25f));
        morto.SetColor("_BottomColor", new Color(0.30f, 0.26f, 0.22f));
        morto.SetColor("_CloudsTint", new Color(0.55f, 0.50f, 0.50f));
        EditorUtility.SetDirty(morto);

        Material vivo = CopiaDoCeu("CeuVivo");
        vivo.SetColor("_TopColor", new Color(0.25f, 0.55f, 0.95f));
        vivo.SetColor("_MiddleColor", new Color(0.80f, 0.90f, 1f));
        vivo.SetColor("_BottomColor", new Color(0.55f, 0.70f, 0.50f));
        vivo.SetColor("_CloudsTint", Color.white);
        vivo.SetColor("_SunTint", new Color(1f, 0.95f, 0.8f));
        EditorUtility.SetDirty(vivo);

        RenderSettings.skybox = morto;
        DynamicGI.UpdateEnvironment();

        if (nivel1)
        {
            NascenteFinal nascente = Object.FindFirstObjectByType<NascenteFinal>();
            if (nascente != null)
            {
                SerializedObject so = new SerializedObject(nascente);
                so.FindProperty("skyboxRestaurado").objectReferenceValue = vivo;
                so.ApplyModifiedProperties();
            }
        }
    }

    private static Material CopiaDoCeu(string nome)
    {
        string caminho = "Assets/Materiais/" + nome + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(caminho) == null)
        {
            AssetDatabase.CopyAsset(CeuBase, caminho);
        }
        return AssetDatabase.LoadAssetAtPath<Material>(caminho);
    }

    // ------------------------------------------------------------------
    // Tocha (prefab)
    // ------------------------------------------------------------------

    /// <summary>
    /// Tocha do jogo: mantém o poste, a luz e a chama de partículas e troca a ponta pela tocha do pack.
    /// </summary>
    private static void TrocarTocha()
    {
        string alvo = "Assets/Prefabs/Tocha.prefab";
        GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(Masmorra + "Props/Torch.prefab");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(alvo) == null || modelo == null)
        {
            Debug.LogWarning("TrocarAssets: não achei a tocha ou o prefab Tocha");
            return;
        }

        GameObject raiz = PrefabUtility.LoadPrefabContents(alvo);
        foreach (string nome in new[] { "Suporte", "Brasa", "ModeloTocha" })
        {
            Transform velho = raiz.transform.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
        }

        GameObject filho = (GameObject)PrefabUtility.InstantiatePrefab(modelo, raiz.transform);
        filho.name = "ModeloTocha";
        CorrigirMateriais(filho);
        SemColisor(filho);
        // O jogo já tem luz e chama próprias; desliga as do pack para não duplicar
        foreach (Light l in filho.GetComponentsInChildren<Light>(true)) l.enabled = false;
        foreach (ParticleSystem ps in filho.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);

        Ajustar(filho.transform, 0f, 0.7f);
        filho.transform.position += new Vector3(0f, 1.7f, 0f);

        Transform chama = raiz.transform.Find("Chama");
        if (chama != null) chama.localPosition = new Vector3(0f, 2.4f, 0f);
        Transform luz = raiz.transform.Find("Luz");
        if (luz != null) luz.localPosition = new Vector3(0f, 2.6f, 0f);

        PrefabUtility.SaveAsPrefabAsset(raiz, alvo);
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    // ------------------------------------------------------------------
    // Ajuste com base em um ponto qualquer do mundo
    // ------------------------------------------------------------------

    /// <summary>
    /// Escala o modelo (uniforme) para a largura ou altura pedida e apoia a base no ponto informado.
    /// </summary>
    private static void AjustarEm(Transform modelo, Vector3 baseMundo, float largura, float altura)
    {
        if (!Limites(modelo, out Bounds b)) return;
        float fator = 1f;
        if (largura > 0f) fator = largura / Mathf.Max(b.size.x, b.size.z, 0.0001f);
        else if (altura > 0f) fator = altura / Mathf.Max(b.size.y, 0.0001f);
        modelo.localScale *= fator;

        Limites(modelo, out b);
        modelo.position += baseMundo - new Vector3(b.center.x, b.min.y, b.center.z);
    }

    // ------------------------------------------------------------------
    // Forja
    // ------------------------------------------------------------------

    /// <summary>
    /// Ilha da casa: forja, bigorna, mesa com ferramentas e o resto do ferreiro (pack Medieval Blacksmith Props).
    /// </summary>
    private static void ReformarForja(GameObject forja)
    {
        if (forja == null) return;

        Transform velho = forja.transform.Find("AdornosDaForja");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);
        Transform bigorna = forja.transform.Find("Bigorna");
        if (bigorna != null) Object.DestroyImmediate(bigorna.gameObject);

        Transform m = new GameObject("AdornosDaForja").transform;
        m.SetParent(forja.transform, false);
        const float y = 5f; // topo da ilha

        GameObject suporte = Mobilia(m, Ronin + "AnvilStand.prefab", new Vector3(70.5f, y, 6.4f), 0f, 0f, 0.6f);
        float topoSuporte = suporte != null && Limites(suporte.transform, out Bounds bs) ? bs.max.y : y + 0.6f;
        Mobilia(m, Ronin + "Anvil1.prefab", new Vector3(70.5f, topoSuporte, 6.4f), 0f, 0.8f, 0f);
        Mobilia(m, Ronin + "Grindstone.prefab", new Vector3(72.6f, y, 6.3f), 20f, 0f, 0.9f);

        // Sem a mesa no meio: o espaço da frente da casa fica livre para o jogador passar
        Mobilia(m, Ronin + "Hammer1.prefab", new Vector3(71.8f, y, 6.1f), 80f, 0.45f, 0f);
        Mobilia(m, Ronin + "Tongs1.prefab", new Vector3(69.4f, y, 6.0f), -20f, 0.5f, 0f);
        Mobilia(m, Ronin + "Barell.prefab", new Vector3(77.3f, y, 6.3f), 0f, 0f, 0.9f);
        Mobilia(m, Ronin + "SwordsRack.prefab", new Vector3(79.2f, y, 6.4f), 0f, 0f, 1.4f);
        Mobilia(m, Ronin + "Forge.prefab", new Vector3(79.4f, y, 10f), 270f, 2.4f, 0f);
        Mobilia(m, Ronin + "Bellow.prefab", new Vector3(79.4f, y, 12.3f), 270f, 1f, 0f);
    }

    // ------------------------------------------------------------------
    // Fonte da Nascente
    // ------------------------------------------------------------------

    /// <summary>
    /// Troca a fonte de cilindros pelo modelo do pack: a escura no vale morto e a clara quando
    /// o jogador restaura o vale.
    /// </summary>
    private static void ReformarFonte()
    {
        GameObject nasc = GameObject.Find("Nascente");
        GameObject escura = AssetDatabase.LoadAssetAtPath<GameObject>(FontePasta + "fountain_4_dark.prefab");
        GameObject clara = AssetDatabase.LoadAssetAtPath<GameObject>(FontePasta + "fountain_4_light.prefab");
        NascenteFinal final = Object.FindFirstObjectByType<NascenteFinal>();
        if (nasc == null || escura == null || clara == null || final == null) return;

        Transform n = nasc.transform;
        foreach (string nome in new[] { "Bacia", "Coluna", "Taca", "EspelhoDagua", "FonteMorta", "FonteViva" })
        {
            Transform velho = n.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
        }

        GameObject morta = (GameObject)PrefabUtility.InstantiatePrefab(escura, n);
        morta.name = "FonteMorta";
        GameObject viva = (GameObject)PrefabUtility.InstantiatePrefab(clara, n);
        viva.name = "FonteViva";

        float altura = 0f;
        foreach (GameObject g in new[] { morta, viva })
        {
            CorrigirMateriais(g);
            SemColisor(g);
            AjustarEm(g.transform, n.position, 3.6f, 0f);
            g.transform.rotation = Quaternion.identity;
        }
        if (Limites(morta.transform, out Bounds b))
        {
            altura = b.size.y;
            BoxCollider c = morta.AddComponent<BoxCollider>();
            // O tamanho do colisor é local: divide pela escala do modelo
            float escala = Mathf.Max(morta.transform.lossyScale.x, 0.0001f);
            c.center = morta.transform.InverseTransformPoint(b.center);
            c.size = new Vector3(b.size.x * 0.85f, b.size.y, b.size.z * 0.85f) / escala;
        }
        viva.SetActive(false);

        // A água e a explosão saem do alto da fonte
        Transform agua = n.Find("AguaDaNascente");
        if (agua != null) agua.localPosition = new Vector3(0f, Mathf.Max(altura, 2f), 0f);
        Transform explosao = n.Find("ExplosaoDeLuz");
        if (explosao != null) explosao.localPosition = new Vector3(0f, Mathf.Max(altura, 2f) * 0.5f, 0f);

        // Morta some e a viva aparece na restauração
        SerializedObject so = new SerializedObject(final);
        AdicionarAoArray(so.FindProperty("arvoresSecas"), morta);
        AdicionarAoArray(so.FindProperty("arvoresVerdes"), viva);
        so.ApplyModifiedProperties();
    }

    private static void AdicionarAoArray(SerializedProperty array, Object item)
    {
        List<Object> atuais = new List<Object>();
        for (int i = 0; i < array.arraySize; i++)
        {
            Object o = array.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null) atuais.Add(o);
        }
        atuais.Add(item);
        array.arraySize = atuais.Count;
        for (int i = 0; i < atuais.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = atuais[i];
    }

    /// <summary>
    /// A caixa empilhada em cima de outras duas ficava no ar porque os modelos novos são mais baixos
    /// que os cubos de antes: baixa cada caixa elevada até o topo das caixas embaixo dela.
    /// </summary>
    private static void AjustarCaixotes()
    {
        GameObject praca = GameObject.Find("1_PracaDaVila");
        if (praca == null) return;

        List<Transform> caixas = new List<Transform>();
        foreach (Transform c in praca.transform)
        {
            if (c.name == "Caixote") caixas.Add(c);
        }

        foreach (Transform alta in caixas)
        {
            if (alta.position.y < 0.05f) continue;
            float baseY = 0f;
            foreach (Transform baixa in caixas)
            {
                if (baixa == alta || baixa.position.y >= 0.05f) continue;
                Vector3 d = baixa.position - alta.position;
                d.y = 0f;
                if (d.magnitude < 1.4f && Limites(baixa, out Bounds b)) baseY = Mathf.Max(baseY, b.max.y);
            }
            alta.position = new Vector3(alta.position.x, baseY, alta.position.z);
        }
    }

    // ------------------------------------------------------------------
    // Enfeites da vila
    // ------------------------------------------------------------------

    private struct Enfeite
    {
        public string prefab;
        public float largura, altura;

        public Enfeite(string prefab, float largura, float altura)
        {
            this.prefab = prefab; this.largura = largura; this.altura = altura;
        }
    }

    /// <summary>
    /// Espalha bancos, cestos, sacos, vasos, baús, barris e outros objetos pela praça, longe do caminho
    /// e do que já existe lá.
    /// </summary>
    private static void EnfeitarVila()
    {
        GameObject praca = GameObject.Find("1_PracaDaVila");
        if (praca == null) return;
        Transform p = praca.transform;

        Transform velho = p.Find("AdornosDaVila");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);

        // Posições já ocupadas (com raio maior para casas e moinho)
        List<Vector3> ocupados = new List<Vector3>();
        List<float> raios = new List<float>();
        foreach (Transform c in p)
        {
            ocupados.Add(c.position);
            raios.Add(c.name.StartsWith("Casa") || c.name.StartsWith("Moinho") ? 4.8f : 1.8f);
        }

        string r = "Assets/RPGPP_LT/Prefabs/Props/";
        Enfeite[] lista =
        {
            new Enfeite(r + "Benches/rpgpp_lt_bench_wood_01.prefab", 1.8f, 0f),
            new Enfeite(r + "Benches/rpgpp_lt_bench_wood_02.prefab", 1.8f, 0f),
            new Enfeite(r + "Tables/rpgpp_lt_table_01.prefab", 1.5f, 0f),
            new Enfeite(r + "Chairs/rpgpp_lt_chair_01a.prefab", 0f, 0.9f),
            new Enfeite(r + "Containers/rpgpp_lt_basket_01.prefab", 0.6f, 0f),
            new Enfeite(r + "Containers/rpgpp_lt_basket_02.prefab", 0.6f, 0f),
            new Enfeite(r + "Containers/rpgpp_lt_sack_01.prefab", 0f, 0.7f),
            new Enfeite(r + "Containers/rpgpp_lt_sack_02_set.prefab", 1.0f, 0f),
            new Enfeite(r + "Containers/rpgpp_lt_vase_01.prefab", 0f, 0.7f),
            new Enfeite(r + "Containers/rpgpp_lt_vase_02.prefab", 0f, 0.6f),
            new Enfeite(r + "Containers/rpgpp_lt_vase_03.prefab", 0f, 0.6f),
            new Enfeite(r + "Containers/rpgpp_lt_crate_02.prefab", 0.9f, 0f),
            new Enfeite(r + "Containers/rpgpp_lt_crate_03.prefab", 0.9f, 0f),
            new Enfeite(r + "Containers/rpgpp_lt_barrel_02.prefab", 0f, 0.95f),
            new Enfeite(r + "Containers/rpgpp_lt_bucket_01.prefab", 0f, 0.45f),
            new Enfeite(r + "Containers/rpgpp_lt_trough_01.prefab", 1.6f, 0f),
            new Enfeite(r + "Wood/rpgpp_lt_log_wood_01.prefab", 1.6f, 0f),
            new Enfeite(r + "Wood/rpgpp_lt_log_wood_02a.prefab", 1.4f, 0f),
            new Enfeite(r + "Items/rpgpp_lt_ladder_01.prefab", 0f, 2.4f),
            new Enfeite(r + "Items/rpgpp_lt_broom_01.prefab", 0f, 1.3f),
            new Enfeite(r + "Items/rpgpp_lt_rake_01.prefab", 0f, 1.3f),
            new Enfeite(r + "Hangers/rpgpp_lt_hanger_wood_01.prefab", 1.4f, 0f),
            new Enfeite(r + "Misc/rpgpp_lt_stones_01.prefab", 1.2f, 0f),
            new Enfeite(Masmorra + "Props/Chest.prefab", 0.9f, 0f),
            new Enfeite(Masmorra + "Props/Barrel.prefab", 0f, 0.9f),
            new Enfeite(Ronin + "Barell.prefab", 0f, 0.9f),
            new Enfeite(Ronin + "SwordsRack.prefab", 0f, 1.4f),
            new Enfeite("Assets/RPGPP_LT/Prefabs/Exterior/Wood_path/rpgpp_lt_shed_wood_01.prefab", 3.2f, 0f),
        };

        Transform grupo = new GameObject("AdornosDaVila").transform;
        grupo.SetParent(p, false);

        Random.State estado = Random.state;
        Random.InitState(31);
        int i = 0, tentativas = 0;
        while (i < lista.Length && tentativas++ < 600)
        {
            Vector3 pos = new Vector3(Random.Range(-18f, 8f), 0f, Random.Range(-13.5f, 13.5f));
            if (Mathf.Abs(pos.z) < 3.6f) continue; // caminho do jogador
            bool livre = true;
            float raioNovo = lista[i].largura > 2.5f ? 2.6f : 1.3f;
            for (int k = 0; k < ocupados.Count; k++)
            {
                Vector3 d = ocupados[k] - pos;
                d.y = 0f;
                if (d.magnitude < raios[k] + raioNovo) { livre = false; break; }
            }
            if (!livre) continue;

            GameObject o = Mobilia(grupo, lista[i].prefab, pos, Random.Range(0f, 360f), lista[i].largura, lista[i].altura);
            if (o != null)
            {
                ocupados.Add(pos);
                raios.Add(raioNovo);
            }
            i++;
        }
        Random.state = estado;
    }

    // ------------------------------------------------------------------
    // Casas, ruínas e rios no mundo em volta
    // ------------------------------------------------------------------

    /// <summary>
    /// Vilas no horizonte: cada uma tem uma praça com poço, bancos e tochas, casas em círculo viradas
    /// para o centro (com barris, caixas e cestos na porta e lenha atrás), carroça, estandartes,
    /// uma estrada de terra e uma cerca em volta com a entrada virada para o nível.
    /// </summary>
    private static void CasasNoCenario(Transform mundo)
    {
        Transform velho = mundo.Find("AldeiasDistantes");
        if (velho != null) Object.DestroyImmediate(velho.gameObject);
        Transform grupo = new GameObject("AldeiasDistantes").transform;
        grupo.SetParent(mundo, false);

        // Candidatos para o centro de cada vila; os que caem perto de um rio são pulados
        Vector3[] candidatos =
        {
            new Vector3(15f, yChao, -105f), new Vector3(-110f, yChao, 30f), new Vector3(115f, yChao, -125f),
            new Vector3(-65f, yChao, -120f), new Vector3(55f, yChao, 130f), new Vector3(-130f, yChao, -65f),
            new Vector3(215f, yChao, 50f), new Vector3(180f, yChao, 150f),
        };

        Random.State estado = Random.state;
        Random.InitState(47);
        List<Vector3> vilas = new List<Vector3>();
        foreach (Vector3 centro in candidatos)
        {
            if (vilas.Count >= 6) break;
            if (PertoDeRio(centro, 34f)) continue;
            CriarAldeia(grupo, "Aldeia_" + vilas.Count, centro, vilas.Count);
            vilas.Add(centro);
        }
        Random.state = estado;

        // Limpa as árvores, arbustos e ruínas que o mundo espalhou dentro das vilas
        for (int i = mundo.childCount - 1; i >= 0; i--)
        {
            Transform c = mundo.GetChild(i);
            if (c.name != "ArvoreSeca" && c.name != "ArbustoSeco" && c.name != "Column" && !c.name.StartsWith("Edgerubble")) continue;
            foreach (Vector3 centro in vilas)
            {
                Vector3 d = c.position - centro;
                d.y = 0f;
                if (d.magnitude < 30f) { Object.DestroyImmediate(c.gameObject); break; }
            }
        }
    }

    /// <summary>
    /// Monta uma vila inteira combinando os modelos. A entrada da cerca e a estrada olham para o
    /// centro do nível, para a vila ser vista de quem joga.
    /// </summary>
    private static void CriarAldeia(Transform pai, string nome, Vector3 centro, int variante)
    {
        const string rp = "Assets/RPGPP_LT/Prefabs/";
        const float raioCasas = 15f, raioCerca = 24f;
        Transform t = new GameObject(nome).transform;
        t.SetParent(pai, false);

        // Direção da entrada: de frente para o meio do nível
        float entrada = Mathf.Atan2(0f - centro.z, 60f - centro.x);

        Material praca = MaterialLiso("PracaDaAldeia", new Color(0.46f, 0.42f, 0.36f));
        Material estrada = MaterialLiso("EstradaDaAldeia", new Color(0.54f, 0.48f, 0.39f));

        // Piso da praça e estrada (finos, só visuais)
        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        piso.name = "Praca";
        piso.transform.SetParent(t, false);
        piso.transform.position = new Vector3(centro.x, yChao + 0.03f, centro.z);
        piso.transform.localScale = new Vector3(20f, 0.03f, 20f);
        Object.DestroyImmediate(piso.GetComponent<Collider>());
        piso.GetComponent<MeshRenderer>().sharedMaterial = praca;

        Vector3 dirEntrada = new Vector3(Mathf.Cos(entrada), 0f, Mathf.Sin(entrada));
        GameObject rua = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rua.name = "Estrada";
        rua.transform.SetParent(t, false);
        rua.transform.position = new Vector3(centro.x, yChao + 0.04f, centro.z) + dirEntrada * 20f;
        rua.transform.rotation = Quaternion.LookRotation(dirEntrada);
        rua.transform.localScale = new Vector3(3f, 0.03f, 40f);
        Object.DestroyImmediate(rua.GetComponent<Collider>());
        rua.GetComponent<MeshRenderer>().sharedMaterial = estrada;

        // Centro da praça: poço, bancos virados para ele, estandartes e tochas
        Mobilia(t, rp + "Props/Misc/rpgpp_lt_well_01.prefab", centro, 0f, 2.4f, 0f);
        string[] bancos =
        {
            rp + "Props/Benches/rpgpp_lt_bench_wood_01.prefab", rp + "Props/Benches/rpgpp_lt_bench_wood_02.prefab",
            rp + "Props/Benches/rpgpp_lt_bench_wood_03.prefab",
        };
        for (int i = 0; i < 3; i++)
        {
            float a = entrada + Mathf.PI * (0.5f + i * 0.5f) + 0.4f;
            Vector3 pos = centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 5.2f;
            Mobilia(t, bancos[i], pos, FrenteParaCentro(pos, centro), 1.8f, 0f);
        }
        for (int i = 0; i < 2; i++)
        {
            float a = entrada + Mathf.PI * (i == 0 ? 0.75f : 1.25f);
            Vector3 pos = centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7.5f;
            Mobilia(t, rp + "Props/Banners/rpgpp_lt_banner_01a.prefab", pos, FrenteParaCentro(pos, centro), 0f, 3.2f);
        }
        GameObject tocha = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tocha.prefab");
        if (tocha != null)
        {
            for (int i = 0; i < 2; i++)
            {
                float a = entrada + (i == 0 ? 0.35f : -0.35f);
                Vector3 pos = centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 9f;
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(tocha, t);
                g.transform.position = pos;
                SemColisor(g);
            }
        }

        // Casas em círculo, viradas para o centro, cada uma com objetos na porta e atrás
        string[] casas =
        {
            rp + "Buildings/Bld_closed/rpgpp_lt_building_01.prefab", rp + "Buildings/Bld_closed/rpgpp_lt_building_02.prefab",
            rp + "Buildings/Bld_closed/rpgpp_lt_building_03.prefab", rp + "Buildings/Bld_closed/rpgpp_lt_building_04.prefab",
            rp + "Buildings/Bld_closed/rpgpp_lt_building_05.prefab",
        };
        string[] naPorta =
        {
            rp + "Props/Containers/rpgpp_lt_barrel_01.prefab", rp + "Props/Containers/rpgpp_lt_crate_01.prefab",
            rp + "Props/Containers/rpgpp_lt_basket_01.prefab", rp + "Props/Containers/rpgpp_lt_sack_02_set.prefab",
            rp + "Props/Containers/rpgpp_lt_barrel_02.prefab", rp + "Props/Containers/rpgpp_lt_crate_03.prefab",
        };
        string[] atras =
        {
            rp + "Props/Wood/rpgpp_lt_log_wood_01.prefab", rp + "Exterior/Wood_path/rpgpp_lt_shed_wood_01.prefab",
            rp + "Props/Items/rpgpp_lt_ladder_01.prefab", rp + "Exterior/Wood_path/rpgpp_lt_shed_wood_02.prefab",
            rp + "Props/Hangers/rpgpp_lt_hanger_wood_01.prefab", rp + "Props/Wood/rpgpp_lt_log_wood_02a.prefab",
        };
        const int quantas = 6;
        for (int i = 0; i < quantas; i++)
        {
            float a = entrada + (i + 0.5f) * Mathf.PI * 2f / quantas + Random.Range(-0.08f, 0.08f);
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 pos = centro + dir * (raioCasas + Random.Range(-1.2f, 1.2f));
            float giro = FrenteParaCentro(pos, centro) + Random.Range(-6f, 6f);
            Mobilia(t, casas[(i + variante) % casas.Length], pos, giro, Random.Range(6.5f, 7.8f), 0f);

            // Na porta: um objeto de cada lado do caminho até o centro
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            Vector3 frente = centro + dir * (raioCasas - 5.2f);
            Mobilia(t, naPorta[(i + variante) % naPorta.Length], frente + lado * 1.6f, Random.Range(0f, 360f), 0.9f, 0f);
            if (i % 2 == 0)
            {
                Mobilia(t, naPorta[(i + variante + 3) % naPorta.Length], frente - lado * 1.7f, Random.Range(0f, 360f), 0.9f, 0f);
            }

            // Atrás da casa
            Mobilia(t, atras[(i + variante) % atras.Length], centro + dir * (raioCasas + 6.5f) + lado * 1.5f,
                    giro, atras[(i + variante) % atras.Length].Contains("shed") ? 3.4f : 1.5f, 0f);
        }

        // Carroça perto da estrada e caixas empilhadas ao lado
        Vector3 pc = centro + new Vector3(Mathf.Cos(entrada + 0.5f), 0f, Mathf.Sin(entrada + 0.5f)) * 11f;
        Mobilia(t, rp + "Props/Misc/rpgpp_lt_wagon_01.prefab", pc, FrenteParaCentro(pc, centro) + 90f, 3f, 0f);

        // Cerca em volta com abertura na entrada (as peças de 3 m acompanham o círculo)
        GameObject cerca = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cerca.prefab");
        if (cerca != null)
        {
            int pecas = Mathf.RoundToInt(Mathf.PI * 2f * raioCerca / 3f);
            for (int i = 0; i < pecas; i++)
            {
                float a = i * Mathf.PI * 2f / pecas;
                float diferenca = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, entrada * Mathf.Rad2Deg));
                if (diferenca < 14f) continue; // entrada da vila
                Vector3 pos = centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * raioCerca;
                GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(cerca, t);
                g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg, 0f));
                SemColisor(g);
            }
        }
    }

    /// <summary>Giro em Y para um objeto na posição pos ficar de frente (+Z) para o centro.</summary>
    private static float FrenteParaCentro(Vector3 pos, Vector3 centro)
    {
        Vector3 d = centro - pos;
        return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }

    /// <summary>Verdadeiro se o ponto está sobre um rio ou perto demais dele.</summary>
    private static bool PertoDeRio(Vector3 pos, float folga)
    {
        foreach (Vector3 centro in centrosDosRios)
        {
            Vector3 d = centro - pos;
            d.y = 0f;
            if (d.magnitude < 7.5f + folga) return true;
        }
        return false;
    }

    private static readonly List<Vector3> centrosDosRios = new List<Vector3>();

    /// <summary>
    /// Dois rios sinuosos no chão do mundo, um de leste a oeste ao sul do nível e outro de norte a sul
    /// ao leste. Árvores e arbustos que caiam em cima deles são removidos.
    /// </summary>
    private static void CriarRios(Transform mundo)
    {
        foreach (string nome in new[] { "Rio_1", "Rio_2" })
        {
            Transform velho = mundo.Find(nome);
            if (velho != null) Object.DestroyImmediate(velho.gameObject);
        }
        centrosDosRios.Clear();

        Material agua = CriarOuPegarMaterial("AguaRio");
        agua.SetColor("_BaseColor", new Color(0.20f, 0.45f, 0.65f));
        agua.SetFloat("_Smoothness", 0.9f);
        agua.SetFloat("_Metallic", 0.1f);
        EditorUtility.SetDirty(agua);

        CriarRio(mundo, "Rio_1", new Vector3(-220f, 0f, 75f), 90f, 48, 14f, 5f, 0.0f, agua);
        CriarRio(mundo, "Rio_2", new Vector3(160f, 0f, -230f), 0f, 44, 12f, 5f, 1.7f, agua);

        // Tira o que ficou dentro dos rios
        for (int i = mundo.childCount - 1; i >= 0; i--)
        {
            Transform c = mundo.GetChild(i);
            if (c.name != "ArvoreSeca" && c.name != "ArbustoSeco" && c.name != "Column" && !c.name.StartsWith("Edgerubble")) continue;
            foreach (Vector3 centro in centrosDosRios)
            {
                Vector3 d = c.position - centro;
                d.y = 0f;
                if (d.magnitude < 11f) { Object.DestroyImmediate(c.gameObject); break; }
            }
        }
    }

    private static void CriarRio(Transform mundo, string nome, Vector3 inicio, float rumoInicial, int segmentos,
                                 float largura, float meandro, float fase, Material agua)
    {
        Transform raiz = new GameObject(nome).transform;
        raiz.SetParent(mundo, false);
        const float comprimento = 12f;
        Vector3 pos = new Vector3(inicio.x, yChao + 0.06f, inicio.z);
        float rumo = rumoInicial;

        for (int i = 0; i < segmentos; i++)
        {
            rumo += Mathf.Sin(i * 0.4f + fase) * meandro;
            Vector3 dir = Quaternion.Euler(0f, rumo, 0f) * Vector3.forward;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = nome + "_" + i;
            seg.transform.SetParent(raiz, false);
            seg.transform.position = pos + dir * (comprimento * 0.5f);
            seg.transform.rotation = Quaternion.LookRotation(dir);
            seg.transform.localScale = new Vector3(largura, 0.12f, comprimento + 1.5f);
            Object.DestroyImmediate(seg.GetComponent<Collider>());
            seg.GetComponent<MeshRenderer>().sharedMaterial = agua;
            centrosDosRios.Add(seg.transform.position);

            pos += dir * comprimento;
        }
    }
}
