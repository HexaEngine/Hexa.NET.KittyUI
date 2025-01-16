namespace Hexa.NET.KittyUI.ImGuiBackend
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGuizmo;

    public class ImGuizmoAddon : ImGuiAddon
    {
        public override void Initialize(ImGuiContextPtr context)
        {
            ImGuizmo.SetImGuiContext(context);
        }

        public override void DisposeCore()
        {
            ImGuizmo.SetImGuiContext(null);
        }

        public override void NewFrame(ImGuiContextPtr context)
        {
            ImGuizmo.SetImGuiContext(context);
        }

        public override void PostNewFrame(ImGuiContextPtr context)
        {
            ImGuizmo.BeginFrame();
        }
    }
}