using System;
using System.Text;

namespace CafeManager
{
    public static class SecurityHelper
    {
        // یک کلید رمزنگاری ساده (می‌توانید هر چیزی بنویسید)
        private static readonly string SecretKey = "CafeManagerLogKey!";

        // تبدیل متن خوانا به متن رمزنگاری شده خرچنگ‌قورباغه!
        public static string Encrypt(string plainText)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ SecretKey[i % SecretKey.Length]); // عملیات XOR
            }
            return Convert.ToBase64String(bytes);
        }

        // برگرداندن متن رمز شده به متن خوانا (مخصوص نمایش به مدیر)
        public static string Decrypt(string cipherText)
        {
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ SecretKey[i % SecretKey.Length]);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "🚨 خطا در رمزگشایی! فایل دستکاری شده است.";
            }
        }
    }
}