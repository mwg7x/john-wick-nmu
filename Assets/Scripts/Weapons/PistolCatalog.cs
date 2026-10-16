using UnityEngine;

namespace LastStep.Weapons
{
    public static class PistolCatalog
    {
        public const int Count = 3;
        private static readonly string[] Names = { "Phantom", "Viper", "Reaper" };
        private static readonly string[] Roles = {
            "50 damage  |  balanced accuracy  |  3 shots / round",
            "35 damage  |  fast fire, light recoil  |  3 shots / round",
            "70 damage  |  heavy recoil  |  3 shots / round"
        };
        private static readonly string[] Paths = { "Weapons/Sentinel", "Weapons/Swift", "Weapons/Vanguard" };
        public static int Clamp(int index) => Mathf.Clamp(index, 0, Count - 1);
        public static string Name(int index) => Names[Clamp(index)];
        public static string Summary(int index) => Roles[Clamp(index)];
        public static WeaponData Load(int index) => Resources.Load<WeaponData>(Paths[Clamp(index)]);
    }
}
