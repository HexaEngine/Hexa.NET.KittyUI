namespace Hexa.NET.KittyUI.UI
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using System.Numerics;

    public abstract class Page : IPage
    {
        protected bool IsDocked;

        protected bool windowEnded;

        private Vector2 size;

        private Vector2 position;

        protected ImGuiWindowFlags Flags;

        private INavigation navigation = null!;

        public abstract string Title { get; }

        public Vector2 Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                ImGuiP.SetWindowSize(Title, size);
            }
        }

        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                ImGuiP.SetWindowPos(Title, position);
            }
        }

        INavigation IPage.Navigation { get => navigation; set => navigation = value; }

        public INavigation Navigation => navigation;

        public event SizeChangedEventHandler? SizeChanged;

        public event PositionChangedEventHandler? PositionChanged;

        public virtual void OnNavigatedFrom(IPage? nextPage)
        {
        }

        public virtual void OnNavigatedTo(IPage? previousPage)
        {
        }

        public virtual unsafe void DrawPage(ImGuiWindowFlags overwriteFlags)
        {
            ImGuiWindowFlags flags = Flags | overwriteFlags;
            if (!ImGui.BeginChild(Title, 0, flags))
            {
                size = Vector2.Zero;
                ImGui.EndChild();
                return;
            }

            Vector2 windowSize = ImGui.GetWindowSize();
            if (size != windowSize)
            {
                Vector2 oldSize = size;
                size = windowSize;
                OnSizeChangedInternal(oldSize, size);
            }

            Vector2 windowPos = ImGui.GetWindowPos();
            if (position != windowPos)
            {
                Vector2 oldPosition = position;
                position = windowPos;
                OnPositionChangedInternal(oldPosition, position);
            }

            windowEnded = false;
            DrawContent();
            if (!windowEnded)
            {
                ImGui.EndChild();
            }
        }

        private void OnPositionChangedInternal(Vector2 oldPosition, Vector2 position)
        {
            OnPositionChanged(oldPosition, position);
            this.PositionChanged?.Invoke(this, oldPosition, position);
        }

        protected virtual void OnPositionChanged(Vector2 oldPosition, Vector2 position)
        {
        }

        private void OnSizeChangedInternal(Vector2 oldSize, Vector2 size)
        {
            OnSizeChanged(oldSize, size);
            this.SizeChanged?.Invoke(this, oldSize, size);
        }

        protected virtual void OnSizeChanged(Vector2 oldSize, Vector2 size)
        {
        }

        public abstract void DrawContent();

        protected void EndChild()
        {
            if (!windowEnded)
            {
                ImGui.EndChild();
                windowEnded = true;
            }
        }

        public virtual void Dispose()
        {
        }
    }
}