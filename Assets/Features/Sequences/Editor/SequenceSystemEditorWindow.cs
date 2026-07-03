using System;
using System.Collections.Generic;
using DioramaEnigma.Dioramas;
using Extensions.EditorTools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DioramaEnigma.Sequences.Editor
{
    /// <summary>
    /// Навигатор последовательностей: все <see cref="Sequence"/> проекта, их шаги и префабы-носители
    /// </summary>
    public sealed class SequenceSystemEditorWindow : EditorWindow
    {
        private const string WINDOW_NAME = "Sequences In Project";
        private const float SQUARE = EditorToolsConstraints.BASE_ELEMENT_HEIGHT;

        private sealed class SequenceInfo
        {
            public Sequence Asset;
            public string Guid;
            public DioramaDefinition Definition;
        }

        /// <summary> Группа последовательностей одного блока (или «без блока», если Block == null) </summary>
        private sealed class BlockGroup
        {
            public DioramaBlock Block;
            public string Key;
            public readonly List<SequenceInfo> Sequences = new();
        }

        private const string NO_BLOCK_KEY = "";

        private static readonly Color COLOR_OPEN = new(1f, 0.52f, 0.05f);

        private readonly List<SequenceInfo> sequences = new();
        private readonly List<BlockGroup> blockGroups = new();
        private readonly HashSet<string> expanded = new();
        private readonly HashSet<string> expandedBlocks = new();

        private readonly HashSet<int> inspectorExpanded = new();
        private readonly Dictionary<int, UnityEditor.Editor> embeddedEditors = new();

        private Vector2 scroll;

        [NonSerialized]
        private GUIStyle styleMainButton;

        [NonSerialized]
        private GUIStyle styleGroupHeader;

        [NonSerialized]
        private GUIStyle styleSmall;

        [MenuItem("Diorama Enigma/" + WINDOW_NAME, priority = 100)]
        public static void Open() => GetWindow<SequenceSystemEditorWindow>(WINDOW_NAME).minSize = new Vector2(420f, 320f);

        /// <summary> Открыть окно и развернуть конкретную последовательность </summary>
        public static void Open(Sequence sequence, int stepIndex = -1)
        {
            var window = GetWindow<SequenceSystemEditorWindow>(WINDOW_NAME);
            window.minSize = new Vector2(420f, 320f);
            window.Refresh();

            if (sequence == null) return;

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sequence));
            window.expanded.Add(guid);

            if (stepIndex >= 0) EditorGUIUtility.PingObject(sequence);
            window.Repaint();
        }

        private void OnEnable() => Refresh();

        private void OnDisable() => ReleaseEmbeddedEditors();

        #region GUI

        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();

            if (sequences.Count == 0)
            {
                EditorGUILayout.Space(EditorToolsConstraints.SPACE_BLOCK_SIZE);
                EditorGUILayout.LabelField("  Sequence в проекте не найдены", styleSmall);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var group in blockGroups)
                DrawBlock(group);
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(SQUARE));

            EditorGUILayout.LabelField($"Последовательностей: {sequences.Count}");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Объекты сцены ↗", EditorStyles.toolbarButton, GUILayout.Width(120)))
                SequenceSceneObjectsWindow.Open();

            if (GUILayout.Button("↺ Обновить", EditorStyles.toolbarButton, GUILayout.Width(90)))
                Refresh();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary> Блок-уровень: иконка-папка сворачивает детей, плашка открывает ассет блока </summary>
        private void DrawBlock(BlockGroup group)
        {
            bool isExpanded = expandedBlocks.Contains(group.Key);
            bool hasAsset = group.Block != null;
            int inspectorId = hasAsset ? group.Block.GetInstanceID() : 0;
            bool inspectorOpen = hasAsset && inspectorExpanded.Contains(inspectorId);

            EditorGUILayout.BeginHorizontal();

            var folderIcon = EditorGUIUtility.IconContent(isExpanded ? "FolderOpened Icon" : "Folder Icon");
            folderIcon.tooltip = isExpanded ? "Свернуть блок" : "Развернуть блок";
            if (GUILayout.Button(folderIcon, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                ToggleExpandedBlock(group.Key);

            string title = hasAsset ? BuildBlockLabel(group.Block) : "Без блока";
            string indicator = (hasAsset ? inspectorOpen : isExpanded) ? "▼" : "▶";

            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_PURPLE))
            {
                if (GUILayout.Button($"  {indicator}  {title}   ({group.Sequences.Count})", styleMainButton, GUILayout.Height(SQUARE)))
                {
                    if (hasAsset) ToggleInspector(inspectorId);
                    else ToggleExpandedBlock(group.Key);
                }
            }

            if (hasAsset)
            {
                DrawOpenButton(group.Block);
                DrawPingButton(group.Block);
            }

            EditorGUILayout.EndHorizontal();

            if (inspectorOpen) DrawInlineInspector(group.Block, 1);

            if (!isExpanded) return;

            EditorGUI.indentLevel++;
            foreach (var info in group.Sequences)
                DrawSequence(info);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        private void DrawSequence(SequenceInfo info)
        {
            bool isExpanded = expanded.Contains(info.Guid);
            int id = info.Asset.GetInstanceID();
            bool inspectorOpen = inspectorExpanded.Contains(id);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 12f);

            var folderIcon = EditorGUIUtility.IconContent(isExpanded ? "FolderOpened Icon" : "Folder Icon");
            folderIcon.tooltip = isExpanded ? "Свернуть шаги" : "Развернуть шаги";
            if (GUILayout.Button(folderIcon, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
            {
                if (isExpanded) expanded.Remove(info.Guid);
                else expanded.Add(info.Guid);
            }

            string label = info.Asset.name;
            string indicator = inspectorOpen ? "▼" : "▶";

            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_ACCENT))
            {
                if (GUILayout.Button($"  {indicator}  {label}   ({info.Asset.Steps.Count} шагов)", styleMainButton, GUILayout.Height(SQUARE)))
                    ToggleInspector(id);
            }

            DrawOpenButton(info.Asset);
            DrawPingButton(info.Asset);

            EditorGUILayout.EndHorizontal();

            if (inspectorOpen) DrawInlineInspector(info.Asset, 1);

            if (isExpanded) DrawSteps(info.Asset);
        }

        private void DrawSteps(Sequence sequence)
        {
            EditorGUI.indentLevel++;

            int currentGroup = -1;
            foreach (var entry in sequence.Steps)
            {
                if (entry == null) continue;

                if (entry.GroupIndex != currentGroup)
                {
                    currentGroup = entry.GroupIndex;
                    EditorGUILayout.LabelField($"   Группа {currentGroup}", styleGroupHeader);
                }

                DrawStepRow(entry.Step as ScriptableObject);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2);
        }

        private void DrawStepRow(ScriptableObject stepAsset)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 12f);

            if (stepAsset == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    GUILayout.Button("○ (шаг не назначен)", styleMainButton, GUILayout.Height(SQUARE));
                EditorGUILayout.EndHorizontal();
                return;
            }

            int id = stepAsset.GetInstanceID();
            bool inspectorOpen = inspectorExpanded.Contains(id);
            string indicator = inspectorOpen ? "▾" : "▸";
            string label = BuildStepLabel(stepAsset);

            if (GUILayout.Button($"  {indicator}{label}", styleMainButton, GUILayout.Height(SQUARE)))
                ToggleInspector(id);

            DrawOpenButton(stepAsset);
            DrawPingButton(stepAsset);

            EditorGUILayout.EndHorizontal();

            if (inspectorOpen) DrawInlineInspector(stepAsset, EditorGUI.indentLevel + 1);
        }

        private void DrawOpenButton(Object asset)
        {
            using (new GUIBackgroundColorScope(COLOR_OPEN))
            {
                var content = EditorGUIUtility.IconContent(EditorToolsConstraints.ICON_INSPECT);
                content.tooltip = "Открыть в инспекторе";
                if (GUILayout.Button(content, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                    OpenInInspector(asset);
            }
        }

        private void DrawPingButton(Object asset)
        {
            using (new GUIBackgroundColorScope(EditorToolsConstraints.COLOR_CYAN))
            {
                if (GUILayout.Button(EditorToolsConstraints.SYMBOL_PING, GUILayout.Width(SQUARE), GUILayout.Height(SQUARE)))
                    EditorGUIUtility.PingObject(asset);
            }
        }

        /// <summary> Выделить ассет и открыть вкладку инспектора </summary>
        private static void OpenInInspector(Object asset)
        {
            Selection.activeObject = asset;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        /// <summary> Inline-инспектор ассета прямо под его строкой </summary>
        private void DrawInlineInspector(Object asset, int indent)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent * 12f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var editor = GetEmbeddedEditor(asset);
                if (editor != null) editor.OnInspectorGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Build

        private void Refresh()
        {
            sequences.Clear();

            var definitionBySequence = BuildDefinitionMap();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(Sequence)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Sequence>(path);
                if (asset == null) continue;

                definitionBySequence.TryGetValue(asset, out var definition);
                sequences.Add(new SequenceInfo { Asset = asset, Guid = guid, Definition = definition });
            }

            sequences.Sort((a, b) => string.Compare(a.Asset.name, b.Asset.name, StringComparison.OrdinalIgnoreCase));

            RebuildBlockGroups();
        }

        /// <summary> Карта «последовательность → определение диорамы» по ассетам проекта </summary>
        private static Dictionary<Sequence, DioramaDefinition> BuildDefinitionMap()
        {
            var map = new Dictionary<Sequence, DioramaDefinition>();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(DioramaDefinition)}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<DioramaDefinition>(path);
                if (definition == null || definition.Sequence == null) continue;

                map[definition.Sequence] = definition;
            }

            return map;
        }

        /// <summary> Сгруппировать последовательности по блокам в порядке реестра; не учтённые — «без блока» </summary>
        private void RebuildBlockGroups()
        {
            blockGroups.Clear();

            var infoByDefinition = new Dictionary<DioramaDefinition, SequenceInfo>();
            foreach (var info in sequences)
                if (info.Definition != null)
                    infoByDefinition[info.Definition] = info;

            var placed = new HashSet<SequenceInfo>();
            var registry = LoadRegistry();

            if (registry != null)
            {
                int entryIndex = 0;
                foreach (var entry in registry.Blocks)
                {
                    if (entry == null) continue;

                    var group = new BlockGroup
                    {
                        Block = entry.Block,
                        Key = entry.Block != null ? entry.Block.Id : $"entry:{entryIndex}",
                    };
                    entryIndex++;

                    foreach (var dioramaEntry in entry.Dioramas)
                    {
                        var def = dioramaEntry?.Definition;
                        if (def != null && infoByDefinition.TryGetValue(def, out var info) && placed.Add(info))
                            group.Sequences.Add(info);
                    }

                    blockGroups.Add(group);
                }
            }

            BlockGroup noBlock = null;
            foreach (var info in sequences)
            {
                if (placed.Contains(info)) continue;

                noBlock ??= new BlockGroup { Block = null, Key = NO_BLOCK_KEY };
                noBlock.Sequences.Add(info);
            }

            if (noBlock != null) blockGroups.Add(noBlock);

            expandedBlocks.Clear();
            foreach (var group in blockGroups)
                expandedBlocks.Add(group.Key);
        }

        /// <summary> Первый найденный реестр диорам в проекте (или null) </summary>
        private static DioramaRegistry LoadRegistry()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(DioramaRegistry)}");
            if (guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<DioramaRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        #endregion

        #region Helpers

        private static string BuildStepLabel(ScriptableObject stepAsset)
        {
            string typeName = stepAsset.GetType().Name.Replace("Sequence", "");
            return $"  {typeName} · {stepAsset.name}";
        }

        private static string BuildBlockLabel(DioramaBlock block)
        {
            string title = string.IsNullOrEmpty(block.Title) ? block.name : block.Title;
            return $"Блок · {title}";
        }

        private void EnsureStyles()
        {
            styleMainButton ??= new GUIStyle("Button")
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE,
                padding = new RectOffset(8, 6, 2, 2),
            };

            styleGroupHeader ??= new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                fontSize = EditorToolsConstraints.BASE_FONT_SIZE - 1,
            };

            styleSmall ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        }

        private void ToggleInspector(int instanceId)
        {
            if (!inspectorExpanded.Remove(instanceId))
                inspectorExpanded.Add(instanceId);
        }

        private void ToggleExpandedBlock(string key)
        {
            if (!expandedBlocks.Remove(key))
                expandedBlocks.Add(key);
        }

        /// <summary> Получить (или создать) встроенный редактор ассета; кэшируется по InstanceID </summary>
        private UnityEditor.Editor GetEmbeddedEditor(Object asset)
        {
            int id = asset.GetInstanceID();

            if (embeddedEditors.TryGetValue(id, out var editor) && editor != null && editor.target == asset)
                return editor;

            if (editor != null) DestroyImmediate(editor);

            editor = UnityEditor.Editor.CreateEditor(asset);
            embeddedEditors[id] = editor;
            return editor;
        }

        private void ReleaseEmbeddedEditors()
        {
            foreach (var editor in embeddedEditors.Values)
                if (editor != null) DestroyImmediate(editor);

            embeddedEditors.Clear();
        }

        #endregion
    }
}
