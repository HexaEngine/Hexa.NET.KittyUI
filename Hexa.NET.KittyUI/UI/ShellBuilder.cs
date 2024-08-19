namespace Hexa.NET.KittyUI.UI
{
    public class ShellBuilder
    {
        private readonly Shell shell;

        public ShellBuilder(string name)
        {
            shell = new(name);
        }

        public Shell Shell => shell;

        public ShellBuilder AddPage(string path, IPage page)
        {
            shell.RegisterPage(path, page);
            return this;
        }

        public ShellBuilder AddPage<T>(string path) where T : IPage, new()
        {
            var page = new T();
            shell.RegisterPage(path, page);
            return this;
        }

        public ShellBuilder SetRootPage(string path)
        {
            shell.SetRootPage(path);
            return this;
        }
    }
}