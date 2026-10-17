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

        [Test]
        public void Equip_TwoOneHandedWeapons_AttachesToRightHandAndLeftHandRespectively()
        {
            var rightHandBone = new GameObject("RightHand").transform;
            rightHandBone.SetParent(_characterObj.transform, false);

            var leftHandBone = new GameObject("LeftHand").transform;
            leftHandBone.SetParent(_characterObj.transform, false);

            var weaponPrefab = new GameObject("WeaponPrefab");
            var attachment = weaponPrefab.AddComponent<HumanoidAttachmentPoint>();
            // Even with useCustomBone = true and RightHand, left slot must mirror to LeftHand when autoMirror is true
            attachment.ConfigureForTest(
                new Vector3(0.02f, 0.1f, 0f),
                new Vector3(-20f, -5f, 85f),
                Vector3.one,
                autoMirror: true,
                useCustom: true,
                customBone: HumanBodyBones.RightHand);

            var axeSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            axeSO.ConfigureForTest("axe", "Axe", weaponPrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var swordSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            swordSO.ConfigureForTest("sword", "Sword", weaponPrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var mainResult = _wardrobeManager.Equip(axeSO, EquipmentSlot.MainHand);
            var offResult = _wardrobeManager.Equip(swordSO, EquipmentSlot.OffHand);

            Assert.IsTrue(mainResult.IsSuccess);
            Assert.IsTrue(offResult.IsSuccess);

            GameObject mainObj = _wardrobeManager.GetEquippedItem(EquipmentSlot.MainHand);
            GameObject offObj = _wardrobeManager.GetEquippedItem(EquipmentSlot.OffHand);

            Assert.IsNotNull(mainObj);
            Assert.IsNotNull(offObj);
            Assert.AreEqual(rightHandBone, mainObj.transform.parent);
            Assert.AreEqual(leftHandBone, offObj.transform.parent);
            Assert.AreEqual(0.02f, mainObj.transform.localPosition.x, 0.001f);
            Assert.AreEqual(-0.02f, offObj.transform.localPosition.x, 0.001f);

            Object.DestroyImmediate(weaponPrefab);
            Object.DestroyImmediate(axeSO);
            Object.DestroyImmediate(swordSO);
        }

        [Test]
        public void Equip_SameOneHandedWeaponInBothHands_SpawnsTwoDistinctInstancesInRightAndLeftHand()
        {
            var rightHandBone = new GameObject("RightHand").transform;
            rightHandBone.SetParent(_characterObj.transform, false);

            var leftHandBone = new GameObject("LeftHand").transform;
            leftHandBone.SetParent(_characterObj.transform, false);

            var axePrefab = new GameObject("AxePrefab");
            var attachment = axePrefab.AddComponent<HumanoidAttachmentPoint>();
            attachment.ConfigureForTest(new Vector3(0.02f, 0.1f, 0.02f), Vector3.zero, Vector3.one, autoMirror: true);

            var axeSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            axeSO.ConfigureForTest("axe", "Axe", axePrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var r1 = _wardrobeManager.Equip(axeSO, EquipmentSlot.MainHand);
            var r2 = _wardrobeManager.Equip(axeSO, EquipmentSlot.OffHand);

            Assert.IsTrue(r1.IsSuccess);
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, _wardrobeManager.EquippedInstances.Count);

            GameObject rightAxe = _wardrobeManager.GetEquippedItem(EquipmentSlot.MainHand);
            GameObject leftAxe = _wardrobeManager.GetEquippedItem(EquipmentSlot.OffHand);

            Assert.AreNotEqual(rightAxe, leftAxe);
            Assert.AreEqual(rightHandBone, rightAxe.transform.parent);
            Assert.AreEqual(leftHandBone, leftAxe.transform.parent);

            Object.DestroyImmediate(axePrefab);
            Object.DestroyImmediate(axeSO);
        }

        [Test]
        public void Equip_OneHandedWeaponRequestedForOffHandWhenMainHandEmpty_RoutesToMainHandFirst()
        {
            var axeSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            axeSO.ConfigureForTest("axe", "Axe", _dummyPrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var shieldSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            shieldSO.ConfigureForTest("shield", "Shield", _dummyPrefab, new[] { EquipmentSlot.OffHand });

            // 1. Pure off-hand item (Shield) equips directly into OffHand even when MainHand is empty
            var shieldResult = _wardrobeManager.Equip(shieldSO, EquipmentSlot.OffHand);
            Assert.IsTrue(shieldResult.IsSuccess);
            Assert.AreEqual(EquipmentSlot.OffHand, shieldResult.Instance.PrimarySlot);
            _wardrobeManager.Unequip(EquipmentSlot.OffHand);

            // 2. Dual-hand 1H weapon requested for OffHand when MainHand is empty automatically routes to MainHand
            var axeResult = _wardrobeManager.Equip(axeSO, EquipmentSlot.OffHand);
            Assert.IsTrue(axeResult.IsSuccess);
            Assert.AreEqual(EquipmentSlot.MainHand, axeResult.Instance.PrimarySlot);
            Assert.AreEqual(axeSO, _wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand));
            Assert.IsNull(_wardrobeManager.GetEquippedItemData(EquipmentSlot.OffHand));

            Object.DestroyImmediate(axeSO);
            Object.DestroyImmediate(shieldSO);
        }

        [Test]
        public void SwapSlots_BetweenMainHandAndOffHand_SwapsWeaponsCleanly()
        {
            var axeSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            axeSO.ConfigureForTest("axe", "Axe", _dummyPrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            var swordSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            swordSO.ConfigureForTest("sword", "Sword", _dummyPrefab, new[] { EquipmentSlot.MainHand, EquipmentSlot.OffHand });

            _wardrobeManager.Equip(axeSO, EquipmentSlot.MainHand);
            _wardrobeManager.Equip(swordSO, EquipmentSlot.OffHand);

            bool swapped = _wardrobeManager.SwapSlots(EquipmentSlot.MainHand, EquipmentSlot.OffHand);

            Assert.IsTrue(swapped);
            Assert.AreEqual(swordSO, _wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand));
            Assert.AreEqual(axeSO, _wardrobeManager.GetEquippedItemData(EquipmentSlot.OffHand));

            Object.DestroyImmediate(axeSO);
            Object.DestroyImmediate(swordSO);
        }

        [Test]
        public void Equip_TwoHandedWeapons_WhenAllowDualTwoHandedEnabled_EquipsInBothHands()
        {
            var greatsword1 = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword1.ConfigureForTest(
                "gs1",
                "Greatsword 1",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            var greatsword2 = ScriptableObject.CreateInstance<WardrobeItemSO>();
            greatsword2.ConfigureForTest(
                "gs2",
                "Greatsword 2",
                _dummyPrefab,
                new[] { EquipmentSlot.MainHand },
                new[] { EquipmentSlot.OffHand });

            // Default: AllowDualTwoHanded is false -> OffHand is rejected for a 2H weapon
            Assert.IsFalse(_wardrobeManager.AllowDualTwoHanded);
            var rejected = _wardrobeManager.Equip(greatsword2, EquipmentSlot.OffHand);
            Assert.IsFalse(rejected.IsSuccess);
            Assert.AreEqual(EquipResultStatus.InvalidSlot, rejected.Status);

            // Enable AllowDualTwoHanded (special class capability)
            _wardrobeManager.AllowDualTwoHanded = true;

            var r1 = _wardrobeManager.Equip(greatsword1, EquipmentSlot.MainHand);
            var r2 = _wardrobeManager.Equip(greatsword2, EquipmentSlot.OffHand);

            Assert.IsTrue(r1.IsSuccess);
            Assert.IsTrue(r2.IsSuccess);
            Assert.AreEqual(2, _wardrobeManager.EquippedInstances.Count);
            Assert.AreEqual(greatsword1, _wardrobeManager.GetEquippedItemData(EquipmentSlot.MainHand));
            Assert.AreEqual(greatsword2, _wardrobeManager.GetEquippedItemData(EquipmentSlot.OffHand));

            Object.DestroyImmediate(greatsword1);
            Object.DestroyImmediate(greatsword2);
        }
    }
}
