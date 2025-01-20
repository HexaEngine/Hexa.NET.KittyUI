namespace Hexa.NET.KittyUI.WebView
{
    using CefSharp;
    using CefSharp.Enums;
    using CefSharp.Internals;
    using CefSharp.Structs;
    using System;
    using System.Threading.Tasks;

    public partial class WebView
    {
        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessage
        {
            add => browser.ConsoleMessage += value;
            remove => browser.ConsoleMessage -= value;
        }

        public event EventHandler<StatusMessageEventArgs> StatusMessage
        {
            add => browser.StatusMessage += value;
            remove => browser.StatusMessage -= value;
        }

        public event EventHandler<FrameLoadStartEventArgs> FrameLoadStart
        {
            add => browser.FrameLoadStart += value;
            remove => browser.FrameLoadStart -= value;
        }

        public event EventHandler<FrameLoadEndEventArgs> FrameLoadEnd
        {
            add => browser.FrameLoadEnd += value;
            remove => browser.FrameLoadEnd -= value;
        }

        public event EventHandler<LoadErrorEventArgs> LoadError
        {
            add => browser.LoadError += value;
            remove => browser.LoadError -= value;
        }

        public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged
        {
            add => browser.LoadingStateChanged += value;
            remove => browser.LoadingStateChanged -= value;
        }

        public event EventHandler<JavascriptMessageReceivedEventArgs> JavascriptMessageReceived
        {
            add
            {
                ((IWebBrowser)browser).JavascriptMessageReceived += value;
            }

            remove
            {
                ((IWebBrowser)browser).JavascriptMessageReceived -= value;
            }
        }


        public bool IsBrowserInitialized => browser.IsBrowserInitialized;

        public bool IsDisposed => browser.IsDisposed;

        public bool IsLoading => browser.IsLoading;

        public bool CanGoBack => browser.CanGoBack;

        public bool CanGoForward => browser.CanGoForward;

        public string Address => browser.Address;

        public IBrowser BrowserCore => browser.BrowserCore;

        public IAccessibilityHandler AccessibilityHandler { get => ((IRenderWebBrowser)browser).AccessibilityHandler; set => ((IRenderWebBrowser)browser).AccessibilityHandler = value; }

        public IBrowserAdapter BrowserAdapter => ((IWebBrowserInternal)browser).BrowserAdapter;

        public bool HasParent { get => ((IWebBrowserInternal)browser).HasParent; set => ((IWebBrowserInternal)browser).HasParent = value; }

        public IDisposable DevToolsContext { get => ((IWebBrowserInternal)browser).DevToolsContext; set => ((IWebBrowserInternal)browser).DevToolsContext = value; }

        public IJavascriptObjectRepository JavascriptObjectRepository => ((IWebBrowser)browser).JavascriptObjectRepository;

        public IDialogHandler DialogHandler { get => ((IWebBrowser)browser).DialogHandler; set => ((IWebBrowser)browser).DialogHandler = value; }

        public IRequestHandler RequestHandler { get => ((IWebBrowser)browser).RequestHandler; set => ((IWebBrowser)browser).RequestHandler = value; }

        public IDisplayHandler DisplayHandler { get => ((IWebBrowser)browser).DisplayHandler; set => ((IWebBrowser)browser).DisplayHandler = value; }

        public ILoadHandler LoadHandler { get => ((IWebBrowser)browser).LoadHandler; set => ((IWebBrowser)browser).LoadHandler = value; }

        public ILifeSpanHandler LifeSpanHandler { get => ((IWebBrowser)browser).LifeSpanHandler; set => ((IWebBrowser)browser).LifeSpanHandler = value; }

        public IKeyboardHandler KeyboardHandler { get => ((IWebBrowser)browser).KeyboardHandler; set => ((IWebBrowser)browser).KeyboardHandler = value; }

        public IJsDialogHandler JsDialogHandler { get => ((IWebBrowser)browser).JsDialogHandler; set => ((IWebBrowser)browser).JsDialogHandler = value; }

        public IDragHandler DragHandler { get => ((IWebBrowser)browser).DragHandler; set => ((IWebBrowser)browser).DragHandler = value; }

        public IDownloadHandler DownloadHandler { get => ((IWebBrowser)browser).DownloadHandler; set => ((IWebBrowser)browser).DownloadHandler = value; }

        public IContextMenuHandler MenuHandler { get => ((IWebBrowser)browser).MenuHandler; set => ((IWebBrowser)browser).MenuHandler = value; }

        public IFocusHandler FocusHandler { get => ((IWebBrowser)browser).FocusHandler; set => ((IWebBrowser)browser).FocusHandler = value; }

        public IResourceRequestHandlerFactory ResourceRequestHandlerFactory { get => ((IWebBrowser)browser).ResourceRequestHandlerFactory; set => ((IWebBrowser)browser).ResourceRequestHandlerFactory = value; }

        public IRenderProcessMessageHandler RenderProcessMessageHandler { get => ((IWebBrowser)browser).RenderProcessMessageHandler; set => ((IWebBrowser)browser).RenderProcessMessageHandler = value; }

        public IFindHandler FindHandler { get => ((IWebBrowser)browser).FindHandler; set => ((IWebBrowser)browser).FindHandler = value; }

        public IAudioHandler AudioHandler { get => ((IWebBrowser)browser).AudioHandler; set => ((IWebBrowser)browser).AudioHandler = value; }

        public IFrameHandler FrameHandler { get => ((IWebBrowser)browser).FrameHandler; set => ((IWebBrowser)browser).FrameHandler = value; }

        public IPermissionHandler PermissionHandler { get => ((IWebBrowser)browser).PermissionHandler; set => ((IWebBrowser)browser).PermissionHandler = value; }


        public string TooltipText => ((IWebBrowser)browser).TooltipText;

        public bool CanExecuteJavascriptInMainFrame => ((IWebBrowser)browser).CanExecuteJavascriptInMainFrame;

        public IRequestContext RequestContext => ((IWebBrowser)browser).RequestContext;

        public void LoadUrl(string url)
        {
            browser.LoadUrl(url);
        }

        public Task<LoadUrlAsyncResponse> LoadUrlAsync(string url)
        {
            return browser.LoadUrlAsync(url);
        }

        public Task<WaitForNavigationAsyncResponse> WaitForNavigationAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return browser.WaitForNavigationAsync(timeout, cancellationToken);
        }

        public bool Focus()
        {
            return false;
        }

        public ScreenInfo? GetScreenInfo()
        {
            return ((IRenderWebBrowser)browser).GetScreenInfo();
        }

        public CefSharp.Structs.Rect GetViewRect()
        {
            return ((IRenderWebBrowser)browser).GetViewRect();
        }

        public bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            return ((IRenderWebBrowser)browser).GetScreenPoint(viewX, viewY, out screenX, out screenY);
        }

        public void OnAcceleratedPaint(PaintElementType type, CefSharp.Structs.Rect dirtyRect, AcceleratedPaintInfo acceleratedPaintInfo)
        {
            ((IRenderWebBrowser)browser).OnAcceleratedPaint(type, dirtyRect, acceleratedPaintInfo);
        }

        public void OnPaint(PaintElementType type, CefSharp.Structs.Rect dirtyRect, nint buffer, int width, int height)
        {
            ((IRenderWebBrowser)browser).OnPaint(type, dirtyRect, buffer, width, height);
        }

        public void OnCursorChange(nint cursor, CefSharp.Enums.CursorType type, CursorInfo customCursorInfo)
        {
            ((IRenderWebBrowser)browser).OnCursorChange(cursor, type, customCursorInfo);
        }

        public bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return ((IRenderWebBrowser)browser).StartDragging(dragData, mask, x, y);
        }

        public void UpdateDragCursor(DragOperationsMask operation)
        {
            ((IRenderWebBrowser)browser).UpdateDragCursor(operation);
        }

        public void OnPopupShow(bool show)
        {
            ((IRenderWebBrowser)browser).OnPopupShow(show);
        }

        public void OnPopupSize(CefSharp.Structs.Rect rect)
        {
            ((IRenderWebBrowser)browser).OnPopupSize(rect);
        }

        public void OnImeCompositionRangeChanged(CefSharp.Structs.Range selectedRange, CefSharp.Structs.Rect[] characterBounds)
        {
            ((IRenderWebBrowser)browser).OnImeCompositionRangeChanged(selectedRange, characterBounds);
        }

        public void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
            ((IRenderWebBrowser)this.browser).OnVirtualKeyboardRequested(browser, inputMode);
        }

        public void OnAfterBrowserCreated(IBrowser browser)
        {
            ((IWebBrowserInternal)this.browser).OnAfterBrowserCreated(browser);
        }

        public void SetAddress(AddressChangedEventArgs args)
        {
            ((IWebBrowserInternal)browser).SetAddress(args);
        }

        public void SetLoadingStateChange(LoadingStateChangedEventArgs args)
        {
            ((IWebBrowserInternal)browser).SetLoadingStateChange(args);
        }

        public void SetTitle(TitleChangedEventArgs args)
        {
            ((IWebBrowserInternal)browser).SetTitle(args);
        }

        public void SetTooltipText(string tooltipText)
        {
            ((IWebBrowserInternal)browser).SetTooltipText(tooltipText);
        }

        public void SetCanExecuteJavascriptOnMainFrame(string frameId, bool canExecute)
        {
            ((IWebBrowserInternal)browser).SetCanExecuteJavascriptOnMainFrame(frameId, canExecute);
        }

        public void SetJavascriptMessageReceived(JavascriptMessageReceivedEventArgs args)
        {
            ((IWebBrowserInternal)browser).SetJavascriptMessageReceived(args);
        }

        public void OnFrameLoadStart(FrameLoadStartEventArgs args)
        {
            ((IWebBrowserInternal)browser).OnFrameLoadStart(args);
        }

        public void OnFrameLoadEnd(FrameLoadEndEventArgs args)
        {
            ((IWebBrowserInternal)browser).OnFrameLoadEnd(args);
        }

        public void OnConsoleMessage(ConsoleMessageEventArgs args)
        {
            ((IWebBrowserInternal)browser).OnConsoleMessage(args);
        }

        public void OnStatusMessage(StatusMessageEventArgs args)
        {
            ((IWebBrowserInternal)browser).OnStatusMessage(args);
        }

        public void OnLoadError(LoadErrorEventArgs args)
        {
            ((IWebBrowserInternal)browser).OnLoadError(args);
        }

        public void Load(string url)
        {
            ((IWebBrowser)browser).Load(url);
        }

        public Task<LoadUrlAsyncResponse> WaitForInitialLoadAsync()
        {
            return ((IWebBrowser)browser).WaitForInitialLoadAsync();
        }

        public IBrowser GetBrowser()
        {
            return ((IWebBrowser)browser).GetBrowser();
        }

        public bool TryGetBrowserCoreById(int browserId, out IBrowser browser)
        {
            return ((IWebBrowser)this.browser).TryGetBrowserCoreById(browserId, out browser);
        }

        public Task<DomRect> GetContentSizeAsync()
        {
            return ((IWebBrowser)browser).GetContentSizeAsync();
        }
    }
}
