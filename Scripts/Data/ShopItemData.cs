using System.Collections.Generic;

namespace StoreRobberyEnhanced.Data
{
    /// <summary>
    /// Defines a purchasable shop item.
    /// </summary>
    internal class ShopItemData
    {
        public string Id { get; }
        public string Name { get; }
        public int Price { get; }
        public ShopItemCategory Category { get; }
        public string Description { get; }   // ⭐ NEW

        public ShopItemData(string id, string name, int price, ShopItemCategory category, string description)
        {
            Id = id;
            Name = name;
            Price = price;
            Category = category;
            Description = description;
        }

        /// <summary>
        /// Categories for shop items.
        /// </summary>
        internal enum ShopItemCategory
        {
            Snack,
            Food,
            Drink,
            Utility,
            Medical,
            Other
        }

        /// <summary>
        /// Static database of all items sold in convenience stores.
        /// </summary>
        internal static class ShopItemDatabase
        {
            public static readonly Dictionary<string, ShopItemData> Items = new Dictionary<string, ShopItemData>
            {
                // Snacks
            { "ps_and_qs", new ShopItemData("ps_and_qs", "P's & Q's", 1, ShopItemCategory.Snack, "Small candy snack. Restores a little health.") },
            { "egochaser", new ShopItemData("egochaser", "EgoChaser", 2, ShopItemCategory.Snack, "Energy bar. Restores some health.") },
            { "meteorite", new ShopItemData("meteorite", "Meteorite", 4, ShopItemCategory.Snack, "Chocolate bar. Restores moderate health.") },
            { "donut", new ShopItemData("donut", "Donut", 2, ShopItemCategory.Snack, "Donut. Restores moderate health.") },

            // Food
            { "sandwich", new ShopItemData("sandwich", "Sandwich", 5, ShopItemCategory.Food, "Half Sandwich. Restores 25% health.") },
            { "taco", new ShopItemData("taco", "Taco", 5, ShopItemCategory.Food, "Taco. Restores 50% health.") },
            { "hotdog", new ShopItemData("hotdog", "Hotdog", 5, ShopItemCategory.Food, "Hotdog. Restores 25% health.") },
            { "burger", new ShopItemData("burger", "Hamburger", 7, ShopItemCategory.Food, "Hamburger. Restores 75% health.") },
            { "steak", new ShopItemData("Steak", "Grilled Steak", 15, ShopItemCategory.Food, "Grill Steak. Restores 75% health.") },

            // Cherry Popper Items
            { "shake1", new ShopItemData("shake1", "Vanilla Milkshake", 7, ShopItemCategory.Drink, "Vanilla Milkshake Restores 15% health.") },
            { "shake2", new ShopItemData("shake2", "Chocolate Milkshake", 7, ShopItemCategory.Drink, "Chocolate Milkshake. Restores 15% health.") },
            { "shake3", new ShopItemData("shake3", "Strawberry Milkshake", 7, ShopItemCategory.Drink, "Strawberry Milkshake. Restores 15% health.") },
            { "coffee", new ShopItemData("coffee", "Iced Coffee", 3, ShopItemCategory.Drink, "Iced Coffee cup. Restores 15% health.") },
            { "juice01", new ShopItemData("juice01", "Slushy", 2, ShopItemCategory.Drink, "Slushy drink. Restores 15% health.") },
            
            // Drinks
            { "waterbottle", new ShopItemData("waterbottle", "Bottled Water", 1, ShopItemCategory.Drink, "Bottle of Water. Restores 15% health.") },
            { "sprunk", new ShopItemData("sprunk", "Sprunk", 1, ShopItemCategory.Drink, "Sprunk soda. Restores 15% health.") },
            { "e_colas", new ShopItemData("e_colas", "eCola", 1, ShopItemCategory.Drink, "Classic cola drink. Restores 15% health.") },
            { "beer1", new ShopItemData("beer1", "Bottle of Beer (PiBwasser)", 5, ShopItemCategory.Drink, "Bottle of beer. Restores 15% health.") },
            { "beer2", new ShopItemData("beer2", "Bottle of Beer (Logger)", 5, ShopItemCategory.Drink, "Bottle of beer. Restores 15% health.") },
            { "beer40", new ShopItemData("beer40", "40oz Bottle of Beer", 9, ShopItemCategory.Drink, "40oz bottle of beer. Restores 15% health.") },
            { "whiskey", new ShopItemData("whiskey", "Bottle of Whiskey", 20, ShopItemCategory.Drink, "Bottle of whiskey. Restores 15% health.") }
                // Medical
                //{ "bandage", new ShopItemData("bandage", "Bandage", 15, ShopItemCategory.Medical, "Stops bleeding and restores health.") }

                // Utility
                //{ "lighter", new ShopItemData("lighter", "Lighter", 5, ShopItemCategory.Utility, "Useful for lighting things.") }
            };
        }
    }
}