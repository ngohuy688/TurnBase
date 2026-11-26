using System;
using System.Collections.Generic;
using System.Linq;
using NueGames.NueDeck.Scripts.Characters;
using NueGames.NueDeck.Scripts.Data.Characters;
using NueGames.NueDeck.Scripts.Enums;
using NueGames.NueDeck.Scripts.NueExtentions;
using UnityEngine;

namespace NueGames.NueDeck.Scripts.Data.Containers
{
    [CreateAssetMenu(fileName = "Encounter Data", menuName = "NueDeck/Containers/EncounterData", order = 4)]
    public class EncounterData : ScriptableObject
    {
        [Header("Settings")]
        [SerializeField] private bool encounterRandomlyAtStage;
        [SerializeField] private List<EnemyEncounterStage> enemyEncounterList;

        public bool EncounterRandomlyAtStage => encounterRandomlyAtStage;
        public List<EnemyEncounterStage> EnemyEncounterList => enemyEncounterList;

        public EnemyEncounter GetEnemyEncounter(int stageId = 0, int encounterId = 0, bool isFinal = false)
        {
            var stage = EnemyEncounterList?.FirstOrDefault(x => x.StageId == stageId);
            if (stage == null)
                return null;

            var encounters = isFinal ? stage.BossEncounterList : stage.EnemyEncounterList;
            if (encounters == null || encounters.Count == 0)
                return null;

            return encounterRandomlyAtStage && !isFinal
                ? encounters.RandomItem()
                : encounters[Mathf.Clamp(encounterId, 0, encounters.Count - 1)];
        }

        public bool TryGetNextStageId(int currentStageId, out int nextStageId)
        {
            nextStageId = 0;
            if (EnemyEncounterList == null || EnemyEncounterList.Count == 0)
            {
                Debug.LogError("EncounterData has no stages configured.", this);
                return false;
            }

            var orderedStages = EnemyEncounterList.OrderBy(stage => stage.StageId).ToList();
            var currentStageIndex = orderedStages.FindIndex(stage => stage.StageId == currentStageId);
            if (currentStageIndex < 0)
            {
                Debug.LogError($"Stage ID {currentStageId} is not configured in EncounterData.", this);
                return false;
            }

            if (currentStageIndex + 1 >= orderedStages.Count)
                return false;

            nextStageId = orderedStages[currentStageIndex + 1].StageId;
            return true;
        }

        public string GetStartingMapNodeId(int stageId)
        {
            var stage = EnemyEncounterList?.FirstOrDefault(x => x.StageId == stageId);
            if (stage == null)
            {
                Debug.LogError($"Stage ID {stageId} is not configured in EncounterData.", this);
                return null;
            }

            if (string.IsNullOrWhiteSpace(stage.MapStartNodeId) ||
                stage.MapNodeList == null ||
                !stage.MapNodeList.Any(x => x != null && x.NodeId == stage.MapStartNodeId))
            {
                Debug.LogError($"Stage {stageId} has no valid starting map node configured.", this);
                return null;
            }

            return stage.MapStartNodeId;
        }

        public List<EncounterMapNode> GetMapNodes(int stageId)
        {
            var stage = EnemyEncounterList?.FirstOrDefault(x => x.StageId == stageId);
            if (stage == null)
            {
                Debug.LogError($"Stage ID {stageId} is not configured in EncounterData.", this);
                return null;
            }

            if (stage.MapNodeList == null || stage.MapNodeList.Count == 0)
            {
                Debug.LogError($"Stage {stageId} has no map nodes configured.", this);
                return null;
            }

            var nodeIds = new HashSet<string>();
            foreach (var node in stage.MapNodeList)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId) || !nodeIds.Add(node.NodeId))
                {
                    Debug.LogError($"Stage {stageId} has a null, empty, or duplicate map node ID.", this);
                    return null;
                }
            }

            if (!nodeIds.Contains(stage.MapStartNodeId))
            {
                Debug.LogError($"Stage {stageId} starting node '{stage.MapStartNodeId}' is missing.", this);
                return null;
            }

            foreach (var node in stage.MapNodeList)
            {
                if (!Enum.IsDefined(typeof(EncounterMapNodeType), node.NodeType) ||
                    float.IsNaN(node.Position.x) || float.IsInfinity(node.Position.x) || node.Position.x < 0f ||
                    float.IsNaN(node.Position.y) || float.IsInfinity(node.Position.y) ||
                    node.Position.y < 0f || node.Position.y > 1f)
                {
                    Debug.LogError($"Map node '{node.NodeId}' has an invalid type or position.", this);
                    return null;
                }

                switch (node.NodeType)
                {
                    case EncounterMapNodeType.Shop:
                        if (!node.ShopContainerData)
                        {
                            Debug.LogError($"Shop map node '{node.NodeId}' has no ShopContainerData assigned.", this);
                            return null;
                        }
                        break;
                    case EncounterMapNodeType.Encounter:
                    case EncounterMapNodeType.Boss:
                        var encounters = node.NodeType == EncounterMapNodeType.Boss
                            ? stage.BossEncounterList
                            : stage.EnemyEncounterList;
                        if (encounters == null || node.EncounterIndex < 0 || node.EncounterIndex >= encounters.Count)
                        {
                            Debug.LogError($"Map node '{node.NodeId}' references an invalid encounter index for stage {stageId}.", this);
                            return null;
                        }
                        break;
                    case EncounterMapNodeType.Event:
                    case EncounterMapNodeType.Unknown:
                        if (!node.EventData && (stage.EventPool == null || stage.EventPool.Count == 0))
                        {
                            Debug.LogError($"Map node '{node.NodeId}' needs an EventData or a stage event pool.", this);
                            return null;
                        }
                        break;
                    case EncounterMapNodeType.Campfire:
                    case EncounterMapNodeType.Treasure:
                        if (!node.EventData)
                        {
                            Debug.LogError($"Map node '{node.NodeId}' needs an EventData asset to define its choices.", this);
                            return null;
                        }
                        break;
                }

                if (node.NextNodeIds == null)
                    continue;

                foreach (var nextNodeId in node.NextNodeIds)
                {
                    if (!nodeIds.Contains(nextNodeId))
                    {
                        Debug.LogError($"Map node '{node.NodeId}' references missing next node '{nextNodeId}' in stage {stageId}.", this);
                        return null;
                    }
                }
            }

            return stage.MapNodeList;
        }

        public bool TryGetMapNode(int stageId, string nodeId, out EncounterMapNode mapNode)
        {
            mapNode = null;
            var nodes = GetMapNodes(stageId);
            if (nodes == null)
                return false;

            mapNode = nodes.FirstOrDefault(x => x.NodeId == nodeId);
            if (mapNode == null)
                Debug.LogError($"Map node '{nodeId}' is not configured for stage {stageId}.", this);

            return mapNode != null;
        }

        public EventData ResolveEvent(int stageId, EncounterMapNode node)
        {
            if (node == null)
                return null;
            if (node.EventData)
                return node.EventData;

            if (node.NodeType != EncounterMapNodeType.Event && node.NodeType != EncounterMapNodeType.Unknown)
                return null;

            var stage = EnemyEncounterList?.FirstOrDefault(x => x.StageId == stageId);
            var validEvents = stage?.EventPool?.Where(item => item && item.Choices != null && item.Choices.Count > 0).ToList();
            return validEvents != null && validEvents.Count > 0 ? validEvents.RandomItem() : null;
        }

        public EnemyEncounter GetEnemyEncounter(int stageId, EncounterMapNode mapNode)
        {
            if (mapNode == null ||
                (mapNode.NodeType != EncounterMapNodeType.Encounter && mapNode.NodeType != EncounterMapNodeType.Boss))
                return null;

            var stage = EnemyEncounterList?.FirstOrDefault(x => x.StageId == stageId);
            if (stage == null)
                return null;

            var encounters = mapNode.NodeType == EncounterMapNodeType.Boss
                ? stage.BossEncounterList
                : stage.EnemyEncounterList;

            if (mapNode.NodeType == EncounterMapNodeType.Encounter && EncounterRandomlyAtStage)
                return encounters != null && encounters.Count > 0 ? encounters.RandomItem() : null;

            if (encounters == null || mapNode.EncounterIndex < 0 || mapNode.EncounterIndex >= encounters.Count)
            {
                Debug.LogError($"Map node '{mapNode.NodeId}' references an invalid encounter index for stage {stageId}.", this);
                return null;
            }

            return encounters[mapNode.EncounterIndex];
        }
    }

    [Serializable]
    public class EnemyEncounterStage
    {
        [SerializeField] private string name;
        [SerializeField] private int stageId;
        [SerializeField] private List<EnemyEncounter> bossEncounterList;
        [SerializeField] private List<EnemyEncounter> enemyEncounterList;
        [SerializeField] private string mapStartNodeId;
        [SerializeField] private List<EncounterMapNode> mapNodeList;
        [SerializeField] private List<EventData> eventPool;

        public string Name => name;
        public int StageId => stageId;
        public List<EnemyEncounter> BossEncounterList => bossEncounterList;
        public List<EnemyEncounter> EnemyEncounterList => enemyEncounterList;
        public string MapStartNodeId => mapStartNodeId;
        public List<EncounterMapNode> MapNodeList => mapNodeList;
        public List<EventData> EventPool => eventPool;
    }

    [Serializable]
    public class EncounterMapNode
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string displayName;
        [SerializeField] private EncounterMapNodeType nodeType;
        [SerializeField] private bool isFinal;
        [SerializeField] private ShopContainerData shopContainerData;
        [SerializeField] private EventData eventData;
        [SerializeField] private int encounterIndex;
        [SerializeField] private int roomNumber;
        [SerializeField] private Vector2 position;
        [SerializeField] private List<string> nextNodeIds;

        public string NodeId => nodeId;
        public string DisplayName => displayName;
        public EncounterMapNodeType NodeType => nodeType;
        public bool IsFinal => isFinal;
        public ShopContainerData ShopContainerData => shopContainerData;
        public EventData EventData => eventData;
        public int EncounterIndex => encounterIndex;
        public int RoomNumber => roomNumber;
        public Vector2 Position => position;
        public List<string> NextNodeIds => nextNodeIds;
    }

    [Serializable]
    public class EnemyEncounter : EncounterBase
    {
        [SerializeField] private List<EnemyCharacterData> enemyList;
        public List<EnemyCharacterData> EnemyList => enemyList;
    }

    [Serializable]
    public abstract class EncounterBase
    {
        [SerializeField] private BackgroundTypes targetBackgroundType;
        public BackgroundTypes TargetBackgroundType => targetBackgroundType;
    }
}
