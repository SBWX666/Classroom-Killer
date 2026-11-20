using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Classroom_Killer
{
    /// <summary>
    /// 凭据哈希工具：PBKDF2(SHA-256) + 每条记录独立随机盐。
    /// 存储格式：PBKDF2$v1$迭代次数$盐Base64$哈希Base64
    /// 兼容旧数据：32 位十六进制字符串按 MD5 校验（登录成功后可自动升级）。
    /// </summary>
    internal static class PasswordHasher
    {
        private const string Prefix = "PBKDF2$v1$";
        private const int Iterations = 120000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>生成新的凭据记录（注册 / 改用户名 / 改密码时调用）。</summary>
        public static string Hash(string plain)
        {
            if (plain == null) plain = string.Empty;

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Derive(plain, salt, Iterations);

            return Prefix
                 + Iterations.ToString(CultureInfo.InvariantCulture) + "$"
                 + Convert.ToBase64String(salt) + "$"
                 + Convert.ToBase64String(hash);
        }

        /// <summary>判断这条记录是否已经是新格式。</summary>
        public static bool IsCurrentFormat(string stored)
        {
            return !string.IsNullOrEmpty(stored)
                && stored.StartsWith(Prefix, StringComparison.Ordinal);
        }

        /// <summary>校验（新格式走 PBKDF2，旧格式自动回退到 MD5）。</summary>
        public static bool Verify(string plain, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (plain == null) plain = string.Empty;

            if (IsCurrentFormat(stored))
            {
                // PBKDF2 $ v1 $ iter $ salt $ hash
                string[] parts = stored.Split('$');
                if (parts.Length != 5) return false;

                int iterations;
                if (!int.TryParse(parts[2], NumberStyles.Integer,
                                  CultureInfo.InvariantCulture, out iterations)
                    || iterations < 1)
                {
                    return false;
                }

                byte[] salt;
                byte[] expected;
                try
                {
                    salt = Convert.FromBase64String(parts[3]);
                    expected = Convert.FromBase64String(parts[4]);
                }
                catch (FormatException)
                {
                    return false;
                }

                return FixedTimeEquals(Derive(plain, salt, iterations), expected);
            }

            return VerifyLegacyMd5(plain, stored);
        }

        // ------------------------------------------------------------------
        // 内部实现
        // ------------------------------------------------------------------

        private static byte[] Derive(string plain, byte[] salt, int iterations)
        {
            using (var kdf = new Rfc2898DeriveBytes(Utf8.GetBytes(plain), salt,
                                                    iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(HashSize);
            }
        }

        /// <summary>
        /// 旧版 MD5 记录：老程序用的是 Encoding.Default（中文系统 = GBK），
        /// 所以 UTF-8 / 系统默认 / GBK 三种编码都试，任一命中即通过。
        /// </summary>
        private static bool VerifyLegacyMd5(string plain, string storedHex)
        {
            string expected = storedHex.Trim();
            if (expected.Length != 32) return false;

            Encoding[] candidates = { Utf8, Encoding.Default, TryGetGbk() };
            foreach (Encoding enc in candidates)
            {
                if (enc == null) continue;

                string actual = ToHex(Md5(enc.GetBytes(plain)));
                if (string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Encoding TryGetGbk()
        {
            try
            {
                return Encoding.GetEncoding(936);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static byte[] Md5(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>定长比较：耗时与"第几位不同"无关。</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
