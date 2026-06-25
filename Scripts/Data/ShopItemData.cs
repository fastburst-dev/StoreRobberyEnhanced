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
                { "sandwich", new ShopItemData("sandwich", "Sandwich", 5, ShopItemCategory.Snack, "Half Sandwich. Restores moderate health.") },
                { "taco", new ShopItemData("taco", "Taco", 5, ShopItemCategory.Snack, "Taco. Restores moderate health.") },
                { "hotdog", new ShopItemData("hotdog", "Hotdog", 5, ShopItemCategory.Snack, "Hotdog. Restores moderate health.") },
                { "burger", new ShopItemData("burger", "Hamburger", 7, ShopItemCategory.Snack, "Hamburger. Restores moderate health.") },
                
                // Drinks
                { "coffee", new ShopItemData("coffee", "Coffee", 3, ShopItemCategory.Drink, "Coffee Cup. Restores 15% health.") },
                { "juice01", new ShopItemData("juice01", "Juice", 2, ShopItemCategory.Drink, "Juice Drink. Restores 15% health.") },
                { "sprunk", new ShopItemData("sprunk", "Sprunk", 1, ShopItemCategory.Drink, "Sprunk soda. Restores 15% health.") },
                { "e_colas", new ShopItemData("e_colas", "eCola", 1, ShopItemCategory.Drink, "Classic cola drink. Restores 15% health.") },
                { "beer1", new ShopItemData("beer1", "Bottle of Beer (PiBwasser)", 5, ShopItemCategory.Drink, "Bottle of Beer. Restores 15% health.") },
                { "beer2", new ShopItemData("beer2", "Bottle of Beer (Logger)", 5, ShopItemCategory.Drink, "Bottle of Beer. Restores 15% health.") },
                { "beer40", new ShopItemData("beer40", "40oz Bottle of Beer", 9, ShopItemCategory.Drink, "40oz Bottle of Beer. Restores 15% health.") },
                { "whiskey", new ShopItemData("whiskey", "Bottle of Whiskey", 20, ShopItemCategory.Drink, "Bottle of Whiskey. Restores 15% health.") }
                
                // Medical
                //{ "bandage", new ShopItemData("bandage", "Bandage", 15, ShopItemCategory.Medical, "Stops bleeding and restores health.") }

                // Utility
                //{ "lighter", new ShopItemData("lighter", "Lighter", 5, ShopItemCategory.Utility, "Useful for lighting things.") }
            };
        }
    }
}