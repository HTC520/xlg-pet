using System;

namespace StardewDesktopPet
{
    public static class StardewData
    {
        private static readonly Random Rng = new Random();

        private static readonly string[] JunimoLines = new[]
        {
            "祝尼魔！(欢快地转圈)",
            "收集齐收集包啦~ ♪",
            "森林的精灵在看着你哦！",
            "今天的防风草长势一定很棒！",
            "稍微休息一下，喝口水吧~",
            "喜欢你留在桌子上的香气！",
            "呀！被你摸头了好开心~",
            "雨天也会在社区中心等你哦。"
        };

        private static readonly string[] ChickenLines = new[]
        {
            "咯咯哒~ ♪",
            "今天的鸡蛋是金星品质吗？",
            "谢恩说蓝色小鸡最特别啦！",
            "咕咕…想吃点新鲜的玉米~",
            "在任务栏散步真舒服！",
            "蹭蹭你的鼠标指针~"
        };

        private static readonly string[] SlimeLines = new[]
        {
            "咕叽咕叽！(弹性十足)",
            "其实我是很友好的史莱姆！",
            "只要戴着史莱姆克星戒指就能贴贴~",
            "弹跳！弹跳！♪",
            "今天心情像紫星果一样甜！"
        };

        private static readonly string[] KrobusLines = new[]
        {
            "嘘…人类，我喜欢你安静地工作。",
            "在下水道待久了，桌面也很不错。",
            "星期五可以一起吃虚空蛋黄酱吗？",
            "今天也是和平的一天呢。",
            "……(害羞地往阴影里缩了缩)"
        };

        private static readonly string[] CatLines = new[]
        {
            "喵~ 今天也要好好摸头哦。",
            "晒太阳是农场猫最重要的事。",
            "绳子？毛线球？我什么都不在乎~",
            "轻轻蹭一下你的手背。",
            "午后的工位是我的专属猫窝。"
        };

        private static readonly string[] SebastianLines = new[]
        {
            "……我只是需要在桌边安静待一会儿。",
            "咖啡凉了，不过代码还在跑。",
            "要不要一起看看晚上的星空？",
            "摩托车今天也停在门口呢。",
            "别盯着我看啦…我在想事情。"
        };

        private static readonly string[] EmilyLines = new[]
        {
            "♪ today is a good day to dance ~",
            "我缝了一件新衣服，想给你看看！",
            "水晶折射的光很好看吧？",
            "跟着音乐动起来吧！♪",
            "要不要和我一起跳舞~"
        };

        private static readonly string[] AbigailLines = new[]
        {
            "晚上一起去矿洞探险吗？",
            "我的紫色水晶在发光呢！",
            "青蛙…啊不，是史莱姆在唱歌！",
            "别怕，幽灵其实很害羞的。",
            "来一局午夜冒险怎么样？"
        };

        private static readonly string[] FurnaceLines = new[]
        {
            "炉火噼啪…把烦恼丢进来吧。",
            "烧掉的是烦恼，留下的是好心情。",
            "矿石需要火，心情也需要。",
            "火光暖暖的，别急，慢慢来。"
        };

        private static readonly string[] Fortunes = new[]
        {
            "🔮 占卜电视：精灵今天非常开心！万事顺遂，适合推进重要任务！",
            "🔮 占卜电视：精灵心情不错，今天会有一点点额外的好运哦~",
            "🔮 占卜电视：精灵今天心情平和，稳步前行就是最好的节奏。",
            "🔮 占卜电视：精灵今天有点调皮，记得多喝水、保存好文件哦！",
            "🔮 占卜电视：星星在云后闪烁，给自己泡杯热茶再出发吧~"
        };

        public static string GetRandomQuote(PetType pet)
        {
            string[] pool;
            if (pet == PetType.BlueChicken || pet == PetType.WhiteChicken)
                pool = ChickenLines;
            else if (pet == PetType.SlimeGreen)
                pool = SlimeLines;
            else             if (pet == PetType.Krobus)
                pool = KrobusLines;
            else if (pet == PetType.FarmCat)
                pool = CatLines;
            else if (pet == PetType.Sebastian)
                pool = SebastianLines;
            else if (pet == PetType.Emily)
                pool = EmilyLines;
            else if (pet == PetType.Abigail)
                pool = AbigailLines;
            else if (pet == PetType.Furnace)
                pool = FurnaceLines;
            else
                pool = JunimoLines;

            return pool[Rng.Next(pool.Length)];
        }

        public static string GetDailyFortune()
        {
            DateTime today = DateTime.Today;
            int seed = today.Year * 10000 + today.Month * 100 + today.Day;
            Random dayRng = new Random(seed);
            return Fortunes[dayRng.Next(Fortunes.Length)];
        }

        public static string GetFeedReaction(PetType pet, EmoteType food)
        {
            if (food == EmoteType.Stardrop)
            {
                return "★ 太美味了！想起了星露谷的味道！(好感度 +25)";
            }
            if (pet == PetType.Krobus && food == EmoteType.VoidMayo)
            {
                return "！这是我最爱的虚空蛋黄酱！谢谢你！(好感度 +25)";
            }
            if (food == EmoteType.VoidMayo)
            {
                return "唔…味道很神秘，不过你的心意我收下啦！(好感度 +5)";
            }
            if (food == EmoteType.Strawberry)
            {
                return "甜滋滋的草莓！好幸福~ (好感度 +15)";
            }
            if (food == EmoteType.Hay)
            {
                return "香喷喷的干草！吃饱饱啦~ 😊 (好感度 +12)";
            }
            return "嚼嚼嚼…新鲜的防风草真好吃！(好感度 +10)";
        }

        public static int GetFoodPoints(PetType pet, EmoteType food)
        {
            if (food == EmoteType.Stardrop) return 25;
            if (pet == PetType.Krobus && food == EmoteType.VoidMayo) return 25;
            if (food == EmoteType.VoidMayo) return 5;
            if (food == EmoteType.Strawberry) return 15;
            if (food == EmoteType.Hay) return 12;
            return 10;
        }
    }
}
