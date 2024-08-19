namespace Hexa.NET.KittyUI.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class NavigationManager : INavigation
    {
        private readonly Dictionary<string, IPage> _pages = new();
        private readonly List<IPage> _navigationHistory = new();
        private readonly Stack<IPage> _navForward = new();
        private IPage? _currentPage;
        private IPage? _rootPage;
        private string _currentPath = "/";

        public IPage? CurrentPage => _currentPage;

        public string CurrentPath => _currentPath;

        public void RegisterPage(string path, IPage page)
        {
            if (_rootPage == null)
            {
                _rootPage = page;
            }
            var normalizedPath = NormalizePath(path);
            if (!_pages.ContainsKey(normalizedPath))
            {
                _pages[normalizedPath] = page;
            }
            page.Navigation = this;
        }

        public void NavigateTo(string path)
        {
            var resolvedPath = ResolvePath(path);
            if (_pages.TryGetValue(resolvedPath, out var page))
            {
                NavigateTo(page);
                _currentPath = resolvedPath;
            }
            else
            {
                Debug.Assert(false, $"Page not found: {resolvedPath}");
            }
        }

        public void NavigateTo(IPage page)
        {
            if (_currentPage != null)
            {
                _currentPage.OnNavigatedFrom(page);
                _navigationHistory.Add(_currentPage);
                _navForward.Clear();
            }
            page.OnNavigatedTo(_currentPage);
            _currentPage = page;
        }

        public void NavigateBackTo(IPage page)
        {
            if (!_navigationHistory.Contains(page))
            {
                return;
            }

            var previousPage = _currentPage;

            while (_navigationHistory.Count > 0 && _currentPage != page)
            {
                if (_currentPage != null)
                {
                    _navForward.Push(_currentPage);
                }

                _currentPage = _navigationHistory[^1];
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
            }

            if (previousPage == _currentPage)
            {
                return;
            }

            _currentPage!.OnNavigatedFrom(previousPage);
            previousPage?.OnNavigatedTo(_currentPage);
            _currentPath = GetPathForPage(_currentPage);
        }

        public void NavigateBack()
        {
            if (_navigationHistory.Count > 0)
            {
                if (_currentPage != null)
                {
                    _navForward.Push(_currentPage);
                }

                var previousPage = _navigationHistory[^1];
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
                _currentPage?.OnNavigatedFrom(previousPage);
                previousPage.OnNavigatedTo(_currentPage);

                _currentPage = previousPage;
                _currentPath = GetPathForPage(_currentPage); // Update current path
            }
        }

        public void NavigateForward()
        {
            if (_navForward.Count > 0)
            {
                if (_currentPage != null)
                {
                    _navigationHistory.Add(_currentPage);
                }

                var previousPage = _navForward.Pop();
                previousPage.OnNavigatedTo(_currentPage);
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

        private static string NormalizePath(string path)
        {
            ReadOnlySpan<char> p = path.AsSpan().TrimEnd('/');
            if (p.Length > 0 && p[0] != '/')
            {
                return $"/{p}"; // root the path.
            }

            return p.ToString();
        }

        private string ResolvePath(string path)
        {
            if (path.StartsWith('/'))
            {
                // Absolute path
                return NormalizePath(path);
            }
            else
            {
                ReadOnlySpan<char> p = path.AsSpan().Trim();

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

        private string GetPathForPage(IPage page)
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

        public IEnumerable<IPage> GetHistoryStack()
        {
            foreach (var item in _navigationHistory)
            {
                yield return item;
            }

            if (_currentPage != null)
            {
                yield return _currentPage;
            }
        }
    }
}