namespace Hexa.NET.KittyUI.ImGuiBackend
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImPlot;

    public class ImPlotAddon : ImGuiAddon
    {
        private ImPlotContextPtr plotContext;

        public override void Initialize(ImGuiContextPtr context)
        {
            ImPlot.SetImGuiContext(context);

            plotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext(plotContext);
            ImPlot.StyleColorsDark(ImPlot.GetStyle());
        }

        public override void DisposeCore()
        {
            if (!plotContext.IsNull)
            {
                if (ImPlot.GetCurrentContext() == plotContext)
                {
                    ImPlot.SetCurrentContext(null);
                }
                ImPlot.DestroyContext(plotContext);
                plotContext = null;
            }
            ImPlot.SetImGuiContext(null);
        }

        public override void NewFrame(ImGuiContextPtr context)
        {
            ImPlot.SetImGuiContext(context);
            ImPlot.SetCurrentContext(plotContext);
        }
    }
}