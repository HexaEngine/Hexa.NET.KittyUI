using Hexa.NET.KittyUI.Windows;
using Hexa.NET.SDL2;

namespace Hexa.NET.KittyUI.UI;

public interface ITitleBar
{
    public event EventHandler<CloseWindowRequest>? CloseWindowRequest;

    public event EventHandler<MaximizeWindowRequest>? MaximizeWindowRequest;

    public event EventHandler<MinimizeWindowRequest>? MinimizeWindowRequest;

    public event EventHandler<RestoreWindowRequest>? RestoreWindowRequest;

    public CoreWindow Window { get; set; } 
        
    public int Height { get; set; }
        
    public void OnAttach(CoreWindow window);

    public void OnDetach(CoreWindow window);

    public unsafe SDLHitTestResult HitTest(SDLWindow* win, SDLPoint* area, void* data);

    public void RequestClose();

    public unsafe void RequestMinimize();

    public void RequestMaximize();

    public void RequestRestore();

    public void Draw();
}