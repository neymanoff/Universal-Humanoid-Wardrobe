using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class WardrobeManagerTests
    {
        private GameObject _characterObj;
        private WardrobeManager _wardrobeManager;
        private GameObject _dummyPrefab;

        [SetUp]
        public void SetUp()
        {
            _characterObj = new GameObject("TestCharacter");
            _characterObj.AddComponent<Animator>();
            _wardrobeManager = _characterObj.AddComponent<WardrobeManager>();

            _dummyPrefab = new GameObject("DummyEquipmentPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterObj != null)
            {
                Object.DestroyImmediate(_characterObj);
            }

            if (_dummyPrefab != null)
            {
                Object.DestroyImmediate(_dummyPrefab);
            }
        }

        [Test]
        public void Equip_WhenItemIsNull_ReturnsItemNull()
        {
            var result = _wardrobeManager.Equip(null, EquipmentSlot.Head);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(EquipResultStatus.ItemNull, result.Status);
        }

        [Test]
        public void Equip_WhenSlotIsDisallowed_ReturnsInvalidSlot()
        {
            var helmItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helmItem.ConfigureForTest("helm", "Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            var result = _wardrobeManager.Equip(helmItem, EquipmentSlot.Chest);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(EquipResultStatus.InvalidSlot, result.Status);

            Object.DestroyImmediate(helmItem);
        }

        [Test]
        public void Equip_SingleSlotItem_EquipsSuccessfullyAndFiresEvents()
        {
            var helmItem = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helmItem.ConfigureForTest("helm", "Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            bool equippedFired = false;
            _wardrobeManager.OnItemEquipped += (slot, item, obj) =>
            {
                if (slot == EquipmentSlot.Head && item == helmItem && obj != null)
                {
                    equippedFired = true;
                }
            };

            var result = _wardrobeManager.Equip(helmItem, EquipmentSlot.Head);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(equippedFired);
            Assert.AreEqual(1, _wardrobeManager.EquippedInstances.Count);

            var loadout = _wardrobeManager.GetCurrentLoadout();
            Assert.AreEqual(1, loadout.entries.Count);
            Assert.AreEqual(EquipmentSlot.Head, loadout.entries[0].slot);
            Assert.AreEqual("helm", loadout.entries[0].itemId);

            Object.DestroyImmediate(helmItem);
        }

        [Test]
        public void Equip_TwoHandedWeapon_OccupiesBothMainHandAndOffHand()
        {
            var greatsword = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword.ConfigureForTest(
                "greatsword",
                "Greatsword",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            var result = _wardrobeManager.Equip(greatsword, EquipmentSlot.MainHand);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, _wardrobeManager.EquippedInstances.Count);

            var instance = _wardrobeManager.EquippedInstances[0];
            Assert.IsTrue(instance.OccupiesSlot(EquipmentSlot.MainHand));
            Assert.IsTrue(instance.OccupiesSlot(EquipmentSlot.OffHand));

            // Loadout captures only primary slots to prevent duplicate equips on reload
            var loadout = _wardrobeManager.GetCurrentLoadout();
            Assert.AreEqual(1, loadout.entries.Count);
            Assert.AreEqual(EquipmentSlot.MainHand, loadout.entries[0].slot);
            Assert.AreEqual("greatsword", loadout.entries[0].itemId);

            Object.DestroyImmediate(greatsword);
        }

        [Test]
        public void Unequip_SecondarySlotOfTwoHander_ClearsPrimarySlotAtomically()
        {
            var greatsword = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword.ConfigureForTest(
                "greatsword",
                "Greatsword",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            _wardrobeManager.Equip(greatsword, EquipmentSlot.MainHand);
            Assert.AreEqual(1, _wardrobeManager.EquippedInstances.Count);

            // Unequip via OffHand (secondary slot)
            bool unequipSuccess = _wardrobeManager.Unequip(EquipmentSlot.OffHand);

            Assert.IsTrue(unequipSuccess);
            Assert.AreEqual(0, _wardrobeManager.EquippedInstances.Count);
            Assert.AreEqual(0, _wardrobeManager.GetCurrentLoadout().entries.Count);

            Object.DestroyImmediate(greatsword);
        }

        [Test]
        public void Equip_ConflictingItemInSameSlot_ReplacesPreviousItem()
        {
            var sword1 = ScriptableObject.CreateInstance<WardrobeItemSO>();
            sword1.ConfigureForTest("sword1", "Bronze Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand });

            var sword2 = ScriptableObject.CreateInstance<WardrobeItemSO>();
            sword2.ConfigureForTest("sword2", "Iron Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand });

            _wardrobeManager.Equip(sword1, EquipmentSlot.MainHand);
            Assert.AreEqual(1, _wardrobeManager.EquippedInstances.Count);
            Assert.AreEqual("sword1", _wardrobeManager.GetCurrentLoadout().entries[0].itemId);

            // Equip sword2 over sword1
            var result = _wardrobeManager.Equip(sword2, EquipmentSlot.MainHand);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, _wardrobeManager.EquippedInstances.Count);
            Assert.AreEqual("sword2", _wardrobeManager.GetCurrentLoadout().entries[0].itemId);

            Object.DestroyImmediate(sword1);
            Object.DestroyImmediate(sword2);
        }

        [Test]
        public void UnequipAll_ClearsAllSlotsAndDestroysInstances()
        {
            var helm = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helm.ConfigureForTest("helm", "Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            var chest = ScriptableObject.CreateInstance<WardrobeItemSO>();
            chest.ConfigureForTest("chest", "Chest", _dummyPrefab, new[] { EquipmentSlot.Chest });

            _wardrobeManager.Equip(helm, EquipmentSlot.Head);
            _wardrobeManager.Equip(chest, EquipmentSlot.Chest);
            Assert.AreEqual(2, _wardrobeManager.EquippedInstances.Count);

            _wardrobeManager.UnequipAll();

            Assert.AreEqual(0, _wardrobeManager.EquippedInstances.Count);
            Assert.AreEqual(0, _wardrobeManager.GetCurrentLoadout().entries.Count);

            Object.DestroyImmediate(helm);
            Object.DestroyImmediate(chest);
        }

        [Test]
        public void ApplyLoadout_RehydratesEquipmentViaResolverCallback()
        {
            var helm = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helm.ConfigureForTest("helm_epic", "Epic Helm", _dummyPrefab, new[] { EquipmentSlot.Head });

            var cuirass = ScriptableObject.CreateInstance<WardrobeItemSO>();
            cuirass.ConfigureForTest("cuirass_epic", "Epic Cuirass", _dummyPrefab, new[] { EquipmentSlot.Chest });

            var itemDb = new Dictionary<string, WardrobeItemSO>
            {
                { "helm_epic", helm },
                { "cuirass_epic", cuirass }
            };

            var loadout = new WardrobeLoadout();
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.Head, "helm_epic"));
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.Chest, "cuirass_epic"));

            _wardrobeManager.ApplyLoadout(loadout, id => itemDb.TryGetValue(id, out var item) ? item : null);

            Assert.AreEqual(2, _wardrobeManager.EquippedInstances.Count);
            var activeLoadout = _wardrobeManager.GetCurrentLoadout();
            Assert.AreEqual(2, activeLoadout.entries.Count);

            Object.DestroyImmediate(helm);
            Object.DestroyImmediate(cuirass);
        }

        [Test]
        public void ModularBodyParts_WhenEquippingConcealingItem_HidesSubMeshRenderer()
        {
            var torsoMeshObj = new GameObject("TorsoMesh");
            var torsoRenderer = torsoMeshObj.AddComponent<MeshRenderer>();
            torsoRenderer.enabled = true;

            var partBinding = new WardrobeManager.ModularBodyPart
            {
                part = BodyPartMask.UpperTorso,
                renderer = torsoRenderer
            };
            _wardrobeManager.ConfigureModularPartsForTest(new[] { partBinding });

            var cuirass = ScriptableObject.CreateInstance<WardrobeItemSO>();
            cuirass.ConfigureForTest(
                "plate_armor",
                "Plate Armor",
                _dummyPrefab,
                new[] { EquipmentSlot.Chest },
                hiddenParts: BodyPartMask.UpperTorso);

            // Equip cuirass -> torsoRenderer should become hidden (enabled = false)
            _wardrobeManager.Equip(cuirass, EquipmentSlot.Chest);
            Assert.IsFalse(torsoRenderer.enabled);

            // Unequip cuirass -> torsoRenderer should become visible again (enabled = true)
            _wardrobeManager.Unequip(EquipmentSlot.Chest);
            Assert.IsTrue(torsoRenderer.enabled);

            Object.DestroyImmediate(torsoMeshObj);
            Object.DestroyImmediate(cuirass);
        }
    }
}
