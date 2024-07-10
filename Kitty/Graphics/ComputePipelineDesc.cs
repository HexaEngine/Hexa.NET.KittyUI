namespace Kitty.Graphics
{
    public struct ComputePipelineDesc
    {
        public string? Path;
        public string Entry;
        public ShaderMacro[]? Macros;

        public ComputePipelineDesc()
        {
            Path = null;
            Entry = "main";
        }

        public ComputePipelineDesc(string? path) : this()
        {
            Path = path;
        }

        public ComputePipelineDesc(string? path, string entry)
        {
            Path = path;
            Entry = entry;
        }

        public ComputePipelineDesc(string? path, string entry = "main", ShaderMacro[]? macros = null)
        {
            Path = path;
            Entry = entry;
            Macros = macros;
        }
    }
}