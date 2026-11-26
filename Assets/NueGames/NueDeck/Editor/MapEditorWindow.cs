using System.Collections.Generic;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.Managers;
using UnityEditor;
using UnityEngine;

namespace NueGames.NueDeck.Editor
{
    public class MapEditorWindow : EditorWindow
    {
        private const int DefaultGridColumns = 10;
        private const int DefaultGridRows = 8;
        private const float NodeWidth = 142f;
        private const float NodeHeight = 52f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 2.5f;

        private EncounterData _encounterData;
        private SerializedObject _serializedEncounterData;
        private int _selectedStageIndex;
        private int _selectedNodeIndex = -1;
        private int _draggedNodeIndex = -1;
        private float _graphZoom = 1f;
        private Vector2 _dragOffset;
        private Vector2 _graphScroll;
        private Vector2 _inspectorScroll;
        private Vector2 _settingsScroll;
        private bool _showStageData;
        private bool _showGridSettings = true;
        [SerializeField] private int _gridColumns = DefaultGridColumns;
        [SerializeField] private int _gridRows = DefaultGridRows;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private bool _showGrid = true;
        private string _validationMessage;
        private MessageType _validationMessageType;

        [MenuItem("Tools/NueDeck/Map Editor")]
        private static void Open()
        {
            GetWindow<MapEditorWindow>("Map Editor");
        }

        private void OnGUI()
        {
            DrawAssetToolbar();
            if (!_encounterData)
            {
                EditorGUILayout.HelpBox("Assign an EncounterData asset to edit its map.", MessageType.Info);
                return;
            }

            if (_serializedEncounterData == null || _serializedEncounterData.targetObject != _encounterData)
                _serializedEncounterData = new SerializedObject(_encounterData);

            _serializedEncounterData.Update();
            var stages = _serializedEncounterData.FindProperty("enemyEncounterList");
            DrawStageToolbar(stages);

            if (stages.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Add a stage to start building a map.", MessageType.Info);
                ApplyChanges();
                return;
            }

            _selectedStageIndex = Mathf.Clamp(_selectedStageIndex, 0, stages.arraySize - 1);
            var stage = stages.GetArrayElementAtIndex(_selectedStageIndex);

            EditorGUILayout.BeginHorizontal();
            DrawStageSettings(stage);
            DrawGraphPanel(stage.FindPropertyRelative("mapNodeList"));
            DrawNodeInspector(stage);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_validationMessage))
                EditorGUILayout.HelpBox(_validationMessage, _validationMessageType);

            ApplyChanges();
        }

        private void DrawAssetToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var selected = (EncounterData)EditorGUILayout.ObjectField(
                "Encounter Data", _encounterData, typeof(EncounterData), false, GUILayout.MinWidth(260f));
            if (selected != _encounterData)
            {
                _encounterData = selected;
                _serializedEncounterData = _encounterData ? new SerializedObject(_encounterData) : null;
                _selectedStageIndex = 0;
                _selectedNodeIndex = -1;
                _validationMessage = null;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStageToolbar(SerializedProperty stages)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (stages.arraySize > 0)
            {
                var labels = new string[stages.arraySize];
                for (var i = 0; i < stages.arraySize; i++)
                {
                    var stage = stages.GetArrayElementAtIndex(i);
                    var name = stage.FindPropertyRelative("name").stringValue;
                    var id = stage.FindPropertyRelative("stageId").intValue;
                    labels[i] = string.IsNullOrWhiteSpace(name) ? $"Stage {id}" : $"{name} ({id})";
                }

                _selectedStageIndex = EditorGUILayout.Popup(_selectedStageIndex, labels, GUILayout.MinWidth(180f));
            }
            else
            {
                GUILayout.Label("No stages", EditorStyles.toolbarButton);
            }

            if (GUILayout.Button("Add Stage", EditorStyles.toolbarButton))
                AddStage(stages);

            using (new EditorGUI.DisabledScope(stages.arraySize <= 1))
            {
                if (GUILayout.Button("Remove Stage", EditorStyles.toolbarButton))
                    RemoveStage(stages);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                ValidateCurrentStage(stages);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStageSettings(SerializedProperty stage)
        {
            _settingsScroll = EditorGUILayout.BeginScrollView(
                _settingsScroll, GUILayout.Width(300f), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var randomEncounters = _serializedEncounterData.FindProperty("encounterRandomlyAtStage");
            EditorGUILayout.PropertyField(randomEncounters, new GUIContent("Random encounters"));

            _showStageData = EditorGUILayout.Foldout(_showStageData, "Stage encounter and event data", true);
            if (_showStageData)
            {
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("name"));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("stageId"));
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("enemyEncounterList"), new GUIContent("Encounter Data"), true);
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("bossEncounterList"), new GUIContent("Boss Data"), true);
                EditorGUILayout.PropertyField(stage.FindPropertyRelative("eventPool"), new GUIContent("Event Pool"), true);
                DrawStartNodeField(stage);
            }
            DrawGridSettings(stage.FindPropertyRelative("mapNodeList"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawGridSettings(SerializedProperty nodes)
        {
            _showGridSettings = EditorGUILayout.Foldout(_showGridSettings, "Grid Settings", true);
            if (!_showGridSettings)
                return;

            EditorGUI.indentLevel++;
            _gridColumns = EditorGUILayout.IntSlider("Columns", _gridColumns, 1, 100);
            _gridRows = EditorGUILayout.IntSlider("Rows", _gridRows, 1, 100);
            _showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
            _snapToGrid = EditorGUILayout.Toggle("Snap While Dragging", _snapToGrid);
            var effectiveColumns = GetGridColumnCount(nodes);
            if (effectiveColumns != _gridColumns)
                EditorGUILayout.HelpBox($"The map uses at least {effectiveColumns} columns to accommodate its nodes.", MessageType.None);

            if (GUILayout.Button("Snap All Nodes To Grid"))
                SnapAllNodes(nodes, effectiveColumns);
            EditorGUI.indentLevel--;
        }

        private void SnapAllNodes(SerializedProperty nodes, int columns)
        {
            Undo.RecordObject(_encounterData, "Snap map nodes to grid");
            for (var i = 0; i < nodes.arraySize; i++)
            {
                var position = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("position");
                position.vector2Value = SnapToGrid(position.vector2Value, columns, _gridRows);
            }
        }

        private int GetGridColumnCount(SerializedProperty nodes)
        {
            return Mathf.Max(_gridColumns, nodes.arraySize);
        }

        private void DrawStartNodeField(SerializedProperty stage)
        {
            var nodes = stage.FindPropertyRelative("mapNodeList");
            var startNode = stage.FindPropertyRelative("mapStartNodeId");
            if (nodes.arraySize == 0)
            {
                EditorGUILayout.PropertyField(startNode, new GUIContent("Starting Node ID"));
                return;
            }

            var labels = new string[nodes.arraySize];
            var selectedIndex = -1;
            for (var i = 0; i < nodes.arraySize; i++)
            {
                var node = nodes.GetArrayElementAtIndex(i);
                var id = node.FindPropertyRelative("nodeId").stringValue;
                var name = node.FindPropertyRelative("displayName").stringValue;
                labels[i] = string.IsNullOrWhiteSpace(name) ? id : $"{name} ({id})";
                if (id == startNode.stringValue)
                    selectedIndex = i;
            }

            var newIndex = EditorGUILayout.Popup("Starting Node", Mathf.Max(0, selectedIndex), labels);
            if (newIndex >= 0 && newIndex < nodes.arraySize)
                startNode.stringValue = nodes.GetArrayElementAtIndex(newIndex)
                    .FindPropertyRelative("nodeId").stringValue;
        }

        private void DrawGraph(Rect graphRect, SerializedProperty nodes, float horizontalBaseWidth)
        {
            GUI.BeginGroup(graphRect);
            var localRect = new Rect(0f, 0f, graphRect.width, graphRect.height);
            EditorGUI.DrawRect(localRect, new Color(0.13f, 0.14f, 0.17f));
            if (_showGrid)
                DrawGrid(localRect, horizontalBaseWidth, _graphZoom, GetGridColumnCount(nodes), _gridRows);
            DrawConnections(nodes, localRect, horizontalBaseWidth, _graphZoom);
            DrawGraphNodes(nodes, localRect, horizontalBaseWidth, _graphZoom);
            HandleGraphMouseEvents(
                nodes, localRect, horizontalBaseWidth, _graphZoom, GetGridColumnCount(nodes), _gridRows);
            GUI.EndGroup();
        }

        private void DrawGraphPanel(SerializedProperty nodes)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                _graphZoom = Mathf.Clamp(_graphZoom - 0.1f, MinZoom, MaxZoom);
            _graphZoom = GUILayout.HorizontalSlider(_graphZoom, MinZoom, MaxZoom, GUILayout.Width(110f));
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24f)))
                _graphZoom = Mathf.Clamp(_graphZoom + 0.1f, MinZoom, MaxZoom);
            GUILayout.Label($"{Mathf.RoundToInt(_graphZoom * 100f)}%", EditorStyles.toolbarButton, GUILayout.Width(45f));
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            {
                _graphZoom = 1f;
                _graphScroll = Vector2.zero;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var positions = new List<Vector2>(nodes.arraySize);
            for (var i = 0; i < nodes.arraySize; i++)
                positions.Add(nodes.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value);
            var minimumGraphWidth = Mathf.Max(400f, position.width - 620f) * _graphZoom;
            var nodeWidth = NodeWidth * _graphZoom;
            var graphWidth = MapManager.GetRequiredContentWidth(positions, minimumGraphWidth, nodeWidth);
            var graphHeight = Mathf.Max(360f, position.height - 130f) * _graphZoom;
            _graphScroll = EditorGUILayout.BeginScrollView(
                _graphScroll, true, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var graphRect = GUILayoutUtility.GetRect(
                graphWidth, graphHeight, GUILayout.Width(graphWidth), GUILayout.Height(graphHeight));
            DrawGraph(graphRect, nodes, minimumGraphWidth);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawGrid(Rect rect, float horizontalBaseWidth, float zoom, int columns, int rows)
        {
            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.08f);
            var horizontalUnitWidth = Mathf.Max(1f, horizontalBaseWidth - NodeWidth * zoom);
            var columnSpacing = horizontalUnitWidth / columns;
            for (var x = NodeWidth * zoom * 0.5f; x <= rect.width; x += columnSpacing)
            {
                Handles.DrawLine(new Vector3(x, 0f), new Vector3(x, rect.height));
            }

            for (var row = 0; row <= rows; row++)
            {
                var y = Mathf.Lerp(0f, rect.height, row / (float)rows);
                Handles.DrawLine(new Vector3(0f, y), new Vector3(rect.width, y));
            }
            Handles.EndGUI();
        }

        private static void DrawConnections(
            SerializedProperty nodes, Rect graphRect, float horizontalBaseWidth, float zoom)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.88f, 0.8f, 0.58f, 0.9f);
            for (var i = 0; i < nodes.arraySize; i++)
            {
                var node = nodes.GetArrayElementAtIndex(i);
                var from = NodeCenter(node.FindPropertyRelative("position").vector2Value, graphRect, horizontalBaseWidth, zoom);
                var nextIds = node.FindPropertyRelative("nextNodeIds");
                for (var j = 0; j < nextIds.arraySize; j++)
                {
                    var targetIndex = FindNodeIndex(nodes, nextIds.GetArrayElementAtIndex(j).stringValue);
                    if (targetIndex < 0)
                        continue;

                    var target = nodes.GetArrayElementAtIndex(targetIndex);
                    var to = NodeCenter(target.FindPropertyRelative("position").vector2Value, graphRect, horizontalBaseWidth, zoom);
                    Handles.DrawAAPolyLine(3f * zoom, from, to);
                }
            }
            Handles.EndGUI();
        }

        private void DrawGraphNodes(
            SerializedProperty nodes, Rect graphRect, float horizontalBaseWidth, float zoom)
        {
            var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = Mathf.Max(8, Mathf.RoundToInt(EditorStyles.centeredGreyMiniLabel.fontSize * zoom))
            };
            for (var i = 0; i < nodes.arraySize; i++)
            {
                var node = nodes.GetArrayElementAtIndex(i);
                var nodeRect = GetNodeRect(
                    node.FindPropertyRelative("position").vector2Value, graphRect, horizontalBaseWidth, zoom);
                var type = (EncounterMapNodeType)node.FindPropertyRelative("nodeType").enumValueIndex;
                var isFinal = node.FindPropertyRelative("isFinal").boolValue;
                var color = GetNodeColor(type);
                if (i == _selectedNodeIndex)
                    color = Color.Lerp(color, Color.white, 0.28f);

                EditorGUI.DrawRect(nodeRect, color);
                GUI.Box(nodeRect, GUIContent.none, EditorStyles.helpBox);
                if (isFinal)
                    EditorGUI.DrawRect(new Rect(nodeRect.x, nodeRect.y, nodeRect.width, 4f), new Color(1f, 0.78f, 0.3f));
                var name = node.FindPropertyRelative("displayName").stringValue;
                var labelRect = isFinal
                    ? new Rect(nodeRect.x, nodeRect.y + 4f, nodeRect.width, nodeRect.height - 4f)
                    : nodeRect;
                var label = string.IsNullOrWhiteSpace(name) ? type.ToString() : name;
                GUI.Label(labelRect, isFinal ? $"[STAGE EXIT] {label}" : label, labelStyle);
            }
        }

        private void HandleGraphMouseEvents(
            SerializedProperty nodes, Rect graphRect, float horizontalBaseWidth, float zoom, int gridColumns, int gridRows)
        {
            var currentEvent = Event.current;
            var dragControlId = GUIUtility.GetControlID(0x4D4150, FocusType.Passive);
            if (currentEvent.button != 0)
                return;

            if (currentEvent.type == EventType.MouseDown)
            {
                for (var i = nodes.arraySize - 1; i >= 0; i--)
                {
                    var position = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2Value;
                    var nodeRect = GetNodeRect(position, graphRect, horizontalBaseWidth, zoom);
                    if (!nodeRect.Contains(currentEvent.mousePosition))
                        continue;

                    _selectedNodeIndex = i;
                    _draggedNodeIndex = i;
                    _dragOffset = currentEvent.mousePosition - nodeRect.position;
                    GUIUtility.hotControl = dragControlId;
                    Undo.RecordObject(_encounterData, "Move map node");
                    currentEvent.Use();
                    Repaint();
                    return;
                }
            }
            else if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == dragControlId &&
                     _draggedNodeIndex >= 0 && _draggedNodeIndex < nodes.arraySize)
            {
                var node = nodes.GetArrayElementAtIndex(_draggedNodeIndex);
                var desiredPosition = currentEvent.mousePosition - _dragOffset +
                                      new Vector2(NodeWidth * zoom * 0.5f, NodeHeight * zoom * 0.5f);
                var position = GraphPosition(desiredPosition, graphRect, horizontalBaseWidth, zoom);
                node.FindPropertyRelative("position").vector2Value = _snapToGrid
                    ? SnapToGrid(position, gridColumns, gridRows)
                    : position;
                currentEvent.Use();
                Repaint();
            }
            else if (currentEvent.type == EventType.MouseUp && GUIUtility.hotControl == dragControlId)
            {
                _draggedNodeIndex = -1;
                GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        private void DrawNodeInspector(SerializedProperty stage)
        {
            _inspectorScroll = EditorGUILayout.BeginScrollView(
                _inspectorScroll, GUILayout.Width(300f), GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical();
            var nodes = stage.FindPropertyRelative("mapNodeList");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Map Nodes", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Node", GUILayout.Width(86f)))
                AddNode(stage, nodes);
            EditorGUILayout.EndHorizontal();

            if (nodes.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This stage has no map nodes.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }

            _selectedNodeIndex = Mathf.Clamp(_selectedNodeIndex, 0, nodes.arraySize - 1);
            var selectedNode = nodes.GetArrayElementAtIndex(_selectedNodeIndex);
            var nodeId = selectedNode.FindPropertyRelative("nodeId").stringValue;
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(nodeId) ? "Selected Node" : nodeId, EditorStyles.boldLabel);
            if (GUILayout.Button("Delete", GUILayout.Width(60f)))
            {
                RemoveNode(stage, nodes, _selectedNodeIndex);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(selectedNode.FindPropertyRelative("nodeId"));
            EditorGUILayout.PropertyField(selectedNode.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(selectedNode.FindPropertyRelative("nodeType"));
            EditorGUILayout.PropertyField(
                selectedNode.FindPropertyRelative("isFinal"),
                new GUIContent("Stage Transition Node", "Completing this node advances to the next stage."));
            EditorGUILayout.PropertyField(selectedNode.FindPropertyRelative("roomNumber"));
            EditorGUILayout.PropertyField(selectedNode.FindPropertyRelative("position"));
            DrawTypeSpecificSettings(stage, selectedNode);
            DrawConnectionsEditor(nodes, _selectedNodeIndex);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawTypeSpecificSettings(SerializedProperty stage, SerializedProperty node)
        {
            var type = (EncounterMapNodeType)node.FindPropertyRelative("nodeType").enumValueIndex;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Node Data", EditorStyles.boldLabel);
            switch (type)
            {
                case EncounterMapNodeType.Encounter:
                    DrawEncounterSelection(stage, node, "enemyEncounterList", "Encounter Data");
                    break;
                case EncounterMapNodeType.Boss:
                    DrawEncounterSelection(stage, node, "bossEncounterList", "Boss Data");
                    break;
                case EncounterMapNodeType.Shop:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("shopContainerData"),
                        new GUIContent("Shop Container"));
                    break;
                case EncounterMapNodeType.Campfire:
                case EncounterMapNodeType.Treasure:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("eventData"),
                        new GUIContent("Event Data"));
                    break;
                case EncounterMapNodeType.Event:
                case EncounterMapNodeType.Unknown:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("eventData"),
                        new GUIContent("Event Override"));
                    EditorGUILayout.HelpBox("Leave the override empty to choose an event from this stage's Event Pool.", MessageType.None);
                    break;
            }
        }

        private static void DrawEncounterSelection(SerializedProperty stage, SerializedProperty node,
            string listName, string label)
        {
            var encounters = stage.FindPropertyRelative(listName);
            var encounterIndex = node.FindPropertyRelative("encounterIndex");
            if (encounters.arraySize == 0)
            {
                EditorGUILayout.HelpBox($"Add {label} to this stage before assigning it to the node.", MessageType.Warning);
                encounterIndex.intValue = 0;
                return;
            }

            var labels = new string[encounters.arraySize];
            for (var i = 0; i < encounters.arraySize; i++)
            {
                var enemyList = encounters.GetArrayElementAtIndex(i).FindPropertyRelative("enemyList");
                labels[i] = $"{label} {i + 1} ({enemyList.arraySize} enemies)";
            }

            encounterIndex.intValue = EditorGUILayout.Popup(label, Mathf.Clamp(encounterIndex.intValue, 0, encounters.arraySize - 1), labels);
            EditorGUILayout.PropertyField(encounters.GetArrayElementAtIndex(encounterIndex.intValue),
                new GUIContent("Selected " + label), true);
        }

        private void DrawConnectionsEditor(SerializedProperty nodes, int sourceIndex)
        {
            var source = nodes.GetArrayElementAtIndex(sourceIndex);
            var nextIds = source.FindPropertyRelative("nextNodeIds");
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);

            for (var i = 0; i < nodes.arraySize; i++)
            {
                if (i == sourceIndex)
                    continue;

                var target = nodes.GetArrayElementAtIndex(i);
                var targetId = target.FindPropertyRelative("nodeId").stringValue;
                var targetName = target.FindPropertyRelative("displayName").stringValue;
                var label = string.IsNullOrWhiteSpace(targetName) ? targetId : $"{targetName} ({targetId})";
                var connectionIndex = FindStringIndex(nextIds, targetId);
                var connected = connectionIndex >= 0;
                var newConnected = EditorGUILayout.ToggleLeft(label, connected);
                if (newConnected == connected)
                    continue;

                if (newConnected)
                {
                    nextIds.InsertArrayElementAtIndex(nextIds.arraySize);
                    nextIds.GetArrayElementAtIndex(nextIds.arraySize - 1).stringValue = targetId;
                }
                else
                {
                    nextIds.DeleteArrayElementAtIndex(connectionIndex);
                }
            }
        }

        private void AddStage(SerializedProperty stages)
        {
            Undo.RecordObject(_encounterData, "Add map stage");
            var newIndex = stages.arraySize;
            stages.InsertArrayElementAtIndex(newIndex);
            var stage = stages.GetArrayElementAtIndex(newIndex);
            stage.FindPropertyRelative("name").stringValue = $"Stage {newIndex + 1}";
            stage.FindPropertyRelative("stageId").intValue = GetNextStageId(stages);
            stage.FindPropertyRelative("mapStartNodeId").stringValue = string.Empty;
            stage.FindPropertyRelative("enemyEncounterList").arraySize = 0;
            stage.FindPropertyRelative("bossEncounterList").arraySize = 0;
            stage.FindPropertyRelative("eventPool").arraySize = 0;
            stage.FindPropertyRelative("mapNodeList").arraySize = 0;
            _selectedStageIndex = newIndex;
            _selectedNodeIndex = -1;
        }

        private static int GetNextStageId(SerializedProperty stages)
        {
            var nextId = 0;
            for (var i = 0; i < stages.arraySize; i++)
                nextId = Mathf.Max(nextId, stages.GetArrayElementAtIndex(i).FindPropertyRelative("stageId").intValue + 1);
            return nextId;
        }

        private void RemoveStage(SerializedProperty stages)
        {
            if (stages.arraySize <= 1)
                return;

            Undo.RecordObject(_encounterData, "Remove map stage");
            stages.DeleteArrayElementAtIndex(_selectedStageIndex);
            _selectedStageIndex = Mathf.Clamp(_selectedStageIndex, 0, stages.arraySize - 1);
            _selectedNodeIndex = -1;
        }

        private void AddNode(SerializedProperty stage, SerializedProperty nodes)
        {
            Undo.RecordObject(_encounterData, "Add map node");
            var newIndex = nodes.arraySize;
            nodes.InsertArrayElementAtIndex(newIndex);
            var node = nodes.GetArrayElementAtIndex(newIndex);
            var nodeId = CreateUniqueNodeId(nodes);
            node.FindPropertyRelative("nodeId").stringValue = nodeId;
            node.FindPropertyRelative("displayName").stringValue = $"Node {newIndex + 1}";
            node.FindPropertyRelative("nodeType").enumValueIndex = (int)EncounterMapNodeType.Encounter;
            node.FindPropertyRelative("isFinal").boolValue = false;
            node.FindPropertyRelative("shopContainerData").objectReferenceValue = null;
            node.FindPropertyRelative("eventData").objectReferenceValue = null;
            node.FindPropertyRelative("encounterIndex").intValue = 0;
            node.FindPropertyRelative("roomNumber").intValue = newIndex + 1;
            var columnCount = Mathf.Max(1, _gridColumns);
            var rowCount = Mathf.Max(1, _gridRows);
            var columnIndex = (newIndex / rowCount) % columnCount;
            var rowIndex = newIndex % rowCount;
            node.FindPropertyRelative("position").vector2Value = new Vector2(
                columnCount == 1 ? 0.5f : Mathf.Lerp(0.1f, 0.9f, columnIndex / (float)(columnCount - 1)),
                rowCount == 1 ? 0.5f : Mathf.Lerp(0.1f, 0.9f, rowIndex / (float)(rowCount - 1)));
            node.FindPropertyRelative("nextNodeIds").arraySize = 0;

            var startNode = stage.FindPropertyRelative("mapStartNodeId");
            if (string.IsNullOrWhiteSpace(startNode.stringValue))
                startNode.stringValue = nodeId;
            _selectedNodeIndex = newIndex;
        }

        private static string CreateUniqueNodeId(SerializedProperty nodes)
        {
            var suffix = nodes.arraySize;
            string nodeId;
            do
            {
                nodeId = $"node-{suffix++}";
            } while (FindNodeIndex(nodes, nodeId) >= 0);
            return nodeId;
        }

        private void RemoveNode(SerializedProperty stage, SerializedProperty nodes, int index)
        {
            if (index < 0 || index >= nodes.arraySize)
                return;

            Undo.RecordObject(_encounterData, "Remove map node");
            var nodeId = nodes.GetArrayElementAtIndex(index).FindPropertyRelative("nodeId").stringValue;
            for (var i = 0; i < nodes.arraySize; i++)
            {
                var nextIds = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("nextNodeIds");
                for (var j = nextIds.arraySize - 1; j >= 0; j--)
                    if (nextIds.GetArrayElementAtIndex(j).stringValue == nodeId)
                        nextIds.DeleteArrayElementAtIndex(j);
            }

            nodes.DeleteArrayElementAtIndex(index);
            var startNode = stage.FindPropertyRelative("mapStartNodeId");
            if (startNode.stringValue == nodeId)
                startNode.stringValue = nodes.arraySize > 0
                    ? nodes.GetArrayElementAtIndex(0).FindPropertyRelative("nodeId").stringValue
                    : string.Empty;
            _selectedNodeIndex = nodes.arraySize > 0 ? Mathf.Clamp(index, 0, nodes.arraySize - 1) : -1;
        }

        private void ValidateCurrentStage(SerializedProperty stages)
        {
            _serializedEncounterData.ApplyModifiedProperties();
            _serializedEncounterData.Update();
            var currentStages = _serializedEncounterData.FindProperty("enemyEncounterList");
            var stageId = currentStages.GetArrayElementAtIndex(_selectedStageIndex)
                .FindPropertyRelative("stageId").intValue;
            var result = _encounterData.GetMapNodes(stageId);
            _validationMessage = result == null
                ? "Stage validation failed. See the Console for details."
                : $"Stage {stageId} is valid ({result.Count} nodes).";
            _validationMessageType = result == null ? MessageType.Error : MessageType.Info;
        }

        private void ApplyChanges()
        {
            if (_serializedEncounterData.ApplyModifiedProperties())
                EditorUtility.SetDirty(_encounterData);
        }

        private static int FindNodeIndex(SerializedProperty nodes, string nodeId)
        {
            for (var i = 0; i < nodes.arraySize; i++)
                if (nodes.GetArrayElementAtIndex(i).FindPropertyRelative("nodeId").stringValue == nodeId)
                    return i;
            return -1;
        }

        private static int FindStringIndex(SerializedProperty strings, string value)
        {
            for (var i = 0; i < strings.arraySize; i++)
                if (strings.GetArrayElementAtIndex(i).stringValue == value)
                    return i;
            return -1;
        }

        private static Rect GetNodeRect(
            Vector2 position, Rect graphRect, float horizontalBaseWidth, float zoom)
        {
            var center = NodeCenter(position, graphRect, horizontalBaseWidth, zoom);
            var width = NodeWidth * zoom;
            var height = NodeHeight * zoom;
            return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
        }

        private static Vector2 NodeCenter(
            Vector2 position, Rect graphRect, float horizontalBaseWidth, float zoom)
        {
            const float margin = 30f;
            var horizontalUnitWidth = Mathf.Max(1f, horizontalBaseWidth - NodeWidth * zoom);
            return new Vector2(
                NodeWidth * zoom * 0.5f + position.x * horizontalUnitWidth,
                Mathf.Lerp(graphRect.height - margin, margin, position.y));
        }

        private static Vector2 GraphPosition(
            Vector2 point, Rect graphRect, float horizontalBaseWidth, float zoom)
        {
            const float margin = 30f;
            var horizontalUnitWidth = Mathf.Max(1f, horizontalBaseWidth - NodeWidth * zoom);
            return new Vector2(
                Mathf.Max(0f, (point.x - NodeWidth * zoom * 0.5f) / horizontalUnitWidth),
                Mathf.InverseLerp(graphRect.height - margin, margin, point.y));
        }

        private static Vector2 SnapToGrid(Vector2 position, int columns, int rows)
        {
            return new Vector2(
                Mathf.Max(0f, Mathf.Round(position.x * columns) / columns),
                Mathf.Clamp(Mathf.Round(position.y * rows) / rows, 0f, 1f));
        }

        private static Color GetNodeColor(EncounterMapNodeType type)
        {
            switch (type)
            {
                case EncounterMapNodeType.Encounter: return new Color(0.3f, 0.45f, 0.68f);
                case EncounterMapNodeType.Boss: return new Color(0.72f, 0.24f, 0.22f);
                case EncounterMapNodeType.Shop: return new Color(0.22f, 0.55f, 0.34f);
                case EncounterMapNodeType.Campfire: return new Color(0.72f, 0.36f, 0.15f);
                case EncounterMapNodeType.Treasure: return new Color(0.72f, 0.56f, 0.16f);
                case EncounterMapNodeType.Event: return new Color(0.5f, 0.36f, 0.68f);
                case EncounterMapNodeType.Unknown: return new Color(0.42f, 0.44f, 0.49f);
                default: return Color.gray;
            }
        }
    }
}
