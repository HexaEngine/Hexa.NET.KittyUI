using System.Diagnostics;

namespace Hexa.NET.KittyUI.UI
{
    public class NavigationManager : INavigation
    {
        private readonly Dictionary<string, Page> _pages = [];
        private readonly List<NavigationItem> _navigationHistory = [];
        private readonly Stack<NavigationItem> _navigationForward = new();
        private NavigationItem? _currentPage;
        private Page? _rootPage;
        private string _currentPath = "/";

        private struct NavigationItem(Page page, object? args) : IEquatable<NavigationItem>
        {
            public Page Page = page;
            public object? Args = args;

            public readonly override bool Equals(object? obj)
            {
                return obj is NavigationItem item && Equals(item);
            }

            public readonly bool Equals(NavigationItem other)
            {
                return EqualityComparer<Page>.Default.Equals(Page, other.Page);
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(Page);
            }

            public static bool operator ==(NavigationItem left, NavigationItem right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(NavigationItem left, NavigationItem right)
            {
                return !(left == right);
            }
        }

        public Page? CurrentPage => _currentPage.HasValue ? _currentPage.Value.Page : null;

        public string CurrentPath => _currentPath;

        public bool CanGoBack => _navigationHistory.Count > 0;

        public bool CanGoForward => _navigationForward.Count > 0;

        public event EventHandler? OpenMenu;

        public void ClearHistory()
        {
            _navigationHistory.Clear();
            _navigationForward.Clear();
        }

        public void RegisterPage(string path, Page page)
        {
            _rootPage ??= page;
            var normalizedPath = NormalizePath(path);
            if (!_pages.ContainsKey(normalizedPath))
            {
                _pages[normalizedPath] = page;
            }
            page.Navigation = this;
        }

        public void NavigateTo(string path, object? args = null)
        {
            var resolvedPath = ResolvePath(path);
            if (_pages.TryGetValue(resolvedPath, out var page))
            {
                NavigateTo(page, args);
                _currentPath = resolvedPath;
            }
            else
            {
                Debug.Assert(false, $"Page not found: {resolvedPath}");
            }
        }

        public void NavigateTo(Page page, object? args = null)
        {
            if (_currentPage != null)
            {
                _currentPage.Value.Page.OnNavigatedFrom(page, args);
                _navigationHistory.Add(_currentPage.Value);
                _navigationForward.Clear();
            }
            page.OnNavigatedTo(CurrentPage, args);
            _currentPage = new NavigationItem(page, args);
        }

        public void NavigateBackTo(Page page)
        {
            if (!_navigationHistory.Contains(new(page, null)))
            {
                return;
            }

            var previousPage = _currentPage;

            while (_navigationHistory.Count > 0 && CurrentPage != page)
            {
                if (_currentPage != null)
                {
                    _navigationForward.Push(_currentPage.Value);
                }

                _currentPage = _navigationHistory[^1];
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
            }

            if (previousPage == _currentPage)
            {
                return;
            }

            var currPage = _currentPage!.Value;
            currPage.Page.OnNavigatedFrom(previousPage?.Page, previousPage?.Args);
            previousPage?.Page.OnNavigatedTo(currPage.Page, currPage.Args);
            _currentPath = GetPathForPage(currPage.Page);
        }

        public void NavigateBack()
        {
            if (_navigationHistory.Count > 0)
            {
                if (_currentPage != null)
                {
                    _navigationForward.Push(_currentPage.Value);
                }

                var previousPage = _navigationHistory[^1];
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
                _currentPage?.Page.OnNavigatedFrom(previousPage.Page, previousPage.Args);
                previousPage.Page.OnNavigatedTo(_currentPage?.Page, _currentPage?.Args);

                _currentPage = previousPage;
                _currentPath = GetPathForPage(_currentPage.Value.Page); // Update current path
            }
        }

        public void NavigateForward()
        {
            if (_navigationForward.Count > 0)
            {
                if (_currentPage != null)
                {
                    _navigationHistory.Add(_currentPage.Value);
                }

                var previousPage = _navigationForward.Pop();
                previousPage.Page.OnNavigatedTo(_currentPage?.Page, _currentPage?.Args);
                _currentPage = previousPage;
            }
        }

        public void NavigateToRoot()
        {
            if (_rootPage != null)
            {
                NavigateTo(_rootPage);
                _currentPath = "/";
            }
        }

        public void SetRootPage(string path)
        {
            var normalizedPath = NormalizePath(path);
            if (_pages.TryGetValue(normalizedPath, out var page))
            {
                _rootPage = page;
                _currentPath = normalizedPath;
            }
        }

        private static string NormalizePath(ReadOnlySpan<char> path)
        {
            ReadOnlySpan<char> p = path.TrimEnd('/');
            if (p.Length > 0 && p[0] != '/')
            {
                return $"/{p}"; // root the path.
            }

            return p.ToString();
        }

        private string ResolvePath(ReadOnlySpan<char> path)
        {
            if (path.StartsWith("/"))
            {
                // Absolute path
                return NormalizePath(path);
            }
            else
            {
                ReadOnlySpan<char> p = path.Trim();

                int level = 0;
                while (p.Length > 0)
                {
                    var nextSlash = p.IndexOf('/');
                    if (nextSlash == -1)
                    {
                        nextSlash = p.Length;
                    }

                    var part = p[..nextSlash];

                    if (part.Length == 1 && part[0] == '.')
                    {
                    }
                    else if (part.Length == 2 && part[0] == '.' && part[1] == '.')
                    {
                        level++;
                    }
                    else
                    {
                        break;
                    }

                    p = p[(nextSlash + 1)..];
                }

                ReadOnlySpan<char> cp = _currentPath;
                while (cp.Length > 0)
                {
                    var idx = cp.LastIndexOf('/');
                    if (idx == -1)
                    {
                        idx = 0;
                    }

                    cp = cp[..idx];
                    level--;
                    if (level == 0)
                    {
                        break;
                    }
                }

                return $"{cp}/{p}";
            }
        }

        private string GetPathForPage(Page page)
        {
            foreach (var kvp in _pages)
            {
                if (kvp.Value == page)
                {
                    return string.IsNullOrWhiteSpace(kvp.Key) ? "/" : kvp.Key;
                }
            }
            return "/";
        }

        public IEnumerable<Page> GetHistoryStack()
        {
            foreach (var item in _navigationHistory)
            {
                yield return item.Page;
            }

            if (_currentPage != null)
            {
                yield return _currentPage.Value.Page;
            }
        }

        public void ShowMenu()
        {
            OpenMenu?.Invoke(this, EventArgs.Empty);
        }
    }
}