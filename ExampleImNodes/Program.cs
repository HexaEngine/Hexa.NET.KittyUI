// See https://aka.ms/new-console-template for more information
using ExampleImNodes;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;
using Hexa.NET.KittyUI.ImNodes;
using Hexa.NET.KittyUI.UI.NodeEditor;
using Hexa.NET.Utilities.Text;

AppBuilder.Create()
    .EnableLogging(true)
    .EnableDebugTools(true)
    .EnableImNodes()
    .SetTitle("Test App")
    .AddWindow<MainWindow>()
    .StyleColorsDark()
    .Run();

namespace ExampleImNodes
{
    public class GameState
    {
        public double Money;

        public static GameState? Global { get; } = new();
    }

    public interface IResourceInputNode
    {
        public void PutResource(GameState state, Pin pinIn, double amount);
    }

    public abstract class GameNode : Node
    {
        protected GameNode(int id, string name, bool removable, bool isStatic) : base(id, name, removable, isStatic)
        {
        }

        public abstract void Tick(GameState state);
    }

    public class InputNode : GameNode
    {
        private Pin pinOut = null!;

        public InputNode(int id) : base(id, "Input", true, false)
        {
        }

        protected override void Initialize(NodeEditor editor)
        {
            pinOut = AddOrGetPin(new Pin(editor.GetUniqueId(), "Out", Hexa.NET.ImNodes.ImNodesPinShape.Circle, PinKind.Output, PinType.Float));
        }

        public override void Tick(GameState state)
        {
            foreach (var link in pinOut.Links)
            {
                if (link.InputNode is IResourceInputNode inputNode)
                {
                    inputNode.PutResource(state, link.Input, 1.0);
                }
            }
        }
    }

    public class ProcessNode : GameNode, IResourceInputNode
    {
        private double storedAmount = 0.0;
        private Pin pinOut = null!;

        public ProcessNode(int id) : base(id, "Process", false, false)
        {
        }

        protected override void Initialize(NodeEditor editor)
        {
            CreateOrGetPin(editor, "In", PinKind.Input, PinType.Float, Hexa.NET.ImNodes.ImNodesPinShape.Circle);
            pinOut = CreateOrGetPin(editor, "Out", PinKind.Output, PinType.Float, Hexa.NET.ImNodes.ImNodesPinShape.Circle);
        }

        public void PutResource(GameState state, Pin pinIn, double amount)
        {
            storedAmount += amount;
        }

        public override void Tick(GameState state)
        {
            var split = pinOut.Links.Count;
            var amountPerLink = split > 0 ? storedAmount / split : 0.0;
            foreach (var link in pinOut.Links)
            {
                if (link.InputNode is IResourceInputNode inputNode)
                {
                    inputNode.PutResource(state, link.Input, amountPerLink);
                }
            }
            if (split > 0)
            {
                storedAmount = 0.0;
            }
        }
    }

    public class OutputNode : GameNode, IResourceInputNode
    {
        public OutputNode(int id) : base(id, "Output", true, false)
        {
        }

        protected override void Initialize(NodeEditor editor)
        {
            AddOrGetPin(new Pin(editor.GetUniqueId(), "In", Hexa.NET.ImNodes.ImNodesPinShape.Circle, PinKind.Input, PinType.Float));
        }

        public void PutResource(GameState state, Pin pinIn, double amount)
        {
            state.Money += amount;
        }

        public override void Tick(GameState state)
        {
        }
    }

    public class MainWindow : ImWindow
    {
        private NodeEditor editor = new();

        public MainWindow()
        {
            IsEmbedded = true;
            editor.AddNode(new InputNode(editor.GetUniqueId()));
            editor.AddNode(new ProcessNode(editor.GetUniqueId()));
            editor.AddNode(new OutputNode(editor.GetUniqueId()));
            editor.Initialize();
        }

        public override string Name { get; } = "Idle Game";

        public override unsafe void DrawContent()
        {
            byte* buffer = stackalloc byte[2048];
            StrBuilder sb = new(buffer, 2048);

            var state = GameState.Global;
            if (state == null) return;

            foreach (var node in editor.Nodes)
            {
                if (node is GameNode gameNode)
                {
                    gameNode.Tick(state);
                }
            }

            sb.Reset();
            sb.Append("Money: $");
            sb.Append(state.Money, 2);
            sb.End();
            ImGui.TextUnformatted(sb);
            ImGui.Separator();
            editor.Draw();
        }
    }
}