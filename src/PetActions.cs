using System;
using System.Collections.Generic;

namespace StardewDesktopPet
{
    // 参考 kele-desktop-pet 的动作定义：每个角色有自己的动作集合与切换权重。
    public enum PetAction
    {
        Idle,
        Walk,
        Happy,
        Sleep,
        Lick,
        LieDown,
        Dance,
        Sit,
        LookUp,
        Coffee,
        Eat
    }

    public static class PetActions
    {
        private static readonly PetAction[] LegacyActions =
        {
            PetAction.Idle, PetAction.Walk, PetAction.Happy, PetAction.Sleep, PetAction.Eat
        };

        private static readonly PetAction[] CatActions =
        {
            PetAction.Walk, PetAction.Idle, PetAction.Lick, PetAction.LieDown,
            PetAction.Sleep, PetAction.Happy, PetAction.Sit, PetAction.LookUp
        };

        private static readonly PetAction[] HumanActions =
        {
            PetAction.Walk, PetAction.Idle, PetAction.Sit, PetAction.LookUp,
            PetAction.Dance, PetAction.Coffee, PetAction.Sleep
        };

        private static readonly PetAction[] FurnaceActions = { PetAction.Idle };

        public static PetAction[] GetAvailable(PetType pet)
        {
            if (pet == PetType.Furnace) return FurnaceActions;
            if (pet == PetType.FarmCat) return CatActions;
            if (pet == PetType.Sebastian || pet == PetType.Emily || pet == PetType.Abigail) return HumanActions;
            return LegacyActions;
        }

