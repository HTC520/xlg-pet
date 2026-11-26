using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace StardewDesktopPet
{
    public enum PetType
    {
        JunimoGreen,
        JunimoPink,
        JunimoBlue,
        JunimoYellow,
        JunimoPurple,
        BlueChicken,
        WhiteChicken,
        SlimeGreen,
        Krobus,
        FarmCat,
        Sebastian,
        Emily,
        Abigail,
        Furnace
    }

    public enum PetAnim
    {
        Idle,
        Walk,
        Happy,
        Sleep,
        Drag,
        Eat,
        Lick,
        LieDown,
        Dance,
        Sit,
        LookUp,
        Coffee
    }

    public enum EmoteType
    {
        Heart,
        Stardrop,
        Music,
        Zzz,
        Parsnip,
        Strawberry,
        VoidMayo,
        Sparkle,
        Hay,
        Ellipsis,
        Dizzy,
        Smile
    }

    public static class PixelSprites
    {
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();

        public static string GetPetDisplayName(PetType pet)
        {
            switch (pet)
            {
                case PetType.JunimoGreen: return "祝尼魔 · 苹果绿";
                case PetType.JunimoPink: return "祝尼魔 · 草莓粉";
                case PetType.JunimoBlue: return "祝尼魔 · 蓝莓蓝";
                case PetType.JunimoYellow: return "祝尼魔 · 柠檬黄";
                case PetType.JunimoPurple: return "祝尼魔 · 葡萄紫";
                case PetType.BlueChicken: return "谢恩的蓝鸡";
                case PetType.WhiteChicken: return "鹈鹕镇白鸡";
                case PetType.SlimeGreen: return "果冻史莱姆";
                case PetType.Krobus: return "科罗布斯";
                case PetType.FarmCat: return "农场猫猫";
                case PetType.Sebastian: return "塞巴斯蒂安";
                case PetType.Emily: return "艾米丽";
                case PetType.Abigail: return "阿比盖尔";
                case PetType.Furnace: return "熔炉 · 烧掉烦恼";
                default: return "祝尼魔";
            }
        }

        public static int GetFrameWidth(PetType pet)
        {
            return pet == PetType.Furnace ? 32 : 16;
        }

        public static int GetFrameHeight(PetType pet)
        {
            return pet == PetType.Sebastian || pet == PetType.Emily ||
                   pet == PetType.Abigail || pet == PetType.Furnace ? 32 : 16;
        }

        private static bool SupportsAnim(PetType pet, PetAnim anim)
        {
            if (pet == PetType.Furnace) return anim == PetAnim.Idle || anim == PetAnim.Happy || anim == PetAnim.Sleep;
            if (pet == PetType.FarmCat) return true;
            if (pet == PetType.Sebastian || pet == PetType.Emily || pet == PetType.Abigail)
            {
                return anim == PetAnim.Idle || anim == PetAnim.Walk || anim == PetAnim.Happy ||
                       anim == PetAnim.Dance || anim == PetAnim.Sit || anim == PetAnim.LookUp ||
                       anim == PetAnim.Coffee;
            }
            return anim == PetAnim.Idle || anim == PetAnim.Walk || anim == PetAnim.Happy ||
                   anim == PetAnim.Sleep || anim == PetAnim.Drag || anim == PetAnim.Eat;
        }

        private static PetAnim FallbackAnim(PetType pet, PetAnim anim)
        {
            if (pet == PetType.Furnace) return PetAnim.Idle;
            if (pet == PetType.Sebastian || pet == PetType.Emily || pet == PetType.Abigail)
            {
                if (anim == PetAnim.Sleep || anim == PetAnim.LieDown || anim == PetAnim.Drag) return PetAnim.Sit;
                if (anim == PetAnim.Lick || anim == PetAnim.Eat) return PetAnim.Coffee;
                return PetAnim.Idle;
            }
            if (anim == PetAnim.Lick || anim == PetAnim.Sit) return PetAnim.Idle;
            if (anim == PetAnim.LieDown) return PetAnim.Sleep;
            if (anim == PetAnim.Dance) return PetAnim.Happy;
            if (anim == PetAnim.LookUp || anim == PetAnim.Coffee) return PetAnim.Idle;
            return PetAnim.Idle;
        }

        public static Bitmap GetPetFrame(PetType pet, PetAnim anim, int frameIndex)
        {
            if (!SupportsAnim(pet, anim)) anim = FallbackAnim(pet, anim);

            int frame = frameIndex & 1;
            string key = string.Format("{0}_{1}_{2}", pet, anim, frame);
            Bitmap bmp;
            if (Cache.TryGetValue(key, out bmp))
            {
                return bmp;
            }

            string[] grid;
            Dictionary<char, Color> palette;

            switch (pet)
            {
                case PetType.JunimoGreen:
                    palette = CreateJunimoPalette("#6CD44C", "#429E2D", "#9DF57A");
                    grid = GetJunimoGrid(anim, frame);
                    break;
                case PetType.JunimoPink:
                    palette = CreateJunimoPalette("#F2799E", "#C44970", "#FFB0C8");
                    grid = GetJunimoGrid(anim, frame);
                    break;
                case PetType.JunimoBlue:
                    palette = CreateJunimoPalette("#5CA2F5", "#356FC2", "#99CAFF");
                    grid = GetJunimoGrid(anim, frame);
                    break;
                case PetType.JunimoYellow:
                    palette = CreateJunimoPalette("#F5D142", "#C79B1C", "#FFF28C");
                    grid = GetJunimoGrid(anim, frame);
                    break;
                case PetType.JunimoPurple:
                    palette = CreateJunimoPalette("#AC72EB", "#7741BA", "#D4A8FF");
                    grid = GetJunimoGrid(anim, frame);
                    break;
                case PetType.BlueChicken:
                    palette = CreateChickenPalette("#60A3EB", "#3874BA", "#9BCEFF");
                    grid = GetChickenGrid(anim, frame);
                    break;
                case PetType.WhiteChicken:
                    palette = CreateChickenPalette("#FFF9EE", "#D9CEB8", "#FFFFFF");
                    grid = GetChickenGrid(anim, frame);
                    break;
                case PetType.SlimeGreen:
                    palette = CreateSlimePalette();
                    grid = GetSlimeGrid(anim, frame);
                    break;
                case PetType.Krobus:
                    palette = CreateKrobusPalette();
                    grid = GetKrobusGrid(anim, frame);
                    break;
                case PetType.FarmCat:
                    palette = CreateCatPalette();
                    grid = GetCatGrid(anim, frame);
                    break;
                case PetType.Sebastian:
                    palette = CreateHumanPalette("#4A3B32", "#2E241F", "#F1C7A4", "#D9A984", "#6C7F9E", "#48596F", "#2E3440", "#20262F", "#C9873F");
                    grid = GetHumanoidGrid(anim, frame);
                    break;
                case PetType.Emily:
                    palette = CreateHumanPalette("#E8B34A", "#B67C26", "#F5CFB0", "#DCAE8B", "#A3385A", "#742744", "#3E6FB0", "#2A4A82", "#F06FA8");
                    grid = GetHumanoidGrid(anim, frame);
                    break;
                case PetType.Abigail:
                    palette = CreateHumanPalette("#7B4FD1", "#523492", "#F3C7A6", "#D7A07D", "#4F7A45", "#33582C", "#3B3346", "#282231", "#F2D25A");
                    grid = GetHumanoidGrid(anim, frame);
                    break;
                case PetType.Furnace:
                    palette = CreateFurnacePalette();
                    grid = GetFurnaceGrid(anim, frame);
                    break;
                default:
                    palette = CreateJunimoPalette("#6CD44C", "#429E2D", "#9DF57A");
                    grid = GetJunimoGrid(anim, frame);
                    break;
            }

            bmp = RenderGrid(grid, palette, GetFrameWidth(pet), GetFrameHeight(pet));
            Cache[key] = bmp;
            return bmp;
        }

        public static Bitmap GetEmote(EmoteType emote)
        {
            string key = "emote_" + emote;
            Bitmap bmp;
            if (Cache.TryGetValue(key, out bmp))
            {
                return bmp;
            }

            string[] grid;
            Dictionary<char, Color> palette = new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#2B1B17") },
                { 'R', ColorTranslator.FromHtml("#F04444") },
                { 'r', ColorTranslator.FromHtml("#B82532") },
                { 'W', ColorTranslator.FromHtml("#FFFFFF") },
                { 'P', ColorTranslator.FromHtml("#A64CE6") },
                { 'p', ColorTranslator.FromHtml("#6E28A8") },
                { 'G', ColorTranslator.FromHtml("#5CC445") },
                { 'g', ColorTranslator.FromHtml("#3A8C29") },
                { 'Y', ColorTranslator.FromHtml("#FFD84D") },
                { 'y', ColorTranslator.FromHtml("#D99B26") },
                { 'B', ColorTranslator.FromHtml("#4B9CF5") },
                { 'C', ColorTranslator.FromHtml("#F5DFB0") },
                { 'c', ColorTranslator.FromHtml("#CCA86E") },
                { 'K', ColorTranslator.FromHtml("#2B2638") },
                { 'k', ColorTranslator.FromHtml("#4B4361") }
            };

            switch (emote)
            {
                case EmoteType.Heart:
                    grid = new[]
                    {
                        "............",
                        "..OOO..OOO..",
                        ".ORWROORWRO.",
                        ".ORRRORRRRO.",
                        ".ORRRRRRRrO.",
                        "..ORRRRRrO..",
                        "...ORRRrO...",
                        "....ORrO....",
                        ".....OO.....",
                        "............"
                    };
                    break;
                case EmoteType.Stardrop:
                    grid = new[]
                    {
                        ".....OO.....",
                        "....OGGO....",
                        "...OGggGO...",
                        "..OOOPPOOO..",
                        ".OPWWPPPPpO.",
                        "OPWPPPPPPPpO",
                        "OPPPPPPPPPpO",
                        ".OPPPPPPPpO.",
                        "..OPPPPPpO..",
                        "...OOPPOO...",
                        ".....OO....."
                    };
                    break;
                case EmoteType.Strawberry:
                    grid = new[]
                    {
                        "....OOOO....",
                        "...OGggGO...",
                        "..OOGggGOO..",
                        ".ORRWYRRRrO.",
                        ".ORRRRRWRrO.",
                        ".ORYRRRRYrO.",
                        "..ORRRWRrO..",
                        "...ORRYrO...",
                        "....ORrO....",
                        ".....OO....."
                    };
                    break;
                case EmoteType.Parsnip:
                    grid = new[]
                    {
                        "...O..O..O..",
                        "..OGOOGOGO..",
                        "...OGggGO...",
                        "....OOOO....",
                        "...OCWWcO...",
                        "...OCCCcO...",
                        "....OCCcO...",
                        "....OCcO....",
                        ".....OcO....",
                        ".....OO....."
                    };
                    break;
                case EmoteType.VoidMayo:
                    grid = new[]
                    {
                        "...OOOOOO...",
                        "..ORRRRRRO..",
                        "...OOOOOO...",
                        "..OKkKKKKO..",
                        ".OKWKKKKKKO.",
                        ".OKKRRRRKKO.",
                        ".OKKKKKKKKO.",
                        ".OKKKKKKKKO.",
                        "..OOOOOOOO.."
                    };
                    break;
                case EmoteType.Music:
                    grid = new[]
                    {
                        ".....OOOOO..",
                        "....OBBBBB..",
                        "....OB...O..",
                        "....OB...O..",
                        "..OOOB..OO..",
                        ".OBBBOBBBBO.",
                        ".OBBBOBBBBO.",
                        "..OOO..OOO.."
                    };
                    break;
                case EmoteType.Zzz:
                    grid = new[]
                    {
                        "......OOOO..",
                        ".......OBO..",
                        "......OBO...",
                        "..OOOOOOO...",
                        "...OBO......",
                        "..OBO.......",
                        "..OOOO......"
                    };
                    break;
                case EmoteType.Hay:
                    grid = new[]
                    {
                        "....OOOO....",
                        "..OOYYYYOO..",
                        ".OYYyYYyYYO.",
                        "OYYyYYYYyYYO",
                        "OYYyYyyYyYYO",
                        "OYyYYYYYYyYO",
                        ".OyyyyyyyyO.",
                        "..OOOOOOOO.."
                    };
                    break;
                case EmoteType.Ellipsis:
                    grid = new[]
                    {
                        "..OOOOOOOO..",
                        ".OWWWWWWWWO.",
                        ".OWKWWKWWKO.",
                        ".OWWWWWWWWO.",
                        "..OOOOOOOO..",
                        "....OOO.....",
                        "....OO......"
                    };
                    break;
                case EmoteType.Dizzy:
                    grid = new[]
                    {
                        "..OOOOOO....",
                        ".OYYYYYYOO..",
                        "OYYOOYYYYYO.",
                        "OYOYYOYYOYO.",
                        "OYYOOYYYYYO.",
                        ".OYYYYYYYO..",
                        "...OOOOOO..."
                    };
                    break;
                case EmoteType.Smile:
                    grid = new[]
                    {
                        "..OOOOOOOO..",
                        ".OYYYYYYYYO.",
                        ".OYKYYYYKYO.",
                        ".OYRYYYYRYO.",
                        ".OYYKYYKYYO.",
                        ".OYYYKKYYYO.",
                        "..OOOOOOOO.."
                    };
                    break;
                default: // Sparkle
                    grid = new[]
                    {
                        "....OO....",
                        "....YY....",
                        "..O.YY.O..",
                        "..YYYYYY..",
                        "OOYYYYYYOO",
                        "..YYYYYY..",
                        "..O.YY.O..",
                        "....YY....",
                        "....OO...."
                    };
                    break;
            }

            bmp = RenderGrid(grid, palette);
            Cache[key] = bmp;
            return bmp;
        }

        public static Bitmap GetKeyboardProp(int activeSlot)
        {
            int slot = ((activeSlot % 3) + 3) % 3;
            string key = "prop_keyboard_" + slot;
            Bitmap bmp;
            if (Cache.TryGetValue(key, out bmp))
            {
                return bmp;
            }

            Dictionary<char, Color> palette = new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#3A1E0D") }, // Dark wood outline
                { 'W', ColorTranslator.FromHtml("#9E5B28") }, // Wood frame
                { 'w', ColorTranslator.FromHtml("#733D16") }, // Wood shade
                { 'K', ColorTranslator.FromHtml("#FFF3D6") }, // Cream keycap
                { 'k', ColorTranslator.FromHtml("#DEC79B") }, // Keycap shadow
                { 'A', ColorTranslator.FromHtml("#6CE055") }, // Lit active keycap
                { 'a', ColorTranslator.FromHtml("#3CA829") }  // Active keycap shade
            };

            // 20x7 crisp retro wooden mechanical keyboard
            string rowKeysTop = slot == 0
                ? "OWAAKWKKWKKWKKWKKWKO"
                : (slot == 1 ? "OWKKWKKWAAKWKKWKKWKO" : "OWKKWKKWKKWKKWAAKWKO");
            string rowKeysBot = slot == 0
                ? "OwaakKKKKKKKKKKKKkkO"
                : (slot == 1 ? "OwkkkKKaakKKKKKKKkkO" : "OwkkkKKKKKKKKKaakkkO");

            string[] grid = new[]
            {
                ".OOOOOOOOOOOOOOOOOO.",
                "OWWWWWWWWWWWWWWWWWWO",
                rowKeysTop,
                rowKeysBot,
                "OWWKKKKKKKKKKKKKKWWO",
                "OwwwwwwwwwwwwwwwwwwO",
                ".OOOOOOOOOOOOOOOOOO."
            };

            bmp = RenderGrid(grid, palette);
            Cache[key] = bmp;
            return bmp;
        }

        private static Dictionary<char, Color> CreateJunimoPalette(string main, string shade, string highlight)
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#2B1B17") }, // Dark outline
                { 'M', ColorTranslator.FromHtml(main) },      // Body main
                { 'S', ColorTranslator.FromHtml(shade) },     // Body shade
                { 'H', ColorTranslator.FromHtml(highlight) }, // Body highlight
                { 'L', ColorTranslator.FromHtml("#58B83C") }, // Leaf green
                { 'l', ColorTranslator.FromHtml("#397D26") }, // Stem/leaf dark
                { 'E', ColorTranslator.FromHtml("#2B1B17") }, // Eye
                { 'C', ColorTranslator.FromHtml("#FF9CA8") }  // Blush cheek
            };
        }

        private static Dictionary<char, Color> CreateChickenPalette(string main, string shade, string highlight)
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#2B1B17") }, // Outline
                { 'M', ColorTranslator.FromHtml(main) },      // Body
                { 'S', ColorTranslator.FromHtml(shade) },     // Wing/shadow
                { 'H', ColorTranslator.FromHtml(highlight) }, // Highlight
                { 'R', ColorTranslator.FromHtml("#E64040") }, // Red comb/wattle
                { 'B', ColorTranslator.FromHtml("#F59E2D") }, // Orange beak/feet
                { 'E', ColorTranslator.FromHtml("#231715") }, // Eye
                { 'C', ColorTranslator.FromHtml("#FF9AA8") }  // Cheek
            };
        }

        private static Dictionary<char, Color> CreateSlimePalette()
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#1D3D22") },
                { 'M', ColorTranslator.FromHtml("#55CF5E") },
                { 'S', ColorTranslator.FromHtml("#349940") },
                { 'H', ColorTranslator.FromHtml("#A6FF9E") },
                { 'W', ColorTranslator.FromHtml("#FFFFFF") },
                { 'E', ColorTranslator.FromHtml("#1A211B") },
                { 'C', ColorTranslator.FromHtml("#FF8CA3") }
            };
        }

        private static Dictionary<char, Color> CreateKrobusPalette()
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#110F17") },
                { 'M', ColorTranslator.FromHtml("#292536") },
                { 'S', ColorTranslator.FromHtml("#1B1826") },
                { 'H', ColorTranslator.FromHtml("#443E59") },
                { 'W', ColorTranslator.FromHtml("#F5F2FF") },
                { 'E', ColorTranslator.FromHtml("#FFFFFF") }
            };
        }

        private static string[] GetJunimoGrid(PetAnim anim, int frame)
        {
            if (anim == PetAnim.Happy)
            {
                if (frame == 0)
                {
                    return new[]
                    {
                        ".......OO.......",
                        "......OlLO......",
                        ".......OlO......",
                        "....OOOOOOOO....",
                        ".OOOHMMMMMMMO...",
                        "OMOMHHMMMMMMMSO.",
                        ".OOMMMMMMMMMMMSO",
                        "..OMEMMMMMMEMSO.",
                        "..OCMMMMMMMMCSO.",
                        "..OMMMMMMMMMMSO.",
                        "...OSSSSSSSSSO..",
                        "....OOOOOOOOO...",
                        "....OO....OO....",
                        "....OO....OO....",
                        "................",
                        "................"
                    };
                }
                return new[]
                {
                    "......OlLO......",
                    ".......OlO......",
                    "....OOOOOOOO....",
                    ".OOOHMMMMMMMOOO.",
                    "OMOMHHMMMMMMMSMO",
                    ".OOMMMMMMMMMMMSO",
                    "..OMEMMMMMMEMSO.",
                    "..OCMMMMMMMMCSO.",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "....OOOOOOOOO...",
                    "...OO......OO...",
                    "...OO......OO...",
                    "................",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Sleep)
            {
                return new[]
                {
                    "................",
                    "................",
                    ".......OO.......",
                    "......OlLO......",
                    ".......OlO......",
                    "....OOOOOOOO....",
                    "...OHMMMMMMMO...",
                    "..OMHHMMMMMMMSO.",
                    "..OMMMMMMMMMMMSO",
                    ".OOMEEEMMMMEEMSO",
                    ".OMMMMMMMMMMMMSO",
                    ".OOCMMMMMMMMCSO.",
                    "..OSSSSSSSSSSSO.",
                    "...OOOOOOOOOOO..",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Drag)
            {
                if (frame == 0)
                {
                    return new[]
                    {
                        ".......OO.......",
                        "......OlLO......",
                        ".......OlO......",
                        "....OOOOOOOO....",
                        "...OHMMMMMMMO...",
                        ".OOMHHMMMMMMMSO.",
                        "OMMMMMMMMMMMMMOO",
                        ".OOMEMMMMMMEMSO.",
                        "..OCMMMMMMMMCSO.",
                        "..OMMMMMMMMMMSO.",
                        "...OSSSSSSSSSO..",
                        "....OOOOOOOOO...",
                        "...OO.......OO..",
                        "..OO.........OO.",
                        "................",
                        "................"
                    };
                }
                return new[]
                {
                    ".......OO.......",
                    "......OlLO......",
                    ".......OlO......",
                    "....OOOOOOOO....",
                    "...OHMMMMMMMO...",
                    ".OOMHHMMMMMMMSO.",
                    "OMMMMMMMMMMMMMOO",
                    ".OOMEMMMMMMEMSO.",
                    "..OCMMMMMMMMCSO.",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "....OOOOOOOOO...",
                    "....OO.....OO...",
                    ".....OO...OO....",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Walk)
            {
                if (frame == 0)
                {
                    return new[]
                    {
                        "................",
                        ".......OO.......",
                        "......OlLO......",
                        ".......OlO......",
                        "....OOOOOOOO....",
                        "...OHMMMMMMMO...",
                        "..OMHHMMMMMMMSO.",
                        ".OOMMMMMMMMMMMSO",
                        "OMOMEMMMMMMEMSMO",
                        ".OOCMMMMMMMMCSO.",
                        "..OMMMMMMMMMMSO.",
                        "...OSSSSSSSSSO..",
                        "....OOOOOOOOO...",
                        "...OO......OO...",
                        "...OO...........",
                        "................"
                    };
                }
                return new[]
                {
                    "................",
                    ".......OO.......",
                    "......OlLO......",
                    ".......OlO......",
                    "....OOOOOOOO....",
                    "...OHMMMMMMMO...",
                    "..OMHHMMMMMMMSO.",
                    ".OOMMMMMMMMMMMSO",
                    "OMOMEMMMMMMEMSMO",
                    ".OOCMMMMMMMMCSO.",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "....OOOOOOOOO...",
                    "....OO......OO..",
                    "............OO..",
                    "................"
                };
            }

            // Idle / Eat
            if (frame == 0)
            {
                return new[]
                {
                    "................",
                    ".......OO.......",
                    "......OlLO......",
                    ".......OlO......",
                    "....OOOOOOOO....",
                    "...OHMMMMMMMO...",
                    "..OMHHMMMMMMMSO.",
                    ".OOMMMMMMMMMMMSO",
                    "OMOMEMMMMMMEMSMO",
                    ".OOCMMMMMMMMCSO.",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "....OOOOOOOOO...",
                    "....OO....OO....",
                    "....OO....OO....",
                    "................"
                };
            }
            return new[]
            {
                "................",
                "................",
                ".......OO.......",
                "......OlLO......",
                ".......OlO......",
                "....OOOOOOOO....",
                "...OHMMMMMMMO...",
                "..OMHHMMMMMMMSO.",
                ".OOMMMMMMMMMMMSO",
                "OMOMEMMMMMMEMSMO",
                ".OOCMMMMMMMMCSO.",
                "..OMMMMMMMMMMSO.",
                "...OSSSSSSSSSO..",
                "....OOOOOOOOO...",
                "....OO....OO....",
                "................"
            };
        }

        private static string[] GetChickenGrid(PetAnim anim, int frame)
        {
            if (anim == PetAnim.Sleep)
            {
                return new[]
                {
                    "................",
                    "................",
                    "................",
                    ".....OOO........",
                    "....ORRRO.......",
                    "....OOOOOO......",
                    "...OHMMMMMO.....",
                    "..OMEMMMMMMO....",
                    ".OBOMCMMMSSMO...",
                    "..OOMMMMMSSSMO..",
                    "..OMMMMMMSSSMO..",
                    "...OSSSSSSSSO...",
                    "....OOOOOOOO....",
                    "................",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Eat)
            {
                if (frame == 0)
                {
                    return new[]
                    {
                        "................",
                        "......OOO.......",
                        ".....ORRRO......",
                        ".....OOOOOO.....",
                        "....OHMMMMMO....",
                        "...OMEMMMMMMO...",
                        "..OBOMCMMMMMOO..",
                        "...OOMMMSSSMMMO.",
                        "....OMMMSSSMMMO.",
                        "....OMMMSSSMMO..",
                        ".....OSSSSSSO...",
                        "......OOOOOO....",
                        "......OB..OB....",
                        ".....OBB..OBB...",
                        "................",
                        "................"
                    };
                }
                return new[]
                {
                    "................",
                    "................",
                    ".....OOO........",
                    "....ORRRO.......",
                    "....OOOOOO......",
                    "...OHMMMMMO.....",
                    "..OMEMMMMMMO....",
                    ".OBOMCMMMMMOO...",
                    "..OOMMMSSSMMMO..",
                    "...OMMMSSSMMMO..",
                    "....OSSSSSSO....",
                    ".....OOOOOO.....",
                    ".....OB..OB.....",
                    "....OBB..OBB....",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Walk || anim == PetAnim.Drag || anim == PetAnim.Happy)
            {
                if (frame == 0)
                {
                    return new[]
                    {
                        ".....OOO........",
                        "....ORRRO.......",
                        "....OOOOOO......",
                        "...OHMMMMMO.....",
                        "..OMEMMMMMMO....",
                        ".OBOMCMMMMMOO...",
                        "..OOMMMSSSMMMO..",
                        "...OMMMSSSMMMO..",
                        "...OMMMSSSMMO...",
                        "....OSSSSSSO....",
                        ".....OOOOOO.....",
                        ".....OB..OB.....",
                        "....OBB.........",
                        "................",
                        "................",
                        "................"
                    };
                }
                return new[]
                {
                    "................",
                    ".....OOO........",
                    "....ORRRO.......",
                    "....OOOOOO......",
                    "...OHMMMMMO.....",
                    "..OMEMMMMMMO....",
                    ".OBOMCMMMMMOO...",
                    "..OOMMMSSSMMMO..",
                    "...OMMMSSSMMMO..",
                    "...OMMMSSSMMO...",
                    "....OSSSSSSO....",
                    ".....OOOOOO.....",
                    ".....OB..OB.....",
                    ".........OBB....",
                    "................",
                    "................"
                };
            }

            // Idle
            if (frame == 0)
            {
                return new[]
                {
                    ".....OOO........",
                    "....ORRRO.......",
                    "....OOOOOO......",
                    "...OHMMMMMO.....",
                    "..OMEMMMMMMO....",
                    ".OBOMCMMMMMOO...",
                    "..OOMMMSSSMMMO..",
                    "...OMMMSSSMMMO..",
                    "...OMMMSSSMMO...",
                    "....OSSSSSSO....",
                    ".....OOOOOO.....",
                    ".....OB..OB.....",
                    "....OBB..OBB....",
                    "................",
                    "................",
                    "................"
                };
            }
            return new[]
            {
                "................",
                ".....OOO........",
                "....ORRRO.......",
                "....OOOOOO......",
                "...OHMMMMMO.....",
                "..OMMMMEBMMO....",
                ".OBOMCMMMMMOO...",
                "..OOMMMSSSMMMO..",
                "...OMMMSSSMMMO..",
                "...OMMMSSSMMO...",
                "....OSSSSSSO....",
                ".....OOOOOO.....",
                ".....OB..OB.....",
                "....OBB..OBB....",
                "................",
                "................"
            };
        }

        private static string[] GetSlimeGrid(PetAnim anim, int frame)
        {
            if (anim == PetAnim.Happy || (anim == PetAnim.Walk && frame == 0))
            {
                return new[]
                {
                    "......OO........",
                    ".....OHO........",
                    "......OO........",
                    "....OOOOOO......",
                    "...OHWMMMMO.....",
                    "..OHMMMMMMMSO...",
                    "..OMEMMMMEMSO...",
                    "..OCMMMMMMCSO...",
                    "..OMMMMMMMMSO...",
                    "...OSSSSSSSO....",
                    "....OOOOOOO.....",
                    "................",
                    "................",
                    "................",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Sleep || frame == 1)
            {
                return new[]
                {
                    "................",
                    "................",
                    "......OO........",
                    ".....OHO........",
                    "......OO........",
                    "...OOOOOOOO.....",
                    "..OHWMMMMMMO....",
                    ".OHMMMMMMMMMSO..",
                    ".OMEEEMMMEEMSO..",
                    ".OCMMMMMMMMCSO..",
                    ".OMMMMMMMMMMSO..",
                    "..OSSSSSSSSSO...",
                    "...OOOOOOOOO....",
                    "................",
                    "................",
                    "................"
                };
            }

            return new[]
            {
                "................",
                "......OO........",
                ".....OHO........",
                "......OO........",
                "....OOOOOO......",
                "...OHWMMMMO.....",
                "..OHMMMMMMMSO...",
                "..OMEMMMMEMSO...",
                "..OCMMMMMMCSO...",
                "..OMMMMMMMMSO...",
                "..OSSSSSSSSSO...",
                "...OOOOOOOOO....",
                "................",
                "................",
                "................",
                "................"
            };
        }

        private static string[] GetKrobusGrid(PetAnim anim, int frame)
        {
            if (anim == PetAnim.Happy)
            {
                return new[]
                {
                    ".....OOOOOO.....",
                    "...OOHMMMMHOO...",
                    "..OMMMMMMMMMMO..",
                    "..OMMMMEEMMMMO..",
                    ".OOMMEEMMMMEEMOO",
                    "OMMMMMMMMMMMMMMO",
                    ".OOMMMWWWWMMMSO.",
                    "..OMMMMMMMMMMSO.",
                    "..OMMMMMMMMMMSO.",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "...OOSOOOOOSOO..",
                    "................",
                    "................",
                    "................",
                    "................"
                };
            }

            if (anim == PetAnim.Sleep)
            {
                return new[]
                {
                    "................",
                    "................",
                    ".....OOOOOO.....",
                    "...OOHMMMMHOO...",
                    "..OMMMMMMMMMMO..",
                    "..OMMMMMMMMMMO..",
                    "..OMMWWMMMWWMO..",
                    "..OMMMMMMMMMMO..",
                    "..OMMMWWWWMMMO..",
                    "..OMMMMMMMMMMO..",
                    "...OSSSSSSSSO...",
                    "....OOOOOOOO....",
                    "................",
                    "................",
                    "................",
                    "................"
                };
            }

            if (frame == 0)
            {
                return new[]
                {
                    "................",
                    ".....OOOOOO.....",
                    "...OOHMMMMHOO...",
                    "..OMMMMMMMMMMO..",
                    "..OMMEEMMMMEEMO.",
                    "..OMMEEMMMMEEMO.",
                    "..OMMMMMMMMMMSO.",
                    "..OMMMWWWWMMMSO.",
                    ".OOMMMMMMMMMMSOO",
                    "OMMMMMMMMMMMMMMO",
                    "..OMMMMMMMMMMSO.",
                    "...OSSSSSSSSSO..",
                    "...OOSOOOOOSOO..",
                    "................",
                    "................",
                    "................"
                };
            }

            return new[]
            {
                "................",
                "................",
                ".....OOOOOO.....",
                "...OOHMMMMHOO...",
                "..OMMMMMMMMMMO..",
                "..OMMEEMMMMEEMO.",
                "..OMMEEMMMMEEMO.",
                "..OMMMMMMMMMMSO.",
                "..OMMMWWWWMMMSO.",
                ".OOMMMMMMMMMMSOO",
                "OMMMMMMMMMMMMMMO",
                "..OMMMMMMMMMMSO.",
                "...OSSSSSSSSSO..",
                "...OOSOOOOOSOO..",
                "................",
                "................"
            };
        }

        private static string PadRow(string row, int width)
        {
            if (row == null) return new string('.', width);
            if (row.Length == width) return row;
            if (row.Length > width) return row.Substring(0, width);
            return row + new string('.', width - row.Length);
        }

        private static string[] NormalizeGrid(string[] grid, int width, int height)
        {
            string[] result = new string[height];
            for (int y = 0; y < height; y++)
            {
                result[y] = y < grid.Length ? PadRow(grid[y], width) : new string('.', width);
            }
            return result;
        }

        private static string[] CombineRows(params string[][] groups)
        {
            List<string> rows = new List<string>();
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null) continue;
                rows.AddRange(groups[i]);
            }
            return rows.ToArray();
        }

        private static Dictionary<char, Color> CreateCatPalette()
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#3A2417") },
                { 'F', ColorTranslator.FromHtml("#E8A054") },
                { 'f', ColorTranslator.FromHtml("#B96E2F") },
                { 'H', ColorTranslator.FromHtml("#FFD9A0") },
                { 'W', ColorTranslator.FromHtml("#FFF3DE") },
                { 'E', ColorTranslator.FromHtml("#2A1B12") },
                { 'C', ColorTranslator.FromHtml("#F28B8B") },
                { 'N', ColorTranslator.FromHtml("#D95F73") },
                { 'T', ColorTranslator.FromHtml("#8A4F22") },
                { 'U', ColorTranslator.FromHtml("#8C5A32") }
            };
        }

        private static Dictionary<char, Color> CreateHumanPalette(
            string hair, string hairShade, string skin, string skinShade,
            string clothes, string clothesShade, string pants, string pantsShade, string accent)
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#2B1B17") },
                { 'H', ColorTranslator.FromHtml(hair) },
                { 'h', ColorTranslator.FromHtml(hairShade) },
                { 'S', ColorTranslator.FromHtml(skin) },
                { 's', ColorTranslator.FromHtml(skinShade) },
                { 'E', ColorTranslator.FromHtml("#2B1B17") },
                { 'C', ColorTranslator.FromHtml(clothes) },
                { 'c', ColorTranslator.FromHtml(clothesShade) },
                { 'P', ColorTranslator.FromHtml(pants) },
                { 'p', ColorTranslator.FromHtml(pantsShade) },
                { 'B', ColorTranslator.FromHtml("#4A3529") },
                { 'W', ColorTranslator.FromHtml("#FFFFFF") },
                { 'A', ColorTranslator.FromHtml(accent) },
                { 'U', ColorTranslator.FromHtml("#8C5A32") }
            };
        }

        private static Dictionary<char, Color> CreateFurnacePalette()
        {
            return new Dictionary<char, Color>
            {
                { 'O', ColorTranslator.FromHtml("#231A17") },
                { 'G', ColorTranslator.FromHtml("#8C8C8C") },
                { 'g', ColorTranslator.FromHtml("#5C5C5C") },
                { 'L', ColorTranslator.FromHtml("#BDBDBD") },
                { 'D', ColorTranslator.FromHtml("#1A1210") },
                { 'F', ColorTranslator.FromHtml("#F27A22") },
                { 'f', ColorTranslator.FromHtml("#C34A12") },
                { 'Y', ColorTranslator.FromHtml("#FFD24A") },
                { 'W', ColorTranslator.FromHtml("#FFF6C8") }
            };
        }

        private static string[] GetCatGrid(PetAnim anim, int frame)
        {
            string[] head =
            {
                "..OO......OO..",
                ".OFHO....OHFO.",
                ".OFFOOOOOOFFO.",
                "OFFFHHHHHHFFFO",
                "OFEFFFWWFFFEFO",
                "OFFFWWWNWWFFFO",
                ".OFFFWWWWFFFO.",
                "..OFFFFFFFFO..",
                ".OTFFFFFFFFTO."
            };

            string[] standLegs = frame == 0
                ? new[] { "..OFFFFFFFFO..", "..OFO....OFO..", "..OOO....OOO.." }
                : new[] { "..OFFFFFFFFO..", "...OFO..OFO...", "...OOO..OOO..." };

            if (anim == PetAnim.Sleep || anim == PetAnim.LieDown)
            {
                string[] flat =
                {
                    "..............",
                    "..............",
                    "..............",
                    "......OO......",
                    ".....OFHO.....",
                    "...OOOFFOOOO..",
                    "..OFFEWWFFFO..",
                    "..OFFFWWWFFFO.",
                    ".OOFFFFFFFFOO.",
                    "OTFFFFFFFFFFTO",
                    ".Oggggggggggo.".Replace('g', 'f'),
                    "..OOOOOOOOOO.."
                };
                return flat;
            }

            if (anim == PetAnim.Sit)
            {
                string[] sit =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFEFFFWWFFFEFO",
                    "OFFFWWWNWWFFFO",
                    ".OFFFWWWWFFFO.",
                    "..OFFFFFFFFO..",
                    ".OTFFFFFFFFTO.",
                    "..OFFFFFFFFO..",
                    "..OFFFFFFFFO..",
                    "...OFFFFFFFO..",
                    "...OOO..OOO..."
                };
                return sit;
            }

            if (anim == PetAnim.Lick || anim == PetAnim.Eat)
            {
                string[] lick =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFFFEFFEFFFO..",
                    ".OFFWWWWWFFO..",
                    "..OFWWCNWFFO..",
                    "..OFFFFFFFFO..",
                    ".OTFFFFFFFFTO.",
                    "..OFFFFFFFFO..",
                    "..OFO....OFO..",
                    "..OOO....OOO.."
                };
                return lick;
            }

            if (anim == PetAnim.Coffee)
            {
                string[] coffee =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFEFFFWWFFFEO.",
                    "OFFFWWWNWWFFFO",
                    ".OFFFWWWWFFFO.",
                    "..OFFFFFFFFO..",
                    ".OTFFFFFFFFTO.",
                    "..OFFFFFFFFO..",
                    "..OFO..UUUOFO.",
                    "..OOO..UUUOOO."
                };
                return coffee;
            }

            if (anim == PetAnim.LookUp)
            {
                string[] look =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFEEFFFWWFFEEO",
                    "OFFFWWWNWWFFFO",
                    ".OFFFWWWWFFFO.",
                    "..OFFFFFFFFO..",
                    ".OTFFFFFFFFTO.",
                    "..OFFFFFFFFO..",
                    "..OFO....OFO..",
                    "..OOO....OOO.."
                };
                return look;
            }

            if (anim == PetAnim.Happy || (anim == PetAnim.Dance && frame == 1))
            {
                string[] jump =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFEFFFWWFFFEFO",
                    "OFFFWWWNWWFFFO",
                    ".OFFFWWWWFFFO.",
                    "..OFFFFFFFFO..",
                    ".OTFFFFFFFFTO.",
                    "..OFFFFFFFFO..",
                    "..OFO....OFO..",
                    "..OOO....OOO.."
                };
                return jump;
            }

            if (anim == PetAnim.Drag)
            {
                string[] drag =
                {
                    "..OO......OO..",
                    ".OFHO....OHFO.",
                    ".OFFOOOOOOFFO.",
                    "OFFFHHHHHHFFFO",
                    "OFEFFFWWFFFEFO",
                    "OFFFWWWNWWFFFO",
                    ".OFFFWWWWFFFO.",
                    ".OFFFFFFFFFFO.",
                    "OF..FFFFFFFFOF",
                    "O....FFFFFF..O"
                };
                return drag;
            }

            return CombineRows(head, standLegs);
        }

        private static string[] GetHumanoidGrid(PetAnim anim, int frame)
        {
            string[] blank = { "................" };
            string[] head =
            {
                ".....OHHHHH.....",
                "....OHHHHHHO....",
                "...OHHHHHHHHO...",
                "...OHHHHHHHHO...",
                "...OHSSEESSEO...",
                "...OHSSSSSSHO...",
                "...OHSCSSCSHO...",
                "....OSSSSSO.....",
                ".....OOOOO......"
            };

            string[] torso =
            {
                "....OCCCCCCCO...",
                "...OCCCCCCCCAO..",
                "...OCCCCCCCAAO..",
                "...OCCCCCAAAAO..",
                "...OCCCCCCCCCO..",
                "....OCCCCCCCO...",
                "....OPPPPPPO...."
            };

            string[] legsIdle =
            {
                "....OPPPPPPO....",
                "....OPPPPPPO....",
                "....OP.PP.PO....",
                "....OB....BO....",
                "....OB....BO...."
            };

            string[] legsWalk = frame == 0
                ? new[]
                {
                    "....OPPPPO......",
                    "...OPPPPPPO.....",
                    "...OP....PO.....",
                    "..OB......BO....",
                    "..OB......BO...."
                }
                : new[]
                {
                    "......OPPPPO....",
                    ".....OPPPPPPO...",
                    ".....PO....OP...",
                    "....BO......OB..",
                    "....BO......OB.."
                };

            string[] legsSit =
            {
                "....OPPPPPPO....",
                "....OPPPPPPO....",
                "....OPP..PPO....",
                "....OBO..OBO....",
                ".....O....O....."
            };

            string[] legsDance = frame == 0
                ? new[]
                {
                    "....OPPPPPPO....",
                    "...OPPPPPPO.....",
                    "..OP......PO....",
                    "..OB......BO....",
                    "....OB....BO...."
                }
                : new[]
                {
                    "....OPPPPPPO....",
                    ".....OPPPPPPO...",
                    "....PO......OP..",
                    "....BO......OB..",
                    "..OB......BO...."
                };

            string[] result;
            switch (anim)
            {
                case PetAnim.Walk:
                    result = CombineRows(blank, blank, head, torso, legsWalk);
                    break;
                case PetAnim.Dance:
                    result = CombineRows(blank, blank, head, torso, legsDance);
                    break;
                case PetAnim.Sit:
                case PetAnim.Sleep:
                    result = CombineRows(blank, blank, blank, head, torso, legsSit);
                    break;
                case PetAnim.Coffee:
                    string[] cup =
                    {
                        "....OPPPPPPO....",
                        "....OPPPPPPO....",
                        "....OP.PP.PO....",
                        "....OB....BO....",
                        "....OB....BO...."
                    };
                    string[] withCup = CombineRows(blank, blank, head, torso, cup);
                    // 在右侧画一只咖啡杯
                    for (int i = 0; i < withCup.Length; i++)
                    {
                        if (i >= 19 && i <= 21)
                        {
                            char[] chars = PadRow(withCup[i], 16).ToCharArray();
                            chars[12] = 'U';
                            chars[13] = 'U';
                            withCup[i] = new string(chars);
                        }
                    }
                    result = withCup;
                    break;
                case PetAnim.LookUp:
                    string[] look = (string[])head.Clone();
                    look[2] = "...OHEEHHHHHO...";
                    look[3] = "...OHEEHHHHHO...";
                    result = CombineRows(blank, blank, look, torso, legsIdle);
                    break;
                case PetAnim.Drag:
                    result = CombineRows(blank, head, torso, legsDance);
                    break;
                default:
                    string[] happyHead = (string[])head.Clone();
                    happyHead[5] = "...OHSSEESSEHO...";
                    result = CombineRows(blank, blank, happyHead, torso, legsIdle);
                    break;
            }
            return result;
        }

        private static string[] GetFurnaceGrid(PetAnim anim, int frame)
        {
            char[,] g = new char[32, 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    g[y, x] = '.';
                }
            }

            // 石砌炉身
            for (int y = 3; y <= 29; y++)
            {
                for (int x = 4; x <= 27; x++)
                {
                    char c = 'G';
                    if (y == 3 || y == 29 || x == 4 || x == 27) c = 'O';
                    else if (y == 4) c = 'L';
                    else if (y % 5 == 4) c = 'g';
                    else if ((x + (y / 5) * 3) % 9 == 4) c = 'g';
                    g[y, x] = c;
                }
            }

            // 拱形炉膛
            for (int y = 12; y <= 29; y++)
            {
                int half = y < 18 ? 3 + (y - 12) : 8;
                for (int x = 16 - half; x <= 15 + half; x++)
                {
                    bool edge = (x == 16 - half || x == 15 + half || y == 12);
                    g[y, x] = edge ? (char)'O' : (char)'D';
                }
            }

            // 跳动的火焰
            int flameFrame = ((anim == PetAnim.Happy || anim == PetAnim.Sleep) ? frame + 2 : frame) & 3;
            int flameHeight = 9 + flameFrame;
            for (int step = 0; step < flameHeight; step++)
            {
                int y = 28 - step;
                int w = Math.Max(1, step / 2);
                int sway = ((step + flameFrame) % 3) - 1;
                for (int dx = -w; dx <= w; dx++)
                {
                    int x = 15 + dx + sway;
                    if (x < 9 || x > 22) continue;
                    if (g[y, x] != 'D') continue;
                    g[y, x] = step < 3 ? 'W' : (step < 6 ? 'Y' : (dx == 0 ? 'Y' : 'F'));
                }
            }

            string[] grid = new string[32];
            for (int y = 0; y < 32; y++)
            {
                char[] row = new char[32];
                for (int x = 0; x < 32; x++)
                {
                    row[x] = g[y, x];
                }
                grid[y] = new string(row);
            }
            return grid;
        }

        private static Bitmap RenderGrid(string[] grid, Dictionary<char, Color> palette)
        {
            return RenderGrid(grid, palette, 0, 0);
        }

        private static Bitmap RenderGrid(string[] grid, Dictionary<char, Color> palette, int width, int height)
        {
            if (width <= 0)
            {
                width = 0;
                for (int y = 0; y < grid.Length; y++)
                {
                    if (grid[y].Length > width) width = grid[y].Length;
                }
            }
            if (height <= 0) height = grid.Length;

            string[] rows = NormalizeGrid(grid, width, height);
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                for (int x = 0; x < width; x++)
                {
                    char ch = row[x];
                    Color c;
                    if (ch != '.' && palette.TryGetValue(ch, out c))
                    {
                        bmp.SetPixel(x, y, c);
                    }
                    else
                    {
                        bmp.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
            return bmp;
        }
    }
}
