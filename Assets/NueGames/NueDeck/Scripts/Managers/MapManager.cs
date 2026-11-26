using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Data.Containers;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.UI;
using NueGames.NueDeck.Scripts.UI.Shop;
using NueGames.NueDeck.Scripts.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace NueGames.NueDeck.Scripts.Managers
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private EncounterButton encounterButtonPrefab;

        private GameManager GameManager => GameManager.Instance;
        private UIManager UIManager => UIManager.Instance;
        private RectTransform _nodeRoot;
        private float _mapBaseWidth;

        private void Start() => BuildMap();

        private void BuildMap()
        {
            if (!encounterButtonPrefab)
            {
                Debug.LogError("MapManager requires an EncounterButton prefab.", this);
                return;
            }

            _nodeRoot = FindNodeRoot();
            if (!_nodeRoot)
            {
                Debug.LogError("MapManager could not find the MapCanvas/EncounterParent RectTransform.", this);
                return;
            }

            var nodes = GameManager.EncounterData.GetMapNodes(GameManager.PersistentGameplayData.CurrentStageId);
            if (nodes == null) return;
            ClearMapRoot();
            ResizeMapRoot(nodes);
            DrawConnections(nodes);
            BuildNodes(nodes);
            UpdateRoomLabel(nodes);
        }

        private RectTransform FindNodeRoot()
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
                if (child.name == "EncounterParent") return child as RectTransform;
            return null;
        }

        private void ClearMapRoot()
        {
            var layout = _nodeRoot.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (layout) layout.enabled = false;
            for (var i = _nodeRoot.childCount - 1; i >= 0; i--)
            {
                var child = _nodeRoot.GetChild(i);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private void ResizeMapRoot(List<EncounterMapNode> nodes)
        {
            var scrollRect = _nodeRoot.GetComponentInParent<ScrollRect>();
            var viewport = scrollRect ? scrollRect.viewport : _nodeRoot.parent as RectTransform;
            var viewportWidth = viewport ? viewport.rect.width : 0f;
            var canvas = _nodeRoot.GetComponentInParent<Canvas>();
            var screenWidth = canvas
                ? Screen.width / Mathf.Max(canvas.scaleFactor, 0.0001f)
                : Screen.width;
            var minimumWidth = Mathf.Max(viewportWidth, screenWidth);
            _mapBaseWidth = minimumWidth;
            var requiredWidth = GetRequiredContentWidth(
                nodes.Select(node => node.Position), minimumWidth, 240f);

            _nodeRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth + 200f);
            _nodeRoot.anchoredPosition = new Vector2(-viewportWidth * 0.5f, _nodeRoot.anchoredPosition.y);
            if (scrollRect)
                scrollRect.horizontalNormalizedPosition = 0f;
        }

        public static float GetRequiredContentWidth(
            IEnumerable<Vector2> nodePositions, float minimumWidth, float nodeWidth = 240f)
        {
            var maxX = 0f;
            foreach (var position in nodePositions)
                maxX = Mathf.Max(maxX, position.x);

            var usableWidth = Mathf.Max(0f, minimumWidth - nodeWidth);
            return Mathf.Max(minimumWidth, nodeWidth + maxX * usableWidth);
        }

        private void DrawConnections(List<EncounterMapNode> nodes)
        {
            var nodesById = nodes.ToDictionary(node => node.NodeId);
            foreach (var node in nodes)
            {
                if (node.NextNodeIds == null) continue;
                foreach (var nextNodeId in node.NextNodeIds)
                {
                    if (!nodesById.TryGetValue(nextNodeId, out var nextNode))
                    {
                        Debug.LogError($"Map node '{node.NodeId}' references missing next node '{nextNodeId}'.", GameManager.EncounterData);
                        continue;
                    }
                    CreateConnection(node.Position, nextNode.Position);
                }
            }
        }

        private void CreateConnection(Vector2 from, Vector2 to)
        {
            var lineObject = new GameObject("MapConnection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObject.transform.SetParent(_nodeRoot, false);
            var lineRect = lineObject.GetComponent<RectTransform>();
            var fromPosition = GetAnchoredPosition(from);
            var toPosition = GetAnchoredPosition(to);
            var direction = toPosition - fromPosition;
            lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = (fromPosition + toPosition) * 0.5f;
            lineRect.sizeDelta = new Vector2(direction.magnitude, 6f);
            lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            var image = lineObject.GetComponent<Image>();
            image.color = new Color(0.85f, 0.82f, 0.72f, 0.7f);
            image.raycastTarget = false;
        }

        private void BuildNodes(List<EncounterMapNode> nodes)
        {
            var progress = GameManager.PersistentGameplayData;
            foreach (var mapNode in nodes)
            {
                var nodeButton = Instantiate(encounterButtonPrefab, _nodeRoot);
                var sceneChanger = nodeButton.GetComponent<SceneChanger>();
                if (IsCombatNode(mapNode.NodeType) && !sceneChanger)
                {
                    Debug.LogError("Combat EncounterButton is missing SceneChanger.", nodeButton);
                    Destroy(nodeButton.gameObject);
                    continue;
                }

                var nodeId = mapNode.NodeId;
                nodeButton.Configure(
                    mapNode.DisplayName,
                    mapNode.NodeType,
                    mapNode.IsFinal,
                    () => SelectNode(mapNode, nodeId, sceneChanger));
                var nodeRect = nodeButton.GetComponent<RectTransform>();
                nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
                nodeRect.pivot = new Vector2(0.5f, 0.5f);
                nodeRect.anchoredPosition = GetAnchoredPosition(mapNode.Position);
                nodeRect.sizeDelta = new Vector2(240f, 76f);

                if (progress.CompletedMapNodeIds.Contains(nodeId)) nodeButton.SetStatus(EncounterButtonStatus.Completed);
                else if (progress.AvailableMapNodeIds.Contains(nodeId)) nodeButton.SetStatus(EncounterButtonStatus.Active);
                else nodeButton.SetStatus(EncounterButtonStatus.Passive);
            }
        }

        private void SelectNode(EncounterMapNode node, string nodeId, SceneChanger sceneChanger)
        {
            if (node.NodeType == EncounterMapNodeType.Shop && (!UIManager || !UIManager.ShopCanvas))
            {
                Debug.LogError("UIManager has no ShopCanvas assigned.", UIManager);
                return;
            }
            if (IsInteractiveNode(node.NodeType) && (!UIManager || !UIManager.NodeEventCanvas))
            {
                Debug.LogError("UIManager has no NodeEventCanvas assigned.", UIManager);
                return;
            }

            if (!GameManager.TrySelectMapNode(nodeId)) return;
            switch (node.NodeType)
            {
                case EncounterMapNodeType.Shop:
                    if (UIManager.CombatCanvas) UIManager.CombatCanvas.CloseCanvas();
                    UIManager.ShopCanvas.OpenShop(node.ShopContainerData, RefreshMap);
                    break;
                case EncounterMapNodeType.Campfire:
                case EncounterMapNodeType.Treasure:
                case EncounterMapNodeType.Event:
                case EncounterMapNodeType.Unknown:
                    var eventData = GameManager.SelectedMapEventData;
                    if (!eventData)
                    {
                        Debug.LogError($"No event data could be resolved for node '{node.NodeId}'.", GameManager.EncounterData);
                        return;
                    }
                    UIManager.NodeEventCanvas.OpenEvent(eventData, RefreshMap);
                    break;
                default:
                    sceneChanger.OpenCombatScene();
                    break;
            }
        }

        private void RefreshMap()
        {
            if (this) BuildMap();
        }

        private static bool IsCombatNode(EncounterMapNodeType type) =>
            type == EncounterMapNodeType.Encounter || type == EncounterMapNodeType.Boss;

        private static bool IsInteractiveNode(EncounterMapNodeType type) =>
            type == EncounterMapNodeType.Campfire || type == EncounterMapNodeType.Treasure ||
            type == EncounterMapNodeType.Event || type == EncounterMapNodeType.Unknown;

        private Vector2 GetAnchoredPosition(Vector2 position)
        {
            var rect = _nodeRoot.rect;
            var usableWidth = Mathf.Max(0f, _mapBaseWidth - 240f);
            var x = 120f + position.x * usableWidth - rect.width * 0.5f;
            return new Vector2(x, (position.y - 0.5f) * rect.height);
        }

        private void UpdateRoomLabel(List<EncounterMapNode> nodes)
        {
            var progress = GameManager.PersistentGameplayData;
            var nextNodeId = progress.AvailableMapNodeIds.FirstOrDefault();
            var nextNode = nodes.FirstOrDefault(node => node.NodeId == nextNodeId);
            var roomNumber = nextNode != null ? nextNode.RoomNumber : progress.CurrentEncounterId + 1;
            UIManager.InformationCanvas.SetRoomText(roomNumber, GameManager.GameplayData.UseStageSystem, progress.CurrentStageId + 1);
        }
    }
}
