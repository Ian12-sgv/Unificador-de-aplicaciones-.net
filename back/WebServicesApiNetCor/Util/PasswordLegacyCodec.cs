using System;
using System.Collections.Generic;
using System.Linq;

namespace WebServicesApiNetCor.Util
{
    /// <summary>
    /// Genera el formato de bytes usado por las contraseñas legacy de dbo.USUARIOS.
    /// </summary>
    public static class PasswordLegacyCodec
    {
        private const byte Separator = 0x3F;

        private static readonly Dictionary<char, byte[]> Table = new Dictionary<char, byte[]>
        {
            ['0'] = new byte[] { 0xAE, 0xC5 }, ['1'] = new byte[] { 0xAE, 0xE6 },
            ['2'] = new byte[] { 0xB5, 0xA3 }, ['3'] = new byte[] { 0xB5, 0xB6 },
            ['4'] = new byte[] { 0xB5, 0xA4 }, ['5'] = new byte[] { 0xB5, 0xA2 },
            ['6'] = new byte[] { 0xB5, 0xAE }, ['7'] = new byte[] { 0xB5, 0xB5 },
            ['8'] = new byte[] { 0xB5, 0xBB }, ['9'] = new byte[] { 0xB5, 0xA9 },
            ['a'] = new byte[] { 0xE6, 0xA9 }, ['b'] = new byte[] { 0xE6, 0xC5 },
            ['c'] = new byte[] { 0xE6, 0xE6 }, ['d'] = new byte[] { 0xB6, 0xA3, 0xA3 },
            ['e'] = new byte[] { 0xB6, 0xA3, 0xB6 }, ['f'] = new byte[] { 0xB6, 0xA3, 0xA4 },
            ['g'] = new byte[] { 0xB6, 0xA3, 0xA2 }, ['h'] = new byte[] { 0xB6, 0xA3, 0xAE },
            ['i'] = new byte[] { 0xB6, 0xA3, 0xB5 }, ['j'] = new byte[] { 0xB6, 0xA3, 0xBB },
            ['k'] = new byte[] { 0xB6, 0xA3, 0xA9 }, ['l'] = new byte[] { 0xB6, 0xA3, 0xC5 },
            ['m'] = new byte[] { 0xB6, 0xA3, 0xE6 }, ['n'] = new byte[] { 0xB6, 0xB6, 0xA3 },
            ['o'] = new byte[] { 0xB6, 0xB6, 0xB6 }, ['p'] = new byte[] { 0xB6, 0xB6, 0xA4 },
            ['q'] = new byte[] { 0xB6, 0xB6, 0xA2 }, ['r'] = new byte[] { 0xB6, 0xB6, 0xAE },
            ['s'] = new byte[] { 0xB6, 0xB6, 0xB5 }, ['t'] = new byte[] { 0xB6, 0xB6, 0xBB },
            ['u'] = new byte[] { 0xB6, 0xB6, 0xA9 }, ['v'] = new byte[] { 0xB6, 0xB6, 0xC5 },
            ['w'] = new byte[] { 0xB6, 0xB6, 0xE6 }, ['x'] = new byte[] { 0xB6, 0xA4, 0xA3 },
            ['y'] = new byte[] { 0xB6, 0xA4, 0xB6 }, ['z'] = new byte[] { 0xB6, 0xA4, 0xA4 },
            [' '] = new byte[] { 0xA2, 0xA4 }, ['@'] = new byte[] { 0xBB, 0xAE },
            ['_'] = new byte[] { 0xE6, 0xB5 }, ['-'] = new byte[] { 0xAE, 0xB5 },
            ['.'] = new byte[] { 0xAE, 0xBB }
        };

        public static bool Matches(string password, byte[] storedCipherBytes)
        {
            if (password == null || storedCipherBytes == null)
                return false;

            var encoded = new List<byte>(password.Length * 4);
            foreach (var rawCharacter in password)
            {
                byte[] value;
                if (!Table.TryGetValue(char.ToLowerInvariant(rawCharacter), out value))
                    return false;

                encoded.AddRange(value);
                encoded.Add(Separator);
            }

            return encoded.SequenceEqual(storedCipherBytes);
        }
    }
}