using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

namespace Structure.Editor
{
    public class WiresEditor : EditorWindow
    {
        public static WiresEditor current;

        [MenuItem("Factory/Wires Editor")]
        public static void OpenWindow()
        {
            current = GetWindow<WiresEditor>();
        }

        [SerializeField] private IStructure selectedStructure;
        [SerializeField] private StructureConfiguration configuration = null;
        [SerializeField] private List<(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)> portsList;
        private List<TakePort> takePorts = new List<TakePort>();
        private string json;
        private bool configDirty = true;

        private class TakePort
        {
            public Port port;
            public string name;
            public TakePort(Port port, string name)
            {
                this.port = port;
                this.name = name;
            }
        }

        public void OnEnable()
        {
            Selection.selectionChanged += GetFomSelection;
            GetFomSelection();
        }

        public void OnDisable()
        {
            Selection.selectionChanged -= GetFomSelection;
        }

        private void GetFomSelection()
        {
            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent(out IStructure structure))
            {
                selectedStructure = structure;
            }
        }

        private void OnGUI()
        {
            if (selectedStructure == null) return;

            switch (Event.current.type)
            {
                default://case EventType.Repaint:
                    if (configuration == null)
                    {
                        CreateButton();
                    }
                    else
                    {
                        CreationMenu();
                    }

                    break;
            }
        }

        private void CreateButton()
        {
            GUILayout.Space(20);
            EditorGUILayout.ObjectField(selectedStructure.transform, typeof(Transform), true);

            if (GUILayout.Button("Create configuration", GUILayout.Width(200)))
            {
                CreateConfiguration();
            }
        }

        private void CreateConfiguration()
        {
            if (selectedStructure.Blocks == null) selectedStructure.RefreshBlocksAndParents();
            portsList = new List<(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)>(selectedStructure.Blocks.Count);

            configuration = new StructureConfiguration
            {
                blocks = new List<BlockConfiguration>(selectedStructure.Blocks.Count),
                wires = new List<string>()
            };

            for (int i = 0; i < selectedStructure.Blocks.Count; i++)
            {
                var block = selectedStructure.Blocks[i];

                var ports = new List<Port>();
                Factory.GetPorts(block, ref ports);

                var specialPorts = new List<Port>();
                Factory.GetSpecialPorts(block, ref specialPorts);

                var infos = Factory.GetPortsInfo(block);

                portsList.Add((block, ports, infos, specialPorts));

                configuration.blocks.Add(Factory.GetConfiguration(block));
            }
        }

        private void CreationMenu()
        {
            foreach (var port in portsList)
            {
                DisplayBlockPorts(port.block, port.ports, port.infos, port.specialPorts);
            }

            CreateWireMenu();

            ShowJson();
        }

        private void ShowJson()
        {
            if (configDirty)
            {
                json = JsonConvert.SerializeObject(configuration);
                configDirty = false;
            }
            GUILayout.TextArea(json, GUILayout.Height(300));
        }

        private void CreateWireMenu()
        {
            for (int i = 0; i < takePorts.Count; i++)
            {
                if(GUILayout.Button(takePorts[i].name))
                {
                    takePorts.Remove(takePorts[i]);
                }
            }

            if (GUILayout.Button("Create wire"))
            {
                configuration.wires.Add(Factory.GetWireString(takePorts.Select(x => { return x.port.Guid; }).ToList()));
                takePorts.Clear(); 
                configDirty = true;
            }
        }

        private void DisplayBlockPorts(IBlock block, List<Port> ports, FieldInfo[] infos, List<Port> specialPorts)
        {
            GUILayout.Label(block.transform.name);
            for (int i = 0; i < ports.Count; i++)
            {
                DisplayPort(ports[i], infos[i]);
            }
            if (specialPorts.Count > 0)
            {
                GUILayout.Label("Special ports:");
                for (int i = 0; i < specialPorts.Count; i++)
                {
                    DisplayPort(specialPorts[i], null);
                }
            }
            GUILayout.Space(10);

        }

        private void DisplayPort(Port port, FieldInfo info)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            if (info != null)
            {
                GUILayout.Label(info.Name, GUILayout.Width(110));
                GUILayout.Space(10);
            }
            else
            {
                GUILayout.Space(60);
            }
            string nameButton;
            if (info != null)
            {
                nameButton = info.Name + ": " + port.ValueType.ToString();
            }
            else
            {
                nameButton = port.ValueType.ToString();
            }
            if (GUILayout.Button(nameButton))
            {
                if (takePorts.Find(item => item.port == port) == null)
                {
                    if (takePorts.Count > 0 && takePorts[0].port.ValueType == port.ValueType)
                    {
                        takePorts.Add(new TakePort(port, nameButton));
                    }
                    else if (takePorts.Count == 0)
                    {
                        takePorts.Add(new TakePort(port, nameButton));
                    }
                }
            }

            GUILayout.EndHorizontal();

        }

    }
}
