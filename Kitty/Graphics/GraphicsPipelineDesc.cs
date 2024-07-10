namespace Kitty.Graphics
{
    using System.ComponentModel;

    public struct GraphicsPipelineDesc
    {
        public GraphicsPipelineDesc()
        {
        }

        [DefaultValue(null)]
        public string? VertexShader = null;

        [DefaultValue(null)]
        public string? VertexShaderCode = null;

        [DefaultValue("main")]
        public string VertexShaderEntrypoint = "main";

        [DefaultValue(null)]
        public string? HullShader = null;

        [DefaultValue(null)]
        public string? HullShaderCode = null;

        [DefaultValue("main")]
        public string HullShaderEntrypoint = "main";

        [DefaultValue(null)]
        public string? DomainShader = null;

        [DefaultValue(null)]
        public string? DomainShaderCode = null;

        [DefaultValue("main")]
        public string DomainShaderEntrypoint = "main";

        [DefaultValue(null)]
        public string? GeometryShader = null;

        [DefaultValue(null)]
        public string? GeometryShaderCode = null;

        [DefaultValue("main")]
        public string GeometryShaderEntrypoint = "main";

        [DefaultValue(null)]
        public string? PixelShader = null;

        [DefaultValue(null)]
        public string? PixelShaderCode = null;

        [DefaultValue("main")]
        public string PixelShaderEntrypoint = "main";

        public GraphicsPipelineState State = GraphicsPipelineState.Default;

        [DefaultValue(null)]
        public InputElementDescription[]? InputElements = null;

        [DefaultValue(null)]
        public ShaderMacro[]? Macros = null;
    }
}