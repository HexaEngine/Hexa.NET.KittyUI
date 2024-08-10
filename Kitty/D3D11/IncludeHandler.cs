#define SHADER_FORCE_OPTIMIZE

namespace Hexa.NET.Kitty.D3D11
{
    using Silk.NET.Core.Native;
    using System.Runtime.InteropServices;
    using System.Text;

    public unsafe class IncludeHandler
    {
        private readonly Stack<string> paths = new();
        private string basePath;

        public IncludeHandler(string basepath)
        {
            basePath = basepath;
        }

        public unsafe int Open(ID3DInclude* pInclude, D3DIncludeType IncludeType, byte* pFileName, void* pParentData, void** ppData, uint* pBytes)
        {
            string fileName = Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pFileName));
            string path = Path.Combine(basePath, fileName);
            var data = File.ReadAllBytes(path);

            paths.Push(basePath);
            var dirName = Path.GetDirectoryName(path);
            basePath = dirName ?? string.Empty;

            *ppData = AllocCopyT(data);
            *pBytes = (uint)data.Length;
            return 0;
        }

        public unsafe int Close(ID3DInclude* pInclude, void* pData)
        {
            basePath = paths.Pop();
            Free(pData);
            return 0;
        }
    }
}