using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.WebGL;
using UnityEngine;

public static class WebBuildOptimizer
{
    private const string CatalogPath = "Assets/Resources/CookVisualCatalog.asset";
    private const string PersistentScenePath = "Assets/Scenes/Persistent.unity";

    public static void ApplyStageTwo()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            OptimizeTextMeshPro();
            ConfigureWebBuildForSize();
            DisableUnusedCloudServices();
            RemoveUnusedTmpDefaults();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("WebBuildOptimizer: stage two completed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    private static void OptimizeTextMeshPro()
    {
        const string fontPath = "Assets/TextMesh Pro/Fonts/font SDF.asset";
        const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string dialogPrefabPath = "Assets/Scripts/DialogSystem/Prefab/DialogBox.prefab";

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (font == null) throw new InvalidOperationException("Missing TMP font: " + fontPath);

        string corpus = BuildTextCorpus();
        if (!font.TryAddCharacters(corpus, out string missing, true))
        {
            string missingDisplay = string.IsNullOrEmpty(missing) ? "unknown" : missing;
            throw new InvalidOperationException("The project font cannot render required characters: " + missingDisplay);
        }

        var fontSerialized = new SerializedObject(font);
        fontSerialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
        fontSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(font);

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(settingsPath);
        if (settings == null) throw new InvalidOperationException("Missing TMP settings: " + settingsPath);
        var settingsSerialized = new SerializedObject(settings);
        settingsSerialized.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        settingsSerialized.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
        settingsSerialized.FindProperty("m_enableEmojiSupport").boolValue = false;
        settingsSerialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);

        var dialogRoot = AssetDatabase.LoadAssetAtPath<GameObject>(dialogPrefabPath);
        if (dialogRoot == null) throw new InvalidOperationException("Missing dialog prefab: " + dialogPrefabPath);
        foreach (var text in dialogRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            var serialized = new SerializedObject(text);
            var spriteAsset = serialized.FindProperty("m_spriteAsset");
            if (spriteAsset != null) spriteAsset.objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(text);
        }
        PrefabUtility.SavePrefabAsset(dialogRoot);

        Debug.Log("WebBuildOptimizer: validated " + corpus.Length + " text characters; TMP dynamic data will be cleared for builds.");
    }

    private static string BuildTextCorpus()
    {
        var builder = new StringBuilder();
        builder.Append("开始营业拖动食材到锅里今日订单第天上下一个顾客暂停继续退出游戏设置说明关闭");
        builder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
        builder.Append("，。！？：；、（）《》【】—…×+-=<>/# ");

        string[] dialogGuids = AssetDatabase.FindAssets("t:RoleDialogSO");
        foreach (string guid in dialogGuids)
        {
            var dialog = AssetDatabase.LoadAssetAtPath<RoleDialogSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (dialog == null) continue;
            builder.Append(dialog.Npc);
            if (dialog.defaultDialog != null)
            {
                foreach (var data in dialog.defaultDialog) builder.Append(data.Content);
            }
            if (dialog.successDialog != null) builder.Append(dialog.successDialog.Content);
            if (dialog.failDialog != null) builder.Append(dialog.failDialog.Content);
        }

        string[] roleGuids = AssetDatabase.FindAssets("t:Role");
        foreach (string guid in roleGuids)
        {
            var role = AssetDatabase.LoadAssetAtPath<Role>(AssetDatabase.GUIDToAssetPath(guid));
            if (role != null) builder.Append(role.RoleName);
        }

        // Remove TMP markup. Markup characters themselves are already in the fixed UI corpus above.
        string source = builder.ToString();
        var unique = new HashSet<char>();
        bool insideTag = false;
        foreach (char c in source)
        {
            if (c == '<') { insideTag = true; continue; }
            if (c == '>') { insideTag = false; continue; }
            if (!insideTag && !char.IsControl(c)) unique.Add(c);
        }

        var sorted = new List<char>(unique);
        sorted.Sort();
        return new string(sorted.ToArray());
    }

    private static void ConfigureWebBuildForSize()
    {
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Low);
        UserBuildSettings.codeOptimization = WasmCodeOptimization.DiskSizeLTO;
        PlayerSettings.WebGL.wasm2023 = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        Debug.Log("WebBuildOptimizer: WebGL configured for OptimizeSize + DiskSizeLTO + Low stripping + Wasm 2023.");
    }

    private static void DisableUnusedCloudServices()
    {
        const string connectPath = "ProjectSettings/UnityConnectSettings.asset";
        var objects = AssetDatabase.LoadAllAssetsAtPath(connectPath);
        if (objects == null || objects.Length == 0)
        {
            Debug.LogWarning("WebBuildOptimizer: UnityConnectSettings could not be loaded through AssetDatabase.");
            return;
        }

        var serialized = new SerializedObject(objects[0]);
        SetBoolIfPresent(serialized, "m_Enabled", false);
        SetBoolIfPresent(serialized, "UnityAnalyticsSettings.m_Enabled", false);
        SetBoolIfPresent(serialized, "UnityAnalyticsSettings.m_InitializeOnStartup", false);
        SetBoolIfPresent(serialized, "PerformanceReportingSettings.m_Enabled", false);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(objects[0]);
    }

    private static void SetBoolIfPresent(SerializedObject serialized, string path, bool value)
    {
        var property = serialized.FindProperty(path);
        if (property != null) property.boolValue = value;
    }

    private static void RemoveUnusedTmpDefaults()
    {
        DeleteIfPresent("Assets/TextMesh Pro/Resources/Fonts & Materials");
        DeleteIfPresent("Assets/TextMesh Pro/Resources/Sprite Assets");
        DeleteIfPresent("Assets/TextMesh Pro/Sprites");
    }

    private static void DeleteIfPresent(string path)
    {
        if (!AssetDatabase.IsValidFolder(path) && AssetDatabase.LoadMainAssetAtPath(path) == null) return;
        if (!AssetDatabase.DeleteAsset(path))
            throw new InvalidOperationException("Could not delete unused asset: " + path);
    }

    public static void ApplyStageOne()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CreateCookCatalog();
            ConfigureAudioImporters();
            UpdatePersistentAudioReferences();
            RemoveDuplicateCookResources();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("WebBuildOptimizer: stage one completed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            throw;
        }
    }

    private static void CreateCookCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CookVisualCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CookVisualCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.crepe = Sprite("Assets/Arts/煎饼果子_0000s_0009_面饼-单个.png");
        catalog.pancakeDone = Sprite("Assets/Arts/煎饼果子_0000s_0002_煎饼果子-单个.png");
        catalog.resultGood = Sprite("Assets/Arts/好饼饼.png");
        catalog.resultBad = Sprite("Assets/Arts/坏饼饼、.png");
        catalog.ladle = Sprite("Assets/Arts/面粉勺子.png");
        catalog.brushSauce = Sprite("Assets/Arts/甜面酱刷子.png");
        catalog.brushPepper = Sprite("Assets/Arts/辣椒刷子.png");
        catalog.eggSplat = Sprite("Assets/Arts/煎饼果子_0000s_0004_鸡蛋-单个.png");

        catalog.iconEgg = catalog.eggSplat;
        catalog.iconPepper = Sprite("Assets/Arts/辣椒酱.png");
        catalog.iconSauce = Sprite("Assets/Arts/甜面酱.png");
        catalog.iconSausage = Sprite("Assets/Arts/煎饼果子_0000s_0005_火腿肠-单个.png");
        catalog.iconSesame = Sprite("Assets/Arts/煎饼果子_0000s_0006_芝麻-单个.png");
        catalog.iconCoriander = Sprite("Assets/Arts/煎饼果子_0000s_0000_香菜单个.png");
        catalog.iconOnion = Sprite("Assets/Arts/香葱单个.png");
        catalog.iconLettuce = Sprite("Assets/Arts/煎饼果子_0000s_0001_生菜-单个.png");
        catalog.iconFlour = Sprite("Assets/Arts/煎饼果子_0011_面糊.png");
        catalog.iconMeat = Sprite("Assets/Arts/里脊肉 单个.png");
        catalog.iconOil = Sprite("Assets/Arts/煎饼果子_0008_油瓶.png");
        catalog.iconCrisp = Sprite("Assets/Arts/煎饼果子_0009_果子.png");

        EditorUtility.SetDirty(catalog);
    }

    private static void ConfigureAudioImporters()
    {
        ConfigureAudio(
            "Assets/Resources/Bgm/Main BGM.mp3",
            AudioClipLoadType.Streaming,
            0.65f,
            false,
            false,
            true);

        ConfigureAudio(
            "Assets/Resources/Sound Effect/Fire.wav",
            AudioClipLoadType.CompressedInMemory,
            0.5f,
            false,
            true,
            false);

        string[] safeMonoClips =
        {
            "Assets/Resources/Sound Effect/Turn Menu.wav",
            "Assets/Resources/Sound Effect/Egg Break.wav",
            "Assets/Resources/Sound Effect/Coin.wav",
            "Assets/Resources/People/First.wav",
            "Assets/Resources/People/Fifth.wav",
        };

        foreach (string path in safeMonoClips)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureAudio(
        string path,
        AudioClipLoadType loadType,
        float quality,
        bool forceMono,
        bool preload,
        bool backgroundLoad)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("Missing audio importer: " + path);

        var settings = importer.defaultSampleSettings;
        settings.loadType = loadType;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = quality;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        settings.preloadAudioData = preload;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = forceMono;
        importer.loadInBackground = backgroundLoad;
        importer.SaveAndReimport();
    }

    private static void UpdatePersistentAudioReferences()
    {
        var scene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
        var audioManagers = UnityEngine.Object.FindObjectsByType<AudioManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (audioManagers.Length != 1)
        {
            throw new InvalidOperationException("Expected exactly one AudioManager in Persistent scene.");
        }

        var serialized = new SerializedObject(audioManagers[0]);
        serialized.FindProperty("bgmClip").objectReferenceValue =
            Audio("Assets/Resources/Bgm/Main BGM.mp3");

        var voices = serialized.FindProperty("npcVoiceClips");
        voices.arraySize = 5;
        string[] voicePaths =
        {
            "Assets/Resources/People/Fifth.wav",
            "Assets/Resources/People/First.wav",
            "Assets/Resources/People/Fourth.wav",
            "Assets/Resources/People/Second.wav",
            "Assets/Resources/People/Third.wav",
        };
        for (int i = 0; i < voicePaths.Length; i++)
        {
            voices.GetArrayElementAtIndex(i).objectReferenceValue = Audio(voicePaths[i]);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(audioManagers[0]);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void RemoveDuplicateCookResources()
    {
        if (AssetDatabase.IsValidFolder("Assets/Resources/Cook") &&
            !AssetDatabase.DeleteAsset("Assets/Resources/Cook"))
        {
            throw new InvalidOperationException("Could not delete duplicate Resources/Cook folder.");
        }
    }

    private static Sprite Sprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing sprite: " + path);
        return sprite;
    }

    private static AudioClip Audio(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) throw new InvalidOperationException("Missing audio: " + path);
        return clip;
    }
}
