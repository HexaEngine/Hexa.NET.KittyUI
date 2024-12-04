namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    public struct OpenGLSampleDesc
    {
        public uint Count;
        public uint Quality;

        public OpenGLSampleDesc(uint count, uint quality)
        {
            Count = count;
            Quality = quality;
        }
    }

    public enum GLTextureMiscFlags
    {
        None,
        CubeMap,
    }

    public struct OpenGLTexture2DDesc
    {
        public int Width;
        public int Height;
        public uint MipLevels;
        public uint ArraySize;
        public GLInternalFormat InternalFormat;
        public GLPixelFormat PixelFormat;
        public GLPixelType PixelType;
        public GLTextureWrapMode WrapS;
        public GLTextureWrapMode WrapT;
        public GLTextureMinFilter MinFilter;
        public GLTextureMagFilter MagFilter;
        public GLTextureSwizzle SwizzleR;
        public GLTextureSwizzle SwizzleG;
        public GLTextureSwizzle SwizzleB;
        public GLTextureSwizzle SwizzleA;
        public GLTextureMiscFlags MiscFlags;
        public OpenGLSampleDesc SampleDesc;

        public readonly GLTextureTarget TextureTarget
        {
            get
            {
                bool ms = SampleDesc.Count > 1;
                var target = ms ? GLTextureTarget.Texture2DMultisample : GLTextureTarget.Texture2D;

                if (ArraySize > 1)
                {
                    target = ms ? GLTextureTarget.Texture2DMultisampleArray : GLTextureTarget.Texture2DArray;
                }

                if ((MiscFlags & GLTextureMiscFlags.CubeMap) != 0 && ArraySize % 6 == 0)
                {
                    target = ArraySize > 6 ? GLTextureTarget.CubeMapArray : GLTextureTarget.CubeMap;
                }

                return target;
            }
        }

        public readonly unsafe void PrepareTexture(GL GL, uint textureId)
        {
            var target = TextureTarget;
            bool compressed = FormatHelper.IsCompressedFormat(InternalFormat);
            bool ms = SampleDesc.Count > 1;

            GL.BindTexture(target, textureId);

            GL.TexParameteri(target, GLTextureParameterName.MinFilter, (int)MinFilter);
            GL.TexParameteri(target, GLTextureParameterName.MagFilter, (int)MagFilter);
            GL.TexParameteri(target, GLTextureParameterName.WrapS, (int)WrapS);
            GL.TexParameteri(target, GLTextureParameterName.WrapT, (int)WrapT);

            GL.TexParameteri(target, GLTextureParameterName.SwizzleR, (int)SwizzleR);
            GL.TexParameteri(target, GLTextureParameterName.SwizzleG, (int)SwizzleG);
            GL.TexParameteri(target, GLTextureParameterName.SwizzleB, (int)SwizzleB);
            GL.TexParameteri(target, GLTextureParameterName.SwizzleA, (int)SwizzleA);

            if (ms)
            {
                switch (target)
                {
                    case GLTextureTarget.Texture2DMultisample:
                        GL.TexImage2DMultisample(target, (int)SampleDesc.Count, InternalFormat, Width, Height, SampleDesc.Quality > 0);
                        break;

                    case GLTextureTarget.Texture2DMultisampleArray:
                        GL.TexImage3DMultisample(target, (int)SampleDesc.Count, InternalFormat, Width, Height, (int)ArraySize, SampleDesc.Quality > 0);
                        break;
                }
            }
            else
            {
                for (int level = 0; level < MipLevels; level++)
                {
                    int mipWidth = Math.Max(1, Width >> level);
                    int mipHeight = Math.Max(1, Height >> level);
                    int compressedSize = compressed ? FormatHelper.CalculateCompressedDataSize(InternalFormat, mipWidth, mipHeight) : 0;

                    switch (target)
                    {
                        case GLTextureTarget.Texture2D:
                            if (compressed)
                            {
                                GL.CompressedTexImage2D(target, level, InternalFormat, mipWidth, mipHeight, 0, compressedSize, null);
                            }
                            else
                            {
                                GL.TexImage2D(target, level, InternalFormat, mipWidth, mipHeight, 0, PixelFormat, PixelType, null);
                            }
                            break;

                        case GLTextureTarget.Texture2DArray or GLTextureTarget.CubeMapArray:
                            if (compressed)
                            {
                                GL.CompressedTexImage3D(target, level, InternalFormat, mipWidth, mipHeight, (int)ArraySize, 0, compressedSize, null);
                            }
                            else
                            {
                                GL.TexImage3D(target, level, InternalFormat, mipWidth, mipHeight, (int)ArraySize, 0, PixelFormat, PixelType, null);
                            }
                            break;

                        case GLTextureTarget.CubeMap:
                            for (GLTextureTarget i = GLTextureTarget.CubeMapPositiveX; i < GLTextureTarget.CubeMapNegativeZ; i++)
                            {
                                if (compressed)
                                {
                                    GL.CompressedTexImage2D(i, level, InternalFormat, mipWidth, mipHeight, 0, compressedSize, null);
                                }
                                else
                                {
                                    GL.TexImage2D(i, level, InternalFormat, mipWidth, mipHeight, 0, PixelFormat, PixelType, null);
                                }
                            }
                            break;
                    }
                }
            }

            GL.BindTexture(target, 0);
        }
    }
}