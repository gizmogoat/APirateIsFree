using GorillaNetworking;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;

namespace APirateIsFree.Patches
{
    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("RequestCosmetics", MethodType.Normal), HarmonyWrapSafe]
    internal class VRRigPatch
    {
        private static bool Prefix(ref VRRig __instance, ref PhotonMessageInfoWrapped info)
        {
            NetPlayer player = NetworkSystem.Instance.GetPlayer(info.senderID);
            if (__instance.netView.IsMine && CosmeticsController.hasInstance)
            {
                if (CosmeticsController.instance.isHidingCosmeticsFromRemotePlayers)
                {
                    __instance.netView.SendRPC("RPC_HideAllCosmetics", info.Sender);
                    return false;
                }
                
                var tempCurrentWornSet = new CosmeticsController.CosmeticSet();
                tempCurrentWornSet.CopyItems(CosmeticsController.instance.currentWornSet);
                foreach (var item in tempCurrentWornSet.items)
                {
                    if (AllowedPatch.badItems.Contains(item.itemName))
                    {
                        Debug.Log($"Stripping non-owned cosmetic {item} from response");

                        tempCurrentWornSet.items[tempCurrentWornSet.items.ToList().IndexOf(item)] =
                            CosmeticsController.instance.nullItem;
                    }
                }
                
                int[] array = tempCurrentWornSet.ToPackedIDArray();
                int[] array2 = CosmeticsController.instance.tryOnSet.ToPackedIDArray();

                __instance.netView.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", player, array, array2);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(GorillaNetworking.CosmeticsController))]
    [HarmonyPatch("UpdateWornCosmetics", MethodType.Normal), HarmonyWrapSafe]
    internal class UpdateWornCosmeticsPatch
    {
        private static bool Prefix(CosmeticsController __instance, bool sync)
        {
            GorillaTagger.Instance.offlineVRRig.LocalUpdateCosmeticsWithTryon(__instance.currentWornSet, __instance.tryOnSet);
            if (sync && GorillaTagger.Instance.myVRRig != null)
            {
                if (__instance.isHidingCosmeticsFromRemotePlayers)
                {
                    GorillaTagger.Instance.myVRRig.SendRPC("RPC_HideAllCosmetics", RpcTarget.All);
                    return false;
                }
                
                var tempCurrentWornSet = new CosmeticsController.CosmeticSet();
                tempCurrentWornSet.CopyItems(__instance.currentWornSet);
                foreach (var item in tempCurrentWornSet.items)
                {
                    if (AllowedPatch.badItems.Contains(item.itemName))
                    {
                        Debug.Log($"Stripping non-owned cosmetic {item} from response");

                        tempCurrentWornSet.items[tempCurrentWornSet.items.ToList().IndexOf(item)] =
                            CosmeticsController.instance.nullItem;
                    }
                }
                
                int[] array = tempCurrentWornSet.ToPackedIDArray();
                int[] array2 = __instance.tryOnSet.ToPackedIDArray();
                GorillaTagger.Instance.myVRRig.SendRPC("RPC_UpdateCosmeticsWithTryonPacked", RpcTarget.All, array, array2);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("LocalUpdateCosmeticsWithTryon", MethodType.Normal), HarmonyWrapSafe]
    internal class LocalUpdateCosmeticsPatch
    {
        private static bool Prefix(VRRig __instance, CosmeticsController.CosmeticSet newTryOnSet)
        {
            if (!__instance.isOfflineVRRig) { return true; }
            __instance.cosmeticSet = CosmeticsController.instance.currentWornSet;
            __instance.tryOnSet = newTryOnSet;
            if (__instance.initializedCosmetics)
            {
                __instance.SetCosmeticsActive();
            }
            return false;
        }
    }
    
    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("IsItemAllowed", MethodType.Normal), HarmonyWrapSafe]
    internal class AllowedPatch
    {
        public static List<string> badItems = new List<string>();

        private static void Postfix(ref string itemName, ref VRRig __instance, ref bool __result)
        {
            if (!__instance.isOfflineVRRig) return;
            if (!__result)
            {
                badItems.Add(itemName);
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(CosmeticsController.CosmeticSet))]
    [HarmonyPatch("LoadFromPlayerPreferences", MethodType.Normal), HarmonyWrapSafe]
    internal class UnlockPatch
    {
        private static void Postfix()
        {
            foreach (CosmeticsController.CosmeticItem item in CosmeticsController.instance.allCosmetics)
            {
                if (!item.itemName.Contains(".")) continue;
                CosmeticsController.instance.UnlockItem(item.itemName);
            }
            CosmeticsController.instance.UpdateWardrobeModelsAndButtons();
            CosmeticsController.instance.UpdateWornCosmetics();
            CosmeticsController.instance.UpdateMyCosmetics();
        }
    }

    [HarmonyPatch(typeof(CosmeticsController)), HarmonyWrapSafe]
    [HarmonyPatch(nameof(CosmeticsController.CompareCategoryToSavedCosmeticSlots))]
    internal class CheckPatch
    {
        private static bool Prefix(CosmeticsController __instance, ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(CosmeticsController.CosmeticSet)), HarmonyWrapSafe]
    [HarmonyPatch(nameof(CosmeticsController.CosmeticSet.LoadFromPlayerPreferences))]
    internal class SlotApplyPatch
    {
        private static bool Prefix(CosmeticsController.CosmeticSet __instance, CosmeticsController controller)
        {
            for (int i = 0; i < 16; i++)
            {
                CosmeticsController.CosmeticSlots cosmeticSlots = (CosmeticsController.CosmeticSlots)i;
                string @string = PlayerPrefs.GetString(CosmeticsController.CosmeticSet.SlotPlayerPreferenceName(cosmeticSlots), "NOTHING");
                if (@string == "null" || @string == "NOTHING")
                {
                    __instance.items[i] = controller.nullItem;
                }
                else
                {
                    CosmeticsController.CosmeticItem item = controller.GetItemFromDict(@string);
                    __instance.items[i] = item;
                }
            }
            return false;
        }
    }
}
