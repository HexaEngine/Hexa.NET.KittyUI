namespace Hexa.NET.KittyUI.ImGuiBackend
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImNodes;

    public class ImNodesAddon : ImGuiAddon
    {
        private ImNodesContextPtr nodesContext;

        public override void Initialize(ImGuiContextPtr context)
        {
            ImNodes.SetImGuiContext(context);

            nodesContext = ImNodes.CreateContext();
            ImNodes.SetCurrentContext(nodesContext);
            ImNodes.StyleColorsDark(ImNodes.GetStyle());
        }

        protected override void DisposeCore()
        {
            if (!nodesContext.IsNull)
            {
                if (ImNodes.GetCurrentContext() == nodesContext)
                {
                    ImNodes.SetCurrentContext(null);
                }
                ImNodes.DestroyContext(nodesContext);
                nodesContext = null;
            }
            ImNodes.SetImGuiContext(null);
        }

        public override void NewFrame(ImGuiContextPtr context)
        {
            ImNodes.SetImGuiContext(context);
            ImNodes.SetCurrentContext(nodesContext);
        }
    }
}