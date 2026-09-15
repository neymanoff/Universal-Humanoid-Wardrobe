using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class WardrobeLoadoutTests
    {
        [Test]
        public void FromJson_WhenNullOrEmpty_ReturnsEmptyLoadout()
        {
            var nullResult = WardrobeLoadout.FromJson(null);
            Assert.IsNotNull(nullResult);
            Assert.IsNotNull(nullResult.entries);
            Assert.AreEqual(0, nullResult.entries.Count);

            var emptyResult = WardrobeLoadout.FromJson("");
            Assert.IsNotNull(emptyResult);
            Assert.AreEqual(0, emptyResult.entries.Count);
        }

        [Test]
        public void ToJson_WhenEmpty_ProducesValidJson()
        {
            var loadout = new WardrobeLoadout();
            string json = loadout.ToJson();

            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("entries"));

            var restored = WardrobeLoadout.FromJson(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(0, restored.entries.Count);
        }

        [Test]
        public void Roundtrip_WithMultipleSlots_PreservesAllEntriesAccurately()
        {
            var original = new WardrobeLoadout();
            original.entries.Add(new EquippedSlotEntry(EquipmentSlot.Head, "iron_helmet"));
            original.entries.Add(new EquippedSlotEntry(EquipmentSlot.Chest, "steel_cuirass"));
            original.entries.Add(new EquippedSlotEntry(EquipmentSlot.MainHand, "excalibur"));
            original.entries.Add(new EquippedSlotEntry(EquipmentSlot.OffHand, "tower_shield"));
            original.entries.Add(new EquippedSlotEntry(EquipmentSlot.LeftRing, "ruby_ring"));

            string json = original.ToJson(prettyPrint: true);
            Assert.IsNotNull(json);

            var restored = WardrobeLoadout.FromJson(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(5, restored.entries.Count);

            Assert.AreEqual(EquipmentSlot.Head, restored.entries[0].slot);
            Assert.AreEqual("iron_helmet", restored.entries[0].itemId);

            Assert.AreEqual(EquipmentSlot.Chest, restored.entries[1].slot);
            Assert.AreEqual("steel_cuirass", restored.entries[1].itemId);

            Assert.AreEqual(EquipmentSlot.MainHand, restored.entries[2].slot);
            Assert.AreEqual("excalibur", restored.entries[2].itemId);

            Assert.AreEqual(EquipmentSlot.OffHand, restored.entries[3].slot);
            Assert.AreEqual("tower_shield", restored.entries[3].itemId);

            Assert.AreEqual(EquipmentSlot.LeftRing, restored.entries[4].slot);
            Assert.AreEqual("ruby_ring", restored.entries[4].itemId);
        }

        [Test]
        public void FromJson_WithInvalidJson_DoesNotThrowAndReturnsFallback()
        {
            var loadout = WardrobeLoadout.FromJson("not a json string");
            Assert.IsNotNull(loadout);
            Assert.IsNotNull(loadout.entries);
        }
    }
}
