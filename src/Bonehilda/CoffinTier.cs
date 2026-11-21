namespace Bonehilda;

internal sealed class CoffinTier
{
    internal readonly string Prefab;
    internal readonly int Stars;
    internal readonly int Slots;
    internal readonly int Wood;
    internal readonly int Bones;
    internal readonly int Bronze;
    internal int Width => Slots == 2 ? 2 : 4;
    internal int Height => Slots / Width;
    internal string Name => $"Bonehilda's coffin ({Stars} star{(Stars == 1 ? "" : "s")})";

    private CoffinTier(string prefab, int stars, int slots, int wood, int bones, int bronze)
    {
        Prefab = prefab; Stars = stars; Slots = slots; Wood = wood; Bones = bones; Bronze = bronze;
    }

    // Retain the original prefab ID for existing worlds and their linked two-star maid.
    internal static readonly CoffinTier[] All =
    {
        new CoffinTier("BH_Coffin_0Star", 0, 2, 10, 5, 0),
        new CoffinTier("BH_Coffin_1Star", 1, 4, 20, 10, 1),
        new CoffinTier(Plugin.CoffinPrefab, 2, 8, 20, 10, 2)
    };

    internal static bool IsCoffin(int prefabHash)
    {
        foreach (var tier in All) if (tier.Prefab.GetStableHashCode() == prefabHash) return true;
        return false;
    }
}
