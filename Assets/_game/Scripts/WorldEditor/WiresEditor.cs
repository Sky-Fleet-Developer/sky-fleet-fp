using System.Collections.Generic;
using System.Linq;
using Core.Graph;
using Core.Graph.Wires;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Serialization;
using UnityEditor;
using UnityEngine;
using Utilities = Core.Graph.Wires.Utilities;

#if UNITY_EDITOR
namespace WorldEditor
{
    public class WiresEditor : EditorWindow
    {
        public static WiresEditor CurrentEditor;

        [MenuItem("Factory/Wires Editor")]
        public static void OpenWindow()
        {
            CurrentEditor = GetWindow<WiresEditor>();
        }

        private IGraph selectedGraph;
        private IGraph currentGraph;
        private Transform currentGraphTransform;
        private StructConfigHolder configHolder;

        [SerializeField] private List<IGraphNode> nodes;

        //[SerializeField] private StructureConfiguration configuration = null;
        private List<IPortsContainer> portsDescriptions;
        private List<IPortsContainer> allContainers;
        private List<IPortsContainer> containersWithPorts;
        private Dictionary<IPortsContainer, bool> expandGroups;
        private List<IPortsContainer> selectedPorts;
        private List<List<IPortsContainer>> wires;

        private bool GetExpand(IPortsContainer container)
        {
            if (expandGroups.TryGetValue(container, out bool val)) return val;

            expandGroups.Add(container, false);
            return false;
        }

        private void SetExpand(IPortsContainer container, bool expand)
        {
            expandGroups[container] = expand;
        }


        private bool configDirty;

        private Vector2 portsScroll;
        private Vector2 wireScroll;

        private GUISkin MainSkin;

        public void OnEnable()
        {
            Selection.selectionChanged += GetFomSelection;
            GetFomSelection();
            MainSkin = Resources.Load<GUISkin>("WiresEditorSkin");
        }

        public void OnDisable()
        {
            Selection.selectionChanged -= GetFomSelection;
        }

        private void OnDestroy()
        {
            SaveAndDestroyDialog();
        }

        private void SaveAndDestroyDialog()
        {
            if (currentGraph == null || !configDirty) return;
            if (EditorUtility.DisplayDialog("", "Save wires?", "Yes", "No"))
            {
                WriteConfig();
            }
        }

        public void GetFomSelection()
        {
            if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent(out IGraph structure))
            {
                Transform parent = Selection.activeTransform.parent;
                if (parent)
                {
                    configHolder = parent.GetComponent<StructConfigHolder>();
                }
                else
                {
                    if (EditorUtility.DisplayDialog("Instanced structure has no config holder!", "Instance one?", "Yes",
                        "No"))
                    {
                        configHolder = StructConfigHolder.CreateForStructure(Selection.activeGameObject);
                    }
                    else
                    {
                        return;
                    }
                }

                currentGraphTransform = Selection.activeTransform;
                selectedGraph = structure;
            }
        }

        private void OnGUI()
        {
            if (selectedGraph == null) return;

            GUI.skin = MainSkin;

            if (currentGraph == null)
            {
                CreateButton();
            }
            else
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height * 0.7f));
                portsScroll = GUILayout.BeginScrollView(portsScroll);
                DrawStructureBlocks();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                GUILayout.BeginArea(new Rect(0, Screen.height * 0.7f, Screen.width, Screen.height * 0.3f));
                wireScroll = GUILayout.BeginScrollView(wireScroll);
                DrawSelectedWire();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void CreateArrays()
        {
            containersWithPorts = new List<IPortsContainer>();
            wires = new List<List<IPortsContainer>>();
            portsDescriptions = new List<IPortsContainer>();
            expandGroups = new Dictionary<IPortsContainer, bool>();
            allContainers = new List<IPortsContainer>();
            selectedPorts = new List<IPortsContainer>();
        }

        private void CreateButton()
        {
            GUILayout.Space(20);
            EditorGUILayout.ObjectField(currentGraphTransform, typeof(Transform), true);

            if (GUILayout.Button("Edit configuration", GUILayout.Width(200)))
            {
                currentGraph = selectedGraph;

                currentGraph.InitGraph();

                nodes = currentGraph.Nodes.ToList();

                CreateArrays();

                foreach (IGraphNode node in nodes)
                {
                    Utilities.GetPortsDescriptions(node, ref portsDescriptions);
                }

                foreach (IPortsContainer portsContainer in portsDescriptions)
                {
                    CollectContainers(portsContainer, allContainers);
                }

                foreach (IPortsContainer portsContainer in allContainers)
                {
                    if (portsContainer.HasNestedValues == false) containersWithPorts.Add(portsContainer);
                }


                foreach (WireConfiguration configWire in configHolder.graphConfiguration.wires)
                {
                    List<IPortsContainer> wire = new List<IPortsContainer>();
                    foreach (string portId in configWire.ports)
                    {
                        var port = currentGraph.GetPort(portId);
                        var container = FindContainerForPort(port);
                        wire.Add(container);
                    }

                    wires.Add(wire);
                }
            }
        }

        private void CollectContainers(IPortsContainer current, List<IPortsContainer> result)
        {
            result.Add(current);

            if (current.HasNestedValues)
            {
                foreach (IPortsContainer portsContainer in current.GetNestedValues())
                {
                    CollectContainers(portsContainer, result);
                }
            }
        }

        private void DrawSelectedWire()
        {
            GUI.backgroundColor = Color.gray;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "Selected wire");
            GUILayout.Space(25);

            for (int index = 0; index < selectedPorts.Count; index++)
            {
                IPortsContainer portsContainer = selectedPorts[index];
                GUI.backgroundColor = portsContainer.GetColor();
                if (GUILayout.Button(portsContainer.GetDescription()))
                {
                    selectedPorts.Remove(portsContainer);
                    index--;
                }
            }

            GUI.backgroundColor = Color.white;
            if (selectedPorts.Count <= 1) return;

            GUILayout.Space(25);
            GUI.backgroundColor = Color.green * 0.6f;
            if (GUILayout.Button("Write wire"))
            {
                if (!selectionIsWire)
                {
                    wires.Add(selectedPorts);
                }

                selectedPorts = new List<IPortsContainer>();
                selectionIsWire = false;
                configDirty = true;
            }

            GUILayout.Space(8);
            GUI.backgroundColor = Color.red * 0.6f;
            if (GUILayout.Button("Clear wire"))
            {
                selectedPorts = new List<IPortsContainer>();
                selectionIsWire = false;
            }
        }

        private float verticalOffset;
        private int depth;

        private void DrawStructureBlocks()
        {
            GUILayout.Box(currentGraph.GetType().Name);
            GUILayout.Space(10);
            for (int i = 0; i < portsDescriptions.Count; i++)
            {
                verticalOffset = 0;
                depth = 0;
                DrawContainer(portsDescriptions[i]);
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Write configuration"))
            {
                WriteConfig();
            }
        }

        private void WriteConfig()
        {
            MonoBehaviour monobeh = currentGraphTransform.gameObject.GetComponent<IStructure>() as MonoBehaviour;
            Undo.RecordObject(monobeh, "Config");
            foreach (List<IPortsContainer> wire in wires)
            {
                configHolder.graphConfiguration.wires.Add(new WireConfiguration(wire.Select(x => x.GetPort().Id).ToList()));
            }
            EditorUtility.SetDirty(monobeh);
            configDirty = false;
        }

        private void DrawContainer(IPortsContainer container)
        {
            GUI.backgroundColor = container.GetColor();
            GUI.color = Color.white;
            if (container.HasNestedValues == false)
            {
                DrawPort(container, GetPortDrawOptions(container));
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(verticalOffset);
            bool expand = GetExpand(container);

            string descr = container.GetDescription();

            if (expand)
            {
                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard,
                    MainSkin.verticalScrollbarUpButton, GUILayout.Width(18)) || GUILayout.Button(descr))
                {
                    SetExpand(container, false);
                    expand = false;
                    if (Event.current.alt) SetExpandAll(false);
                }
            }
            else
            {
                if (EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard,
                    MainSkin.verticalScrollbarDownButton, GUILayout.Width(18)) || GUILayout.Button(descr))
                {
                    SetExpand(container, true);
                    if (Event.current.alt) SetExpandAll(true);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (expand)
            {
                float v = verticalOffset;
                int d = depth;
                depth++;
                verticalOffset += 35;
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

        private void DrawPort(IPortsContainer container, PortDrawOptions options)
        {
            GUILayout.BeginHorizontal();

            float space = verticalOffset;

            if (options.connected)
            {
                space -= 25;
            }

            if (options.selected)
            {
                space -= 25;
            }

            if (options.canConnect)
            {
                space -= 15;
            }

            GUILayout.Space(verticalOffset + space);
            if (options.connected)
            {
                GUILayout.Label("--", GUILayout.Width(25));
            }

            if (options.selected)
            {
                GUILayout.Label("-->", GUILayout.Width(25));
            }

            if (options.canConnect)
            {
                GUILayout.Label(">", GUILayout.Width(15));
            }

            if (GUILayout.Button(container.GetDescription()))
            {
                if (options.selected)
                {
                    selectedPorts.Remove(container);
                }
                else
                {
                    OnPortSelected(container, options);
                }
            }

            GUILayout.EndHorizontal();
        }

        private struct PortDrawOptions
        {
            public bool selected;
            public bool canConnect;
            public bool connected;
            public bool isFirst;
        }

        private PortDrawOptions GetPortDrawOptions(IPortsContainer container)
        {
            bool connected = false;
            foreach (List<IPortsContainer> wire in wires)
            {
                connected |= wire.Contains(container);
            }

            return new PortDrawOptions()
            {
                selected = selectedPorts.Contains(container),
                connected = connected,
                canConnect = CanConnect(container),
                isFirst = selectedPorts.Count == 0
            };
        }

        private bool selectionIsWire;

        private void OnPortSelected(IPortsContainer port, PortDrawOptions options)
        {
            List<IPortsContainer> wire = GetWireForPort(port);

            if (wire == null)
            {
                if (options.canConnect)
                {
                    configDirty = true;
                    AddSelectedPort(port);
                }
            }
            else
            {
                if (!wire[0].GetPort().Port.CanConnect(port.GetPort().Port)) return;

                configDirty = true;
                if (!options.isFirst)
                {
                    if (selectionIsWire)
                    {
                        wires.Remove(wire);
                    }
                    else
                    {
                        var s = selectedPorts;
                        selectedPorts = wire;
                        selectedPorts.AddRange(s);
                    }

                    foreach (IPortsContainer portsContainer in wire)
                    {
                        AddSelectedPort(portsContainer);
                    }
                }
                else
                {
                    selectedPorts = wire;
                }

                selectionIsWire = true;
            }
        }

        private void AddSelectedPort(IPortsContainer port)
        {
            if (selectedPorts.Contains(port)) return;
            selectedPorts.Add(port);
        }

        private bool CanConnect(IPortsContainer port)
        {
            int selectedCount = selectedPorts.Count;
            return selectedCount > 0 && selectedPorts[0].GetPort().Port.CanConnect(port.GetPort().Port) ||
                   selectedCount == 0;
        }

        private IPortsContainer FindContainerForPort(PortPointer port)
        {
            List<IPortsContainer> result = new List<IPortsContainer>();

            foreach (IPortsContainer portsContainer in containersWithPorts)
            {
                if (portsContainer.GetPort().Id == port.Id) return portsContainer;
            }

            return null;
        }

        private List<IPortsContainer> GetWireForPort(IPortsContainer port)
        {
            foreach (List<IPortsContainer> wire in wires)
            {
                if (wire.Contains(port)) return wire;
            }

            return null;
        }
    }
}
#endif