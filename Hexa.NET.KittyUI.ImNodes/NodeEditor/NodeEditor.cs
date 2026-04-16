namespace Hexa.NET.KittyUI.UI.NodeEditor
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImNodes;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public unsafe class NodeEditor
    {
        private string? state;
        private ImNodesEditorContextPtr context;

        private readonly List<Node> nodes = [];
        private readonly List<Link> links = [];
        private int idState;

        public NodeEditor()
        {
        }

        public event EventHandler<Node>? NodeAdded;

        public event EventHandler<Node>? NodeRemoved;

        public event EventHandler<Link>? LinkAdded;

        public event EventHandler<Link>? LinkRemoved;

        [JsonProperty(Order = 0)]
        public List<Node> Nodes => nodes;

        [JsonProperty(Order = 2)]
        public List<Link> Links => links;

        public int IdState { get => idState; set => idState = value; }

        public string State { get => SaveState(); set => RestoreState(value); }

        public virtual void Initialize()
        {
            if (context.Handle == null)
            {
                context = ImNodes.EditorContextCreate();

                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].InitializeInternal(this);
                }
                for (int i = 0; i < links.Count; i++)
                {
                    links[i].Initialize(this);
                }
            }
        }

        public int GetUniqueId()
        {
            return idState++;
        }

        public Node GetNode(int id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node.Id == id)
                    return node;
            }
            throw new();
        }

        public T GetNode<T>() where T : Node
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is T t)
                    return t;
            }
            throw new KeyNotFoundException();
        }

        public Link GetLink(int id)
        {
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (link.Id == id)
                    return link;
            }

            throw new KeyNotFoundException();
        }

        public Node CreateNode(string name, bool removable = true, bool isStatic = false)
        {
            Node node = new(GetUniqueId(), name, removable, isStatic);
            AddNode(node);
            return node;
        }

        public void AddNode(Node node)
        {
            if (context.Handle != null)
                node.InitializeInternal(this);
            nodes.Add(node);
            NodeAdded?.Invoke(this, node);
        }

        public void RemoveNode(Node node)
        {
            nodes.Remove(node);
            NodeRemoved?.Invoke(this, node);
        }

        public void AddLink(Link link)
        {
            if (context.Handle != null)
                link.Initialize(this);
            links.Add(link);
            LinkAdded?.Invoke(this, link);
        }

        public void RemoveLink(Link link)
        {
            links.Remove(link);
            LinkRemoved?.Invoke(this, link);
        }

        public Link CreateLink(Pin input, Pin output)
        {
            Link link = new(GetUniqueId(), output.Parent, output, input.Parent, input);
            AddLink(link);
            return link;
        }

        public string SaveState()
        {
            return Marshal.PtrToStringAnsi((nint)ImNodes.SaveEditorStateToIniString(context)) ?? string.Empty;
        }

        public void RestoreState(string state)
        {
            if (context.Handle == null)
            {
                this.state = state;
                return;
            }
            ImNodes.LoadEditorStateFromIniString(context, state, (uint)state.Length);
        }

        public void Draw()
        {
            ImNodes.EditorContextSet(context);
            ImNodes.BeginNodeEditor();
            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Draw();
            }
            for (int i = 0; i < Links.Count; i++)
            {
                Links[i].Draw();
            }

            ImNodes.EndNodeEditor();

            int idNode1 = 0;
            int idNode2 = 0;
            int idpin1 = 0;
            int idpin2 = 0;
            if (ImNodes.IsLinkCreated(ref idNode1, ref idpin1, ref idNode2, ref idpin2))
            {
                var pino = GetNode(idNode1).GetOutput(idpin1);
                var pini = GetNode(idNode2).GetInput(idpin2);
                if (pini.CanCreateLink(pino) && pino.CanCreateLink(pini))
                    CreateLink(pini, pino);
            }
            int idLink = 0;
            if (ImNodes.IsLinkDestroyed(ref idLink))
            {
                GetLink(idLink).Destroy();
            }
            if (ImGui.IsKeyPressed(ImGuiKey.Delete))
            {
                int numLinks = ImNodes.NumSelectedLinks();
                if (numLinks != 0)
                {
                    int[] links = new int[numLinks];
                    ImNodes.GetSelectedLinks(ref links[0]);
                    for (int i = 0; i < links.Length; i++)
                    {
                        GetLink(links[i]).Destroy();
                    }
                }
                int numNodes = ImNodes.NumSelectedNodes();
                if (numNodes != 0)
                {
                    int[] nodes = new int[numNodes];
                    ImNodes.GetSelectedNodes(ref nodes[0]);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        var node = GetNode(nodes[i]);
                        if (node.Removable)
                        {
                            node.Destroy();
                        }
                    }
                }
            }
            int idpinStart = 0;
            if (ImNodes.IsLinkStarted(ref idpinStart))
            {
            }

            for (int i = 0; i < Nodes.Count; i++)
            {
                var id = Nodes[i].Id;
                Nodes[i].IsHovered = ImNodes.IsNodeHovered(ref id);
            }

            ImNodes.EditorContextSet(null);

            if (state != null)
            {
                RestoreState(state);
                state = null;
            }
        }

        public void Destroy()
        {
            var nodes = this.nodes.ToArray();
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].Destroy();
            }
            this.nodes.Clear();
            ImNodes.EditorContextFree(context);
            context = null;
        }
    }
}