using System;
using System.Threading;
using System.Windows.Forms;

namespace CafeManager
{
    static class Program
    {
        // یک نام منحصر‌به‌فرد برای پروژه خود انتخاب کنید
        private static Mutex mutex = new Mutex(true, "CafeManager_Unique_App_Name_12345");

        [STAThread]
        static void Main()
        {
            // بررسی اینکه آیا قبلاً برنامه باز شده است یا خیر
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // ابتدا فرم لاگین را نمایش می‌دهیم
                LoginForm login = new LoginForm();
                if (login.ShowDialog() == DialogResult.OK)
                {
                    // اگر لاگین موفق بود، برنامه اصلی اجرا می‌شود
                    Application.Run(new FormMain());
                }

                mutex.ReleaseMutex();
            }
            else
            {
                // برنامه قبلاً باز شده است
                MessageBox.Show("برنامه در حال حاضر در حال اجرا است.", "توجه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}