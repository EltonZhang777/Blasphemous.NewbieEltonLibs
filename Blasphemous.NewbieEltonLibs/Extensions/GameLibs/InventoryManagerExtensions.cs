using Blasphemous.ModdingAPI;
using Framework.Inventory;
using Framework.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using static Framework.Managers.InventoryManager;

namespace Blasphemous.NewbieEltonLibs.Extensions.GameLibs;

/// <summary>
/// Extension methods for InventoryManager providing simplified inventory query and lookup operations
/// </summary>
public static class InventoryManagerExtensions
{
    /// <summary>
    /// Gets all inventory objects from all categories
    /// </summary>
    public static List<BaseInventoryObject> GetAllInventoryObjects(this InventoryManager inventoryManager)
    {
        List<BaseInventoryObject> result = [
            .. inventoryManager.GetAllCollectibleItems(),
            .. inventoryManager.GetAllPrayers(),
            .. inventoryManager.GetAllQuestItems(),
            .. inventoryManager.GetAllRelics(),
            .. inventoryManager.GetAllRosaryBeads(),
            .. inventoryManager.GetAllSwords(),
            ];
        return result;
    }

    /// <summary>
    /// Gets all owned inventory objects from all categories
    /// </summary>
    public static List<BaseInventoryObject> GetAllOwnedInventoryObjects(this InventoryManager inventoryManager)
    {
        List<BaseInventoryObject> result = [
            .. inventoryManager.GetCollectibleItemOwned(),
            .. inventoryManager.GetPrayersOwned(),
            .. inventoryManager.GetQuestItemOwned(),
            .. inventoryManager.GetRelicsOwned(),
            .. inventoryManager.GetRosaryBeadOwned(),
            .. inventoryManager.GetSwordsOwned(),
            ];
        return result;
    }

    /// <summary>
    /// Gets all inventory objects of a specific type
    /// </summary>
    public static List<T> GetAllInventoryObjectsOfType<T>(this InventoryManager inventoryManager) where T : BaseInventoryObject
    {
        switch (typeof(T))
        {
            case Type t when t == typeof(Relic):
                return inventoryManager.GetAllRelics().Select(x => x as T).ToList();
            case Type t when t == typeof(RosaryBead):
                return inventoryManager.GetAllRosaryBeads().Select(x => x as T).ToList();
            case Type t when t == typeof(QuestItem):
                return inventoryManager.GetAllQuestItems().Select(x => x as T).ToList();
            case Type t when t == typeof(Prayer):
                return inventoryManager.GetAllPrayers().Select(x => x as T).ToList();
            case Type t when t == typeof(BlasCollectibleItem):
                return inventoryManager.GetAllCollectibleItems().Select(x => x as T).ToList();
            case Type t when t == typeof(Sword):
                return inventoryManager.GetAllSwords().Select(x => x as T).ToList();
        }
        return null;
    }

    /// <summary>
    /// Gets all inventory objects of a specific item type
    /// </summary>
    public static List<BaseInventoryObject> GetAllInventoryObjectsOfType(this InventoryManager inventoryManager, ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Relic:
                return inventoryManager.GetAllRelics().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Bead:
                return inventoryManager.GetAllRosaryBeads().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Quest:
                return inventoryManager.GetAllQuestItems().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Prayer:
                return inventoryManager.GetAllPrayers().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Collectible:
                return inventoryManager.GetAllCollectibleItems().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Sword:
                return inventoryManager.GetAllSwords().Select(x => x as BaseInventoryObject).ToList();
        }
        return null;
    }

    /// <summary>
    /// Gets all owned inventory objects of a specific type
    /// </summary>
    public static List<T> GetOwnedInventoryObjectsOfType<T>(this InventoryManager inventoryManager) where T : BaseInventoryObject
    {
        switch (typeof(T))
        {
            case Type t when t == typeof(Relic):
                return inventoryManager.GetRelicsOwned().Select(x => x as T).ToList();
            case Type t when t == typeof(RosaryBead):
                return inventoryManager.GetRosaryBeadOwned().Select(x => x as T).ToList();
            case Type t when t == typeof(QuestItem):
                return inventoryManager.GetQuestItemOwned().Select(x => x as T).ToList();
            case Type t when t == typeof(Prayer):
                return inventoryManager.GetPrayersOwned().Select(x => x as T).ToList();
            case Type t when t == typeof(BlasCollectibleItem):
                return inventoryManager.GetCollectibleItemOwned().Select(x => x as T).ToList();
            case Type t when t == typeof(Sword):
                return inventoryManager.GetSwordsOwned().Select(x => x as T).ToList();
        }
        return null;
    }

    /// <summary>
    /// Gets all owned inventory objects of a specific item type
    /// </summary>
    public static List<BaseInventoryObject> GetOwnedInventoryObjectsOfType(this InventoryManager inventoryManager, ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Relic:
                return inventoryManager.GetRelicsOwned().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Bead:
                return inventoryManager.GetRosaryBeadOwned().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Quest:
                return inventoryManager.GetQuestItemOwned().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Prayer:
                return inventoryManager.GetPrayersOwned().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Collectible:
                return inventoryManager.GetCollectibleItemOwned().Select(x => x as BaseInventoryObject).ToList();
            case ItemType.Sword:
                return inventoryManager.GetSwordsOwned().Select(x => x as BaseInventoryObject).ToList();
        }
        return null;
    }

    /// <summary>
    /// Gets a specific inventory item by its id
    /// </summary>
    /// <param name="inventoryManager">The inventory manager instance</param>
    /// <param name="id">The inventory item id (e.g. RB301, PR12)</param>
    /// <param name="throwError">Whether to throw an exception if the item type is unknown</param>
    public static BaseInventoryObject GetInventoryItemFromId(this InventoryManager inventoryManager, string id, bool throwError = false)
    {
        id = id.Trim();
        ItemType itemType = inventoryManager.GetItemTypeFromId(id);
        switch (itemType)
        {
            case ItemType.Relic:
                return inventoryManager.GetRelic(id);
            case ItemType.Bead:
                return inventoryManager.GetRosaryBead(id);
            case ItemType.Quest:
                return inventoryManager.GetQuestItem(id);
            case ItemType.Prayer:
                return inventoryManager.GetPrayer(id);
            case ItemType.Collectible:
                return inventoryManager.GetCollectibleItem(id);
            case ItemType.Sword:
                return inventoryManager.GetSword(id);
            default:
                ModLog.Error($"Unknown inventory item type for id: {id}");
                if (throwError)
                {
                    throw new KeyNotFoundException($"Unknown inventory item type for id: {id}");
                }
                return null;
        }
    }

    /// <summary>
    /// Tries to get an inventory item by id without throwing exceptions
    /// </summary>
    /// <param name="inventoryManager">The inventory manager instance</param>
    /// <param name="id">The inventory item id</param>
    /// <param name="inventoryObject">The found inventory object, or null</param>
    /// <returns>True if the item was found, false otherwise</returns>
    public static bool TryGetInventoryItemFromId(this InventoryManager inventoryManager, string id, out BaseInventoryObject inventoryObject)
    {
        inventoryObject = null;
        try
        {
            inventoryObject = inventoryManager.GetInventoryItemFromId(id, true);
        }
        catch (KeyNotFoundException e)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Determines the item type from its id prefix
    /// </summary>
    /// <param name="inventoryManager">The inventory manager instance</param>
    /// <param name="id">The inventory item id</param>
    /// <param name="throwError">Whether to throw an exception for unknown id prefixes</param>
    public static ItemType GetItemTypeFromId(this InventoryManager inventoryManager, string id, bool throwError = false)
    {
        ItemType result = ItemType.Collectible;
        id = id.Trim();

        if (id.StartsWith("RE"))
        {
            result = ItemType.Relic;
        }
        else if (id.StartsWith("RB"))
        {
            result = ItemType.Bead;
        }
        else if (id.StartsWith("QI"))
        {
            result = ItemType.Quest;
        }
        else if (id.StartsWith("PR"))
        {
            result = ItemType.Prayer;
        }
        else if (id.StartsWith("CO"))
        {
            result = ItemType.Collectible;
        }
        else if (id.StartsWith("HE"))
        {
            result = ItemType.Sword;
        }
        else
        {
            ModLog.Error($"Unknown inventory item type for id: {id}");
            if (throwError)
            {
                throw new KeyNotFoundException($"Unknown inventory item type for id: {id}");
            }
        }

        return result;
    }

    /// <summary>
    /// Tries to determine the item type from an id without throwing exceptions
    /// </summary>
    /// <param name="inventoryManager">The inventory manager instance</param>
    /// <param name="id">The inventory item id</param>
    /// <param name="itemType">The determined item type</param>
    /// <returns>True if the type was identified, false otherwise</returns>
    public static bool TryGetItemTypeFromId(this InventoryManager inventoryManager, string id, out ItemType itemType)
    {
        itemType = ItemType.Collectible;
        try
        {
            itemType = inventoryManager.GetItemTypeFromId(id, true);
        }
        catch (KeyNotFoundException e)
        {
            return false;
        }
        return true;
    }
}
