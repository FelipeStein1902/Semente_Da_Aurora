using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Faz árvores e arbustos vivos crescerem do chão quando são ativados.
/// Cada árvore ou arbusto é achado pelo nome do objeto (começa com "ArvoreVerde" ou "ArbustoVerde"),
/// então dá para ativar de uma vez um grupo inteiro delas.
/// </summary>
public static class CrescimentoDeArvores
{
    private const float EscalaInicial = 0.001f;

    /// <summary>
    /// Liga o objeto e faz cada árvore ou arbusto dentro dele crescer do chão até o tamanho original.
    /// </summary>
    /// <param name="dono">Componente que executa a animação.</param>
    /// <param name="alvo">Árvore, arbusto ou grupo de árvores (pode estar desligado).</param>
    /// <param name="duracao">Tempo do crescimento, em segundos.</param>
    public static void Ativar(MonoBehaviour dono, GameObject alvo, float duracao = 1.5f)
    {
        if (alvo == null)
        {
            return;
        }

        List<Transform> plantas = new List<Transform>();
        Coletar(alvo.transform, plantas);

        List<Vector3> finais = new List<Vector3>();
        foreach (Transform planta in plantas)
        {
            finais.Add(planta.localScale);
            planta.localScale = Vector3.one * EscalaInicial;
        }

        alvo.SetActive(true);
        if (plantas.Count > 0)
        {
            dono.StartCoroutine(Crescer(plantas, finais, duracao));
        }
    }

    private static void Coletar(Transform t, List<Transform> plantas)
    {
        if (t.name.StartsWith("ArvoreVerde") || t.name.StartsWith("ArbustoVerde"))
        {
            plantas.Add(t);
            return;
        }
        foreach (Transform filho in t)
        {
            Coletar(filho, plantas);
        }
    }

    private static IEnumerator Crescer(List<Transform> plantas, List<Vector3> finais, float duracao)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            for (int i = 0; i < plantas.Count; i++)
            {
                if (plantas[i] != null)
                {
                    plantas[i].localScale = Vector3.Lerp(Vector3.one * EscalaInicial, finais[i], t);
                }
            }
            yield return null;
        }
        for (int i = 0; i < plantas.Count; i++)
        {
            if (plantas[i] != null)
            {
                plantas[i].localScale = finais[i];
            }
        }
    }
}
