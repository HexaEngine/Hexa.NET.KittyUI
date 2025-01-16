namespace Hexa.NET.KittyUI.ImGuiBackend
{
    using Hexa.NET.ImGui;

    public abstract class ImGuiAddon : IDisposable
    {
        private bool disposedValue;

        public virtual void Initialize(ImGuiContextPtr context)
        {
        }

        public virtual void NewFrame(ImGuiContextPtr context)
        {
        }

        public virtual void PostNewFrame(ImGuiContextPtr context)
        {
        }

        public virtual void EndFrame()
        {
        }

        public virtual void PostEndFrame()
        {
        }

        public abstract void DisposeCore();

        public void Dispose()
        {
            if (!disposedValue)
            {
                DisposeCore();
                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}