        public static bool Supports(PetType pet, PetAction action)
        {
            PetAction[] all = GetAvailable(pet);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == action) return true;
            }
            return false;
        }

        public static string GetLabel(PetAction action)
        {
            switch (action)
            {
                case PetAction.Walk: return "走路";
                case PetAction.Happy: return "开心跳跃";
                case PetAction.Sleep: return "睡觉";
                case PetAction.Lick: return "舔爪子";
                case PetAction.LieDown: return "趴下";
                case PetAction.Dance: return "跳舞";
                case PetAction.Sit: return "坐下";
                case PetAction.LookUp: return "抬头看天";
                case PetAction.Coffee: return "喝咖啡";
                case PetAction.Eat: return "吃东西";
                default: return "发呆";
            }
        }

        public static PetAnim GetAnim(PetAction action)
        {
            switch (action)
            {
                case PetAction.Walk: return PetAnim.Walk;
                case PetAction.Happy: return PetAnim.Happy;
                case PetAction.Sleep: return PetAnim.Sleep;
                case PetAction.Lick: return PetAnim.Lick;
                case PetAction.LieDown: return PetAnim.LieDown;
                case PetAction.Dance: return PetAnim.Dance;
                case PetAction.Sit: return PetAnim.Sit;
                case PetAction.LookUp: return PetAnim.LookUp;
                case PetAction.Coffee: return PetAnim.Coffee;
                case PetAction.Eat: return PetAnim.Eat;
                default: return PetAnim.Idle;
            }
        }

        public static int GetDurationTicks(PetAction action, int activity)
        {
            int scale = Math.Max(6, 14 - activity); // 活跃度越高，停留越短
            switch (action)
            {
                case PetAction.Walk: return 35 + scale * 6;
                case PetAction.Happy: return 26 + scale;
                case PetAction.Sleep: return 70 + scale * 8;
                case PetAction.Lick: return 40 + scale * 3;
                case PetAction.LieDown: return 60 + scale * 4;
                case PetAction.Dance: return 80 + scale * 5;
                case PetAction.Sit: return 55 + scale * 4;
                case PetAction.LookUp: return 28 + scale * 2;
                case PetAction.Coffee: return 45 + scale * 3;
                case PetAction.Eat: return 24 + scale;
                default: return 55 + scale * 5;
            }
        }

        public static string GetSpeech(PetType pet, PetAction action)
        {
            string name = PixelSprites.GetPetDisplayName(pet);
            switch (action)
            {
                case PetAction.Walk: return "出去溜达一圈，看看镇上有什么新鲜事~";
                case PetAction.Happy: return "今天也是元气满满的一天！♪";
                case PetAction.Sleep: return "Zzz… 先眯一小会儿……";
                case PetAction.Lick: return "喵~ 毛发要打理得顺顺的。";
                case PetAction.LieDown: return "在地上摊成一张饼…好舒服。";
                case PetAction.Dance: return "♪ 跟着节拍一起摇摆吧~";
                case PetAction.Sit: return "安静坐一会儿，陪你工作。";
                case PetAction.LookUp: return "抬头看看天空，云朵好像棉花糖。";
                case PetAction.Coffee: return "来杯咖啡，继续写代码！";
                case PetAction.Eat: return "嚼嚼嚼…这个味道不错！";
                default: return name + "：发呆中……";
            }
        }

        // 参考仓库的 transitions：按当前动作给出带权重的下一个动作。
        public static PetAction PickNext(PetType pet, PetAction current, Random rng, int activity, bool allowWalk)
        {
            if (pet == PetType.Furnace) return PetAction.Idle;

            if (pet == PetType.FarmCat)
            {
                if (current == PetAction.Walk || current == PetAction.Idle)
                {
                    PetAction[] choices = allowWalk
                        ? new[] { PetAction.Idle, PetAction.Walk, PetAction.LieDown, PetAction.Lick, PetAction.Sleep, PetAction.Happy }
                        : new[] { PetAction.Idle, PetAction.LieDown, PetAction.Lick, PetAction.Sleep, PetAction.Happy };
                    int[] weights = allowWalk
                        ? new[] { 25, 25, 15, 15, 15, 5 }
                        : new[] { 30, 20, 20, 20, 10 };
                    return PickWeighted(choices, weights, rng);
                }
                PetAction[] common = allowWalk
                    ? new[] { PetAction.Walk, PetAction.Idle, PetAction.Lick, PetAction.Sleep }
                    : new[] { PetAction.Idle, PetAction.Lick, PetAction.Sleep, PetAction.Sit };
                int[] commonWeights = allowWalk ? new[] { 45, 25, 18, 12 } : new[] { 40, 25, 20, 15 };
                return PickWeighted(common, commonWeights, rng);
            }

            if (pet == PetType.Sebastian || pet == PetType.Emily || pet == PetType.Abigail)
            {
                if (current == PetAction.Walk || current == PetAction.Idle || current == PetAction.Sit)
                {
                    PetAction[] choices = allowWalk
                        ? new[] { PetAction.Idle, PetAction.Walk, PetAction.Sit, PetAction.LookUp, PetAction.Dance, PetAction.Coffee }
                        : new[] { PetAction.Idle, PetAction.Sit, PetAction.LookUp, PetAction.Dance, PetAction.Coffee };
                    int[] weights = allowWalk
                        ? new[] { 22, 30, 13, 12, 13, 10 }
                        : new[] { 26, 18, 16, 22, 18 };
                    return PickWeighted(choices, weights, rng);
                }
                PetAction[] back = allowWalk
                    ? new[] { PetAction.Walk, PetAction.Idle, PetAction.Sit }
                    : new[] { PetAction.Idle, PetAction.Sit, PetAction.LookUp };
                int[] backWeights = allowWalk ? new[] { 65, 20, 15 } : new[] { 45, 35, 20 };
                return PickWeighted(back, backWeights, rng);
            }

            // 原有 9 种伙伴沿用经典节奏：走路 / 发呆 / 开心 / 打盹。
            int walk = allowWalk ? 20 + activity * 5 : 0;
            int idle = 40;
            int happy = 12;
            int sleep = 18;
            PetAction[] legacy = { PetAction.Walk, PetAction.Idle, PetAction.Happy, PetAction.Sleep };
            int[] legacyWeights = { walk, idle, happy, sleep };
            return PickWeighted(legacy, legacyWeights, rng);
        }

        // 双击宠物时优先展示角色特色动作（对应参考仓库的 doubleClickActions）。
        public static PetAction PickDoubleClickAction(PetType pet, Random rng)
        {
            if (pet == PetType.FarmCat)
            {
                return PickWeighted(
                    new[] { PetAction.Lick, PetAction.LieDown, PetAction.Sleep, PetAction.Happy },
                    new[] { 40, 30, 20, 10 }, rng);
            }
            if (pet == PetType.Sebastian)
            {
                return PickWeighted(new[] { PetAction.Coffee, PetAction.Sit, PetAction.LookUp }, new[] { 50, 30, 20 }, rng);
            }
            if (pet == PetType.Emily || pet == PetType.Abigail)
            {
                return PickWeighted(new[] { PetAction.Dance, PetAction.Happy, PetAction.Sit }, new[] { 55, 25, 20 }, rng);
            }
            return PetAction.Happy;
        }

        private static PetAction PickWeighted(PetAction[] choices, int[] weights, Random rng)
        {
            if (choices == null || choices.Length == 0) return PetAction.Idle;

            int total = 0;
            for (int i = 0; i < weights.Length && i < choices.Length; i++)
            {
                total += Math.Max(0, weights[i]);
            }
            if (total <= 0) return choices[0];

            int roll = rng.Next(total);
            int acc = 0;
            for (int i = 0; i < weights.Length && i < choices.Length; i++)
            {
                acc += Math.Max(0, weights[i]);
                if (roll < acc) return choices[i];
            }
            return choices[choices.Length - 1];
        }
    }
}
