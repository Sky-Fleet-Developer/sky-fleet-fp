using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Wires;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using Utilities = Core.Structure.Wires.Utilities;

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

        private IStructure selectedStructure;
        private IStructure currentStructure;
        [SerializeField] private List<IBlock> blocks;
        //[SerializeField] private StructureConfiguration configuration = null;
        private List<IPortsContainer> ports;
        private Dictionary<IPortsContainer, bool> expandGroups;
        private bool GetExpand(IPortsContainer container)
        {
            if (expandGroups.TryGetValue(container, out bool val)) return val;
            
            expandGroups.Add(container,false);
            return false;
        }

        private void SetExpand(IPortsContainer container, bool expand)
        {
            expandGroups[container] = expand;
        }
  

        private string json;
        private bool configDirty = true;

        private Vector2 cashScrollPositionMenuPorts;
        private Vector2 cashScrollPositionCreateWire;

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

            cashScrollPositionMenuPorts = GUILayout.BeginScrollView(cashScrollPositionMenuPorts);
            switch (Event.current.type)
            {
                case EventType.Layout:
                case EventType.Repaint:
                case EventType.MouseDown:
                case EventType.MouseMove:
                case EventType.MouseUp:
                case EventType.KeyDown:
                case EventType.KeyUp:
                    if (currentStructure == null)
                    {
                        CreateButton();
                    }
                    else
                    {
                        DrawStructureBlocks();
                    }

                    break;
            }
            GUILayout.EndScrollView();
        }


        //New
        private void CreateButton()
        {
            GUILayout.Space(20);
            EditorGUILayout.ObjectField(selectedStructure.transform, typeof(Transform), true);

            if (GUILayout.Button("Edit configuration", GUILayout.Width(200)))
            {
                currentStructure = selectedStructure;
                
                if (currentStructure.Blocks == null) currentStructure.RefreshBlocksAndParents();

                blocks = currentStructure.Blocks;
                ports = new List<IPortsContainer>();
                expandGroups = new Dictionary<IPortsContainer, bool>();
                foreach (IBlock block in blocks)
                {
                    Utilities.GetPortsDescriptions(block, ref ports);
                }
            }
        }


        
        private float verticalOffset;
        private int depth;
        private void DrawStructureBlocks()
        {
            GUILayout.Box(currentStructure.GetType().Name);
            GUILayout.Space(10);
            for (int i = 0; i < ports.Count; i++)
            {
                verticalOffset = 0;
                depth = 0;
                DrawContainer(ports[i]);
            }
        }

        private void DrawContainer(IPortsContainer container)
        {
            GUI.backgroundColor = container.GetColor();
            GUI.color = Color.white;
            if (container.HasNestedValues == false)
            {
                DrawPort(container);
                return;
            }
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(verticalOffset);
            bool expand = GetExpand(container);

            if (expand)
            {
                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, GUILayout.Width(18)))
                {
                    SetExpand(container, false);
                    expand = false;
                    if(Event.current.alt) SetExpandAll(false);
                }
            }
            else
            {
                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, GUILayout.Width(18)))
                {
                    SetExpand(container, true);
                    if(Event.current.alt) SetExpandAll(true);
                }
            }
            GUILayout.Box(container.GetDescription());
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (expand)
            {
                float v = verticalOffset;
                int d = depth;
                depth++;
                verticalOffset += 10;
                if (depth > 4) return;
                foreach (IPortsContainer portsContainer in container.GetNestedValues())
                {
                    DrawContainer(portsContainer);
                }
                depth = d;
                verticalOffset = v;
            }
        }

        private void SetExpandAll(bool value)
        {
            
        }

        private void DrawPort(IPortsContainer container)
        {
            Port port = container.GetPort();
            GUILayout.BeginHorizontal();
            GUILayout.Space(verticalOffset);
            if (GUILayout.Button(container.GetDescription()))
            {
                OnPortSelected(port);
            }
            GUILayout.EndHorizontal();
        }

        private void OnPortSelected(Port port)
        {
            
        }

        //Old
        /*private void CreateConfiguration()
        {
            try
            {
                configuration = (StructureConfiguration)JsonConvert.DeserializeObject(selectedStructure.Configuration);
            }
            catch
            {
                if (selectedStructure.Blocks == null) selectedStructure.RefreshBlocksAndParents();
                portsList = new List<(IBlock block, List<PortPointer> ports, FieldInfo[] infos, List<PortPointer> specialPorts)>(selectedStructure.Blocks.Count);

                configuration = new StructureConfiguration
                {
                    blocks = new List<BlockConfiguration>(selectedStructure.Blocks.Count),
                    wires = new List<string>()
                };

                for (int i = 0; i < selectedStructure.Blocks.Count; i++)
                {
                    IBlock block = selectedStructure.Blocks[i];

                    List<PortPointer> ports = new List<PortPointer>();
                    Factory.GetPorts(block, ref ports);

                    List<PortPointer> specialPorts = new List<PortPointer>();
                    Factory.GetSpecialPorts(block, ref specialPorts);

                    FieldInfo[] infos = Factory.GetPortsInfo(block);

                    portsList.Add((block, ports, infos, specialPorts));

                    configuration.blocks.Add(Factory.GetConfiguration(block));
                }
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
                if (GUILayout.Button(takePorts[i].name))
                {
                    takePorts.Remove(takePorts[i]);
                }
            }

            if (GUILayout.Button("Create wire"))
            {
                configuration.wires.Add(Factory.GetWireString(takePorts.Select(x => { return x.port.Id; }).ToList()));
                takePorts.Clear();
                configDirty = true;
            }
        }

        private void DisplayBlockPorts(IBlock block, List<PortPointer> ports, FieldInfo[] infos, List<PortPointer> specialPorts)
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

        private void DisplayPort(PortPointer port, FieldInfo info)
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
                nameButton = info.Name + ": " + port.Port.ToString();
            }
            else
            {
                nameButton = port.Port.ToString();
            }
            if (GUILayout.Button(nameButton))
            {
                if (takePorts.Find(item => item.port.Equals(port)) == null)
                {
                    if (takePorts.Count > 0 && takePorts[0].port.Port.CanConnect(port.Port))
                    {
                        takePorts.Add(new PortInfo(port, nameButton));
                    }
                    else if (takePorts.Count == 0)
                    {
                        takePorts.Add(new PortInfo(port, nameButton));
                    }
                }
            }

            GUILayout.EndHorizontal();

        }*/

    }
}
