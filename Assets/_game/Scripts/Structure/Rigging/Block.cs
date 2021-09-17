using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Structure.Rigging
{
    public abstract class Block : MonoBehaviour, IBlock
    {
        public Vector3 localPosition => transform.position;
        public Parent Parent { get; private set; }
        public IStructure Structure { get; private set; }
        [ShowInInspector] public string Guid
        {
            get
            {
                if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
                return guid;
            }
            set => guid = value;
        }
        [SerializeField, HideInInspector] private string guid;

        [ShowInInspector]
        public string MountingType
        {
            get => mountingType;
            set => mountingType = value;
        }
        [SerializeField, HideInInspector] private string mountingType;

        public virtual void InitBlock(IStructure structure, Parent parent)
        {
            Parent = parent;
            Structure = structure;
        }

        public virtual void OnInitComplete() { }

        private void Start()
        {
        }
    }
}
