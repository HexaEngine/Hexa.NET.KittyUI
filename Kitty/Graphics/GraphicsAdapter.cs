namespace Kitty.Graphics
{
    using Silk.NET.Core.Contexts;
    using System;
    using System.Linq;

    public static class GraphicsAdapter
    {
        public static List<IGraphicsAdapter> Adapters { get; } = new();

        public static GraphicsBackend Backend { get; private set; }

        public static IGraphicsAdapter ChooseAdapter(GraphicsBackend backend)
        {
            if (backend == GraphicsBackend.Auto)
            {
                if (Adapters.Count == 1)
                {
                    IGraphicsAdapter adapter = Adapters[0];
                    Backend = adapter.Backend;
                    return adapter;
                }
                else
                {
                    IGraphicsAdapter adapter = Adapters[0];
                    for (int i = 0; i < Adapters.Count; i++)
                    {
                        if (Adapters[i].PlatformScore > adapter.PlatformScore)
                        {
                            adapter = Adapters[i];
                        }
                    }
                    Backend = adapter.Backend;
                    return adapter;
                }
            }

            {
                IGraphicsAdapter adapter = Adapters.FirstOrDefault(x => x.Backend == backend) ?? throw new PlatformNotSupportedException();
                Backend = adapter.Backend;
                return adapter;
            }
        }

        public static IGraphicsDevice CreateGraphicsDevice(INativeWindowSource window, GraphicsBackend backend, bool debug)
        {
            if (backend == GraphicsBackend.Auto)
            {
                if (Adapters.Count == 1)
                {
                    IGraphicsAdapter adapter = Adapters[0];
                    Backend = adapter.Backend;
                    return adapter.CreateGraphicsDevice(debug);
                }
                else
                {
                    IGraphicsAdapter adapter = Adapters[0];
                    for (int i = 0; i < Adapters.Count; i++)
                    {
                        if (Adapters[i].PlatformScore > adapter.PlatformScore)
                        {
                            adapter = Adapters[i];
                        }
                    }
                    Backend = adapter.Backend;
                    return adapter.CreateGraphicsDevice(debug);
                }
            }

            {
                IGraphicsAdapter adapter = Adapters.FirstOrDefault(x => x.Backend == backend) ?? throw new PlatformNotSupportedException();
                Backend = adapter.Backend;
                return adapter.CreateGraphicsDevice(debug);
            }
        }
    }
}