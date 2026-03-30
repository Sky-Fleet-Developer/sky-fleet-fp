using System;
using System.Collections.Generic;
using System.Linq;
using Core.Configurations.GoogleSheets;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Ai
{
    public class RelationsRawData
    {
        public string[] IdList;	
        public string[] Positive;	
        public string[] Ally;	
        public string[] Negative;	
        public string[] Enemy;	
    }

    public enum RelationType
    {
        Enemy = 0,
        Negative = 1,
        Neutral = 2,
        Positive = 3,
        Ally = 4,
    }

    [Serializable]
    public class RelationData
    {
        public string signId;
        public RelationType relation;
    }
    
    [Serializable]
    public class RelationsData
    {
        [SerializeField] private string signId;
        [SerializeField] private List<RelationData> relations;
        private Dictionary<string, RelationType> _relations;
        public string SignId => signId;

        public RelationsData(string signId, List<RelationData> relations)
        {
            this.signId = signId;
            this.relations = relations;
        }

        public RelationType GetRelation(string otherSignId)
        {
            _relations ??= relations.ToDictionary(x => x.signId, x => x.relation);

            return _relations.GetValueOrDefault(otherSignId, RelationType.Neutral);
        }
    }
    
    [CreateAssetMenu(menuName = "SF/Game/TableRelations", fileName = "TableRelations")]
    public class TableRelations : Table<RelationsRawData>
    {
        public override string TableName => "Relations";
        
        [SerializeField] private RelationsData[] data;
        private Dictionary<string, RelationsData> _data;

        protected override RelationsRawData[] Data
        {
            set
            {
                Dictionary<string, List<(List<RelationData> list, int priority)>> dataDic = new ();
                for (int i = 0; i < value.Length; i++)
                {
                    RelationsRawData rawData = value[i];
                    List<RelationData> relations = new ();
                    ParseRelations(rawData.Positive, relations, RelationType.Positive);
                    ParseRelations(rawData.Ally, relations, RelationType.Ally);
                    ParseRelations(rawData.Negative, relations, RelationType.Negative);
                    ParseRelations(rawData.Enemy, relations, RelationType.Enemy);
                    
                    for (var j = 0; j < rawData.IdList.Length; j++)
                    {
                        var list = dataDic.GetValueOrDefault(rawData.IdList[j], new());
                        list.Add((relations, i));
                        dataDic[rawData.IdList[j]] = list;
                    }
                }
                
                List<RelationsData> relationsList = new();
                // sort by priority and remove duplicates
                foreach ((string mySign, IOrderedEnumerable<(List<RelationData> list, int priority)> myRelations) relationsData in dataDic.Select(x => (x.Key, x.Value.OrderByDescending(y => y.priority))))
                {
                    Dictionary<string, RelationType> priorities = new();
                    foreach ((List<RelationData> list, int priority) in relationsData.myRelations)
                    {
                        foreach (var relationData in list)
                        {
                            priorities[relationData.signId] = relationData.relation;
                        }
                    }
                    List<RelationData> resultRelations = priorities.Select(x => new RelationData { signId = x.Key, relation = x.Value }).ToList();
                    relationsList.Add(new RelationsData(relationsData.mySign, resultRelations));
                }
                
                data = relationsList.ToArray();
                void ParseRelations(string[] relations, List<RelationData> output, RelationType relationType)
                {
                    if (relations == null || relations.Length == 0)
                    {
                        return;
                    }

                    for (var i = 0; i < relations.Length; i++)
                    {
                        output.Add(new RelationData { signId = relations[i], relation = relationType });
                    }
                }
            }
        }

        public RelationType GetRelation(string mySign, string otherSign)
        {
            _data ??= data.ToDictionary(x => x.SignId, x => x);
            
            return _data[mySign].GetRelation(otherSign);
        }

        public IEnumerable<string> GetAllRegisteredSignatures()
        {
            return data.Select(x => x.SignId);
        }
    }
}