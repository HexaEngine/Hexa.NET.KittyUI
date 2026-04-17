using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Hexa.NET.KittyUI.UI
{
    public class ShellBuilder
    {
        private readonly AppHost appHost;
        private readonly Shell shell;

        public ShellBuilder(AppHost appHost, string name)
        {
            this.appHost = appHost;
            shell = new(name);
        }

        public Shell Shell => shell;

        public ShellBuilder AddPage(string path, Page page)
        {
            shell.RegisterPage(path, page);
            return this;
        }

        public ShellBuilder AddPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string path) where T : Page
        {
            var page = ActivatorUtilities.CreateInstance<T>(appHost.Services);
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