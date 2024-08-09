namespace Kitty.Text
{
    public static class Utf8Formatter
    {
        public static unsafe int FormatByteSize(byte* buf, int bufSize, long byteSize, bool addSuffixSpace, int digits = -1)
        {
            const int suffixes = 7;
            int suffixIndex = 0;
            float size = byteSize;
            while (size >= 1024 && suffixIndex < suffixes)
            {
                size /= 1024;
                suffixIndex++;
            }

            int suffixSize = suffixIndex == 0 ? 1 : 2;  // 'B' or 'KB', 'MB', etc.

            if (addSuffixSpace)
            {
                suffixSize++;
            }

            // Early exit if the buffer is too small
            if (bufSize - suffixSize <= 0)
            {
                return 0;
            }

            int i = FormatFloat(size, buf, bufSize - suffixSize, digits) - 1; // overwrite terminator from FormatFloat.

            if (addSuffixSpace)
            {
                buf[i++] = (byte)' ';
            }

            byte suffix = suffixIndex switch
            {
                1 => (byte)'K',
                2 => (byte)'M',
                3 => (byte)'G',
                4 => (byte)'T',
                5 => (byte)'P',
                6 => (byte)'E',
                _ => 0,
            };

            if (suffix != 0)
            {
                buf[i++] = suffix;
            }

            buf[i++] = (byte)'B';
            buf[i] = 0; // Null-terminate

            return i;
        }

        public static unsafe int FractionToInt(float number, int precision)
        {
            number -= (int)number;
            return (int)(number * MathF.Pow(10, precision));
        }

        public static unsafe int FractionToIntLimit(float number, int maxPrecision)
        {
            number -= (int)number;
            for (int i = 0; i < maxPrecision && number - (int)number < 0; i++)
            {
                number *= 10;
            }

            return (int)number;
        }

        public static unsafe int FractionToInt(double number, int precision)
        {
            number -= (int)number;
            return (int)(number * MathF.Pow(10, precision));
        }

        public static unsafe int FractionToIntLimit(double number, int maxPrecision)
        {
            number -= (int)number;
            for (int i = 0; i < maxPrecision && number - (int)number < 0; i++)
            {
                number *= 10;
            }

            return (int)number;
        }

        public static unsafe int FormatFloat(float value, byte* buffer, int bufSize, int digits = -1)
        {
            if (float.IsNaN(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'N';
                buffer[1] = (byte)'a';
                buffer[2] = (byte)'N';
                buffer[3] = 0;
                return 4;
            }
            if (float.IsPositiveInfinity(value))
            {
                if (bufSize < 4)
                {
                    return 0;
                }
                buffer[0] = (byte)'i';
                buffer[1] = (byte)'n';
                buffer[2] = (byte)'f';
                buffer[3] = 0;
                return 4;
            }
            if (float.IsNegativeInfinity(value))
            {
                if (bufSize < 5)
                {
                    return 0;
                }
                buffer[0] = (byte)'-';
                buffer[1] = (byte)'i';
                buffer[2] = (byte)'n';
                buffer[3] = (byte)'f';
                buffer[4] = 0;
                return 5;
            }
            if (value == 0)
            {
                if (bufSize < 2)
                {
                    return 0;
                }
                buffer[0] = (byte)'0';
                buffer[1] = 0;
                return 2;
            }

            var number = MathF.Truncate(value);
            var fraction = value - number;

            int i = FormatInt((int)number, buffer, bufSize) - 1; // overwrite terminator.

            buffer[i++] = (byte)'.';

            if (i >= bufSize)
            {
                buffer[bufSize - 1] = 0; // Null-terminate in case of overflow
                return i;
            }
            int factionInt;
            if (digits >= 0)
            {
                factionInt = FractionToInt(fraction, Math.Min(bufSize - i - 1, digits));
            }
            else
            {
                factionInt = FractionToIntLimit(fraction, bufSize - i - 1);
            }

            i += FormatInt(factionInt, buffer + i, bufSize - i);

            return i;
        }

        public static unsafe int FormatInt(int value, byte* buffer, int bufSize)
        {
            if (bufSize < 2)
            {
                return 0;
            }

            int abs = Math.Abs(value);

            int i = 0;
            if (value < 0 && i < bufSize)
            {
                buffer[i++] = (byte)'-';
            }

            if (abs == 0 && i < bufSize)
            {
                buffer[i++] = (byte)'0';
            }
            else
            {
                while (abs > 0 && i < bufSize - 1)
                {
                    buffer[i++] = (byte)('0' + abs % 10);
                    abs /= 10;
                }
            }

            // Reverse the digits for correct order
            for (int j = 0, k = i - 1; j < k; j++, k--)
            {
                (buffer[k], buffer[j]) = (buffer[j], buffer[k]);
            }

            if (i < bufSize)
            {
                buffer[i++] = 0; // Null-terminate
            }
            else
            {
                buffer[bufSize - 1] = 0; // Force Null-terminate
            }

            return i;
        }
    }
}