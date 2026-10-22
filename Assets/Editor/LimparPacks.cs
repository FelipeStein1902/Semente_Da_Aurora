using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Apaga de um pack de assets tudo o que o projeto não usa (cenas, prefabs do jogo e os modelos que o
/// TrocarAssets ainda vai usar), para o repositório ficar leve. Menu: Semente da Aurora → Limpar pack da masmorra.
/// </summary>
public static class LimparPacks
{
    private const string PackMasmorra = "Assets/RPG Dungeon Pack Sample";

    // Modelos do pack que o TrocarAssets coloca no jogo (mesmo que ainda não estejam nas cenas)
    private static readonly string[] Usados =
    {
        PackMasmorra + "/Prefabs/Props/Torch.prefab",
        PackMasmorra + "/Prefabs/Props/Barrel.prefab",
        PackMasmorra + "/Prefabs/Props/Chest.prefab",
        PackMasmorra + "/Prefabs/Individuals/Edgerubble Top.prefab",
        PackMasmorra + "/Prefabs/Individuals/Edgerubble Bottom.prefab",
    };

    [MenuItem("Semente da Aurora/Limpar pack da masmorra (só o que é usado)")]
    public static void LimparMasmorra()
    {
        if (!AssetDatabase.IsValidFolder(PackMasmorra))
        {
            EditorUtility.DisplayDialog("Limpar pack", "A pasta " + PackMasmorra + " não existe.", "OK");
            return;
        }

        // Raízes: tudo o que o jogo usa (cenas, prefabs, materiais, animações) e os modelos acima
        List<string> raizes = new List<string>(Usados);
        foreach (string pasta in new[] { "Assets/Cenas", "Assets/Prefabs", "Assets/Resources", "Assets/Materiais", "Assets/Animacoes" })
        {
            if (!AssetDatabase.IsValidFolder(pasta)) continue;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { pasta }))
            {
                raizes.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // Dependências de tudo isso (modelos, materiais, texturas)
        HashSet<string> manter = new HashSet<string>(AssetDatabase.GetDependencies(raizes.ToArray(), true));

        // O que existe no pack e não está na lista
        List<string> apagar = new List<string>();
        long bytes = 0;
        foreach (string guid in AssetDatabase.FindAssets("", new[] { PackMasmorra }))
        {
            string caminho = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(caminho) || manter.Contains(caminho)) continue;
            apagar.Add(caminho);
            FileInfo f = new FileInfo(caminho);
            if (f.Exists) bytes += f.Length;
        }

        if (apagar.Count == 0)
        {
            EditorUtility.DisplayDialog("Limpar pack", "Não há nada a apagar: o pack já só tem o que é usado.", "OK");
            return;
        }

        string aviso = "Vão ser apagados " + apagar.Count + " arquivos (" + (bytes / (1024 * 1024)) + " MB) de " + PackMasmorra
                       + ", que o jogo não usa.\n\nO que é usado fica (tocha, barril, baú, entulho e o que as cenas referenciam). "
                       + "Dá para baixar o pack de novo pelo Package Manager. Continuar?";
        if (!EditorUtility.DisplayDialog("Limpar pack da masmorra", aviso, "Apagar", "Cancelar")) return;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string caminho in apagar) AssetDatabase.DeleteAsset(caminho);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();
        Debug.Log("LimparPacks: " + apagar.Count + " arquivos apagados do pack da masmorra (" + (bytes / (1024 * 1024)) + " MB).");
    }
}
