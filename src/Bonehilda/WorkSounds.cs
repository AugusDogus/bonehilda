using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;

namespace Bonehilda;

internal static class WorkSounds
{
    private static readonly System.Reflection.MethodInfo CreateEffects = AccessTools.Method(typeof(EffectList), "Create");
    private static EffectList? hammer;
    private static EffectList? supply;
    private static bool HasSound(EffectList effects) => effects.m_effectPrefabs.Any(e => e.m_enabled && e.m_prefab && e.m_prefab.GetComponentsInChildren<AudioSource>(true).Length > 0);
    private static EffectList AudioOnly(EffectList source) => new EffectList
    {
        m_effectPrefabs = source.m_effectPrefabs.Where(e => e.m_enabled && e.m_prefab && e.m_prefab.GetComponentsInChildren<AudioSource>(true).Length > 0).ToArray()
    };

    internal static void Configure()
    {
        hammer = AudioOnly(PrefabManager.Instance.GetPrefab("woodwall").GetComponent<Piece>().m_placeEffect);
        supply = AudioOnly(PrefabManager.Instance.GetPrefab("smelter").GetComponent<Smelter>().m_oreAddedEffects);
        if (!HasSound(hammer) || !HasSound(supply)) throw new System.InvalidOperationException("Vanilla work sounds could not be resolved.");
    }

    internal static void Repair(Piece piece)
    {
        // The same build effects and positional hammer sound used by player repairs.
        Play(piece.m_placeEffect, piece.transform.position, piece.transform.rotation);
        if (!HasSound(piece.m_placeEffect)) Play(hammer!, piece.transform.position, piece.transform.rotation);
    }

    internal static void Supplied(SupplyTarget target)
    {
        var field = target.Kind == SupplyTarget.SupplyKind.MachineInput ? "m_oreAddedEffects" :
            target.Kind == SupplyTarget.SupplyKind.FermenterInput ? "m_addedEffects" :
            target.Kind == SupplyTarget.SupplyKind.Ammunition ? "m_addAmmoEffect" : "m_fuelAddedEffects";
        var native = AccessTools.Field(target.Consumer.GetType(), field)?.GetValue(target.Consumer) as EffectList;
        // Native owner handlers already play their effects. Fill silent adapters without doubling sound.
        if (native == null || !HasSound(native)) Play(supply!, target.Port ? target.Port!.position : target.Consumer.transform.position, target.Consumer.transform.rotation);
    }

    internal static void Play(EffectList effects, Vector3 position, Quaternion rotation)
    {
        // Current Valheim adds an exclusive-player ZDOID parameter to the reference API.
        var args = CreateEffects.GetParameters().Length == 6
            ? new object[] { position, rotation, null!, 1f, -1, ZDOID.None }
            : new object[] { position, rotation, null!, 1f, -1 };
        try { CreateEffects.Invoke(effects, args); }
        catch (System.Exception e) { Plugin.Instance.LogWarning("Could not play work effects: " + e.GetBaseException().Message); }
    }
}
