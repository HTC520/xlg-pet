using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace StardewDesktopPet
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                LayeredWindowHelper.SetProcessDPIAware();
            }
            catch
            {
                // Non-fatal on older OS
            }

            if (args != null && args.Length > 0 && args[0] == "--self-test")
            {
                return RunSelfTest();
            }

            bool createdNew;
            using (Mutex singleInstance = new Mutex(true, "StardewDesktopPet_SingleInstance_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new PetWindow());
                return 0;
            }
        }

        private static int RunSelfTest()
        {
            foreach (PetType pt in Enum.GetValues(typeof(PetType)))
            {
                foreach (PetAnim anim in Enum.GetValues(typeof(PetAnim)))
                {
                    Bitmap b0 = PixelSprites.GetPetFrame(pt, anim, 0);
                    Bitmap b1 = PixelSprites.GetPetFrame(pt, anim, 1);
                    int expectedW = PixelSprites.GetFrameWidth(pt);
                    int expectedH = PixelSprites.GetFrameHeight(pt);
                    if (b0 == null || b1 == null || b0.Width != expectedW || b0.Height != expectedH ||
                        b1.Width != expectedW || b1.Height != expectedH)
                    {
                        Console.Error.WriteLine("Sprite validation failed: " + pt + " / " + anim);
                        return 1;
                    }
                }

                PetAction[] actions = PetActions.GetAvailable(pt);
                if (actions == null || actions.Length == 0)
                {
                    Console.Error.WriteLine("Action validation failed: " + pt);
                    return 6;
                }
            }

            foreach (EmoteType em in Enum.GetValues(typeof(EmoteType)))
            {
                Bitmap eb = PixelSprites.GetEmote(em);
                if (eb == null || eb.Width == 0 || eb.Height == 0)
                {
                    Console.Error.WriteLine("Emote validation failed: " + em);
                    return 2;
                }
            }

            PetConfig cfg = PetConfig.Load();
            string fortune = StardewData.GetDailyFortune();
            if (cfg == null || string.IsNullOrEmpty(fortune))
            {
                Console.Error.WriteLine("Config/Data validation failed.");
                return 3;
            }

            for (int slot = 0; slot < 3; slot++)
            {
                Bitmap kb = PixelSprites.GetKeyboardProp(slot);
                if (kb == null || kb.Width != 20 || kb.Height != 7)
                {
                    Console.Error.WriteLine("Keyboard prop validation failed: " + slot);
                    return 4;
                }
            }

            InputStats stats = new InputStats();
            stats.RecordActivity(3, 1, 1, 0);
            if (stats.TodayKeyPresses != 3 || stats.TodayMouseClicks != 2 || stats.GetLast7Days().Count != 7)
            {
                Console.Error.WriteLine("InputStats validation failed.");
                return 5;
            }

            Console.WriteLine("SELF_TEST_OK: 14 pets x 12 anims x 2 frames + 12 emotes + action catalog verified.");
            return 0;
        }
    }
}
