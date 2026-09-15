using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class EquipmentRuleResolverTests
    {
        private GameObject _dummyPrefab;

        [SetUp]
        public void SetUp()
        {
            _dummyPrefab = new GameObject("DummyPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_dummyPrefab != null)
            {
                Object.DestroyImmediate(_dummyPrefab);
            }
        }

        [Test]
        public void ValidateEquipRequest_WhenItemIsNull_ReturnsItemNull()
        {
            var result = EquipmentRuleResolver.ValidateEquipRequest(null, EquipmentSlot.Head);
            Assert.AreEqual(EquipResultStatus.ItemNull, result);
        }

        [Test]
        public void ValidateEquipRequest_WhenPrefabIsNull_ReturnsMissingPrefab()
        {
            var item = ScriptableObject.CreateInstance<WardrobeItemSO>();
            item.ConfigureForTest("helm", "Iron Helm", null, new[] { EquipmentSlot.Head });

            var result = EquipmentRuleResolver.ValidateEquipRequest(item, EquipmentSlot.Head);
            Assert.AreEqual(EquipResultStatus.MissingPrefab, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void ValidateEquipRequest_WhenSlotIsNotAllowed_ReturnsInvalidSlot()
        {
            var item = ScriptableObject.CreateInstance<WardrobeItemSO>();
            item.ConfigureForTest("helm", "Iron Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            var result = EquipmentRuleResolver.ValidateEquipRequest(item, EquipmentSlot.Chest);
            Assert.AreEqual(EquipResultStatus.InvalidSlot, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void ValidateEquipRequest_WhenSlotIsAllowed_ReturnsSuccess()
        {
            var item = ScriptableObject.CreateInstance<WardrobeItemSO>();
            item.ConfigureForTest("helm", "Iron Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            var result = EquipmentRuleResolver.ValidateEquipRequest(item, EquipmentSlot.Head);
            Assert.AreEqual(EquipResultStatus.Success, result);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void GetConflictingSlots_WhenNoItemsEquipped_ReturnsEmptyList()
        {
            var item = ScriptableObject.CreateInstance<WardrobeItemSO>();
            item.ConfigureForTest("sword", "Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand });

            var currentSlots = new Dictionary<EquipmentSlot, EquippedItemInstance>();
            var conflicts = EquipmentRuleResolver.GetConflictingSlots(item, EquipmentSlot.MainHand, currentSlots);

            Assert.IsNotNull(conflicts);
            Assert.AreEqual(0, conflicts.Count);

            Object.DestroyImmediate(item);
        }

        [Test]
        public void GetConflictingSlots_WhenTargetSlotAlreadyOccupied_ReturnsTargetSlot()
        {
            var existingItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            existingItem.ConfigureForTest("dagger", "Dagger", _dummyPrefab, new[] { EquipmentSlot.MainHand });
            var existingInstance = new EquippedItemInstance(existingItem, _dummyPrefab, EquipmentSlot.MainHand, new[] { EquipmentSlot.MainHand });

            var currentSlots = new Dictionary<EquipmentSlot, EquippedItemInstance>
            {
                { EquipmentSlot.MainHand, existingInstance }
            };

            var newItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            newItem.ConfigureForTest("sword", "Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand });

            var conflicts = EquipmentRuleResolver.GetConflictingSlots(newItem, EquipmentSlot.MainHand, currentSlots);

            Assert.AreEqual(1, conflicts.Count);
            Assert.Contains(EquipmentSlot.MainHand, conflicts);

            Object.DestroyImmediate(existingItem);
            Object.DestroyImmediate(newItem);
        }

        [Test]
        public void GetConflictingSlots_WhenEquippingTwoHanderOverOneHanderAndShield_ReturnsBothSlots()
        {
            var swordItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            swordItem.ConfigureForTest("sword", "Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand });
            var swordInstance = new EquippedItemInstance(swordItem, _dummyPrefab, EquipmentSlot.MainHand, new[] { EquipmentSlot.MainHand });

            var shieldItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            shieldItem.ConfigureForTest("shield", "Shield", _dummyPrefab, new[] { EquipmentSlot.OffHand });
            var shieldInstance = new EquippedItemInstance(shieldItem, _dummyPrefab, EquipmentSlot.OffHand, new[] { EquipmentSlot.OffHand });

            var currentSlots = new Dictionary<EquipmentSlot, EquippedItemInstance>
            {
                { EquipmentSlot.MainHand, swordInstance },
                { EquipmentSlot.OffHand, shieldInstance }
            };

            // Two-handed greatsword occupies MainHand and OffHand
            var greatsword = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword.ConfigureForTest(
                "greatsword",
                "Greatsword",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            var conflicts = EquipmentRuleResolver.GetConflictingSlots(greatsword, EquipmentSlot.MainHand, currentSlots);

            Assert.AreEqual(2, conflicts.Count);
            Assert.Contains(EquipmentSlot.MainHand, conflicts);
            Assert.Contains(EquipmentSlot.OffHand, conflicts);

            Object.DestroyImmediate(swordItem);
            Object.DestroyImmediate(shieldItem);
            Object.DestroyImmediate(greatsword);
        }

        [Test]
        public void GetConflictingSlots_WhenEquippingShieldOverExistingTwoHander_ClearsBothHands()
        {
            // Existing two-handed weapon in MainHand + OffHand
            var greatsword = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword.ConfigureForTest(
                "greatsword",
                "Greatsword",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            var twoHanderInstance = new EquippedItemInstance(
                greatsword,
                _dummyPrefab,
                EquipmentSlot.MainHand,
                new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var currentSlots = new Dictionary<EquipmentSlot, EquippedItemInstance>
            {
                { EquipmentSlot.MainHand, twoHanderInstance },
                { EquipmentSlot.OffHand, twoHanderInstance }
            };

            // Incoming shield for OffHand only
            var shieldItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            shieldItem.ConfigureForTest("shield", "Shield", _dummyPrefab, new[] { EquipmentSlot.OffHand });

            var conflicts = EquipmentRuleResolver.GetConflictingSlots(shieldItem, EquipmentSlot.OffHand, currentSlots);

            // Because the two-hander occupies both slots, equipping into OffHand must conflict with both
            Assert.AreEqual(2, conflicts.Count);
            Assert.Contains(EquipmentSlot.MainHand, conflicts);
            Assert.Contains(EquipmentSlot.OffHand, conflicts);

            Object.DestroyImmediate(greatsword);
            Object.DestroyImmediate(shieldItem);
        }
    }
}
