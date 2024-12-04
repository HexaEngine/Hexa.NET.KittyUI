namespace Hexa.NET.KittyUI.OpenGL
{
#if GLES

    using Hexa.NET.OpenGLES;

#else

    using Hexa.NET.OpenGL;

#endif

    using System;

    public struct GLVersion : IEquatable<GLVersion>
    {
        public int Major;
        public int Minor;
        public bool ES;

        public GLVersion(int major, int minor, bool es = false)
        {
            Major = major;
            Minor = minor;
            ES = es;
        }

        public unsafe static GLVersion Current { get; internal set; }

        internal static unsafe GLVersion GetInternalVersion(GL GL)
        {
            byte* versionStr = GL.GetString(GLStringName.Version);

            if (versionStr == null)
            {
                return default;
            }

            int major = 0, minor = 0;
            bool isES = false;

            byte* ptr = versionStr;
            while (*ptr != 0)
            {
                if (*ptr == (byte)'E' && *(ptr + 1) == (byte)'S')
                {
                    isES = true;
                    break;
                }
                ptr++;
            }

            ptr = versionStr;

            if (*ptr >= '0' && *ptr <= '9')
            {
                major = *ptr - '0';
                ptr++;
                if (*ptr >= '0' && *ptr <= '9')
                {
                    major = major * 10 + (*ptr - '0');
                    ptr++;
                }
            }

            while (*ptr != 0 && *ptr != (byte)'.')
            {
                ptr++;
            }
            ptr++; // Move past the dot

            if (*ptr >= '0' && *ptr <= '9')
            {
                minor = *ptr - '0';
                ptr++;
                if (*ptr >= '0' && *ptr <= '9')
                {
                    minor = minor * 10 + (*ptr - '0');
                }
            }

            return new GLVersion(major, minor, isES);
        }

        public readonly bool IsAtLeast(int major, int minor)
        {
            if (Major > major)
                return true;
            if (Major == major && Minor >= minor)
                return true;
            return false;
        }

        public static bool operator >(GLVersion v1, GLVersion v2)
        {
            if (v1.ES && !v2.ES) return false;
            if (!v1.ES && v2.ES) return false;

            if (v1.Major > v2.Major)
                return true;
            if (v1.Major == v2.Major && v1.Minor > v2.Minor)
                return true;
            return false;
        }

        public static bool operator <(GLVersion v1, GLVersion v2)
        {
            return !(v1 >= v2);
        }

        public static bool operator >=(GLVersion v1, GLVersion v2)
        {
            // Treat OpenGL as greater than OpenGL ES by default
            if (v1.ES && !v2.ES) return false;
            if (!v1.ES && v2.ES) return false;

            if (v1.Major > v2.Major)
                return true;
            if (v1.Major == v2.Major && v1.Minor >= v2.Minor)
                return true;
            return false;
        }

        public static bool operator <=(GLVersion v1, GLVersion v2)
        {
            return !(v1 > v2);
        }

        public override readonly string ToString()
        {
            return ES ? $"OpenGL ES {Major}.{Minor}" : $"OpenGL {Major}.{Minor}";
        }

        public override readonly bool Equals(object? obj)
        {
            return obj is GLVersion version && Equals(version);
        }

        public readonly bool Equals(GLVersion other)
        {
            return Major == other.Major &&
                   Minor == other.Minor &&
                   ES == other.ES;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, ES);
        }

        public static bool operator ==(GLVersion left, GLVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GLVersion left, GLVersion right)
        {
            return !(left == right);
        }
    }
}