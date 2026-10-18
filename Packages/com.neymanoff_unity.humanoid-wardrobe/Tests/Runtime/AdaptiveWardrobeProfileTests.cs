using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class AdaptiveWardrobeProfileTests
    {
        private GameObject _characterObj;
        private WardrobeManager _wardrobeManager;
        private CharacterBodyScale _bodyScale;
        private GameObject _basePrefab;
        private GameObject _dwarfPrefab;

        [SetUp]
        public void SetUp()
        {
            _characterObj = new GameObject("TestCharacterAdaptive");
            _characterObj.AddComponent<Animator>();
            _bodyScale = _characterObj.AddComponent<CharacterBodyScale>();
            _wardrobeManager = _characterObj.AddComponent<WardrobeManager>();

            _basePrefab = new GameObject("BaseItemPrefab");
            _dwarfPrefab = new GameObject("DwarfVariantPrefab");
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterObj != null) Object.DestroyImmediate(_characterObj);
            if (_basePrefab != null) Object.DestroyImmediate(_basePrefab);
            if (_dwarfPrefab != null) Object.DestroyImmediate(_dwarfPrefab);
        }

        [Test]
        public void HumanoidAttachmentPoint_WithProfileTransformOverride_AppliesProfileTunedOffsets()
        {
            var propObj = new GameObject("WeaponProp");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();

            var defaultPos = new Vector3(0, 0, 0);
            var defaultRot = new Vector3(0, 0, 0);
            var defaultScale = Vector3.one;

            var dwarfOverride = new ProfileTransformOverride(
                "Dwarf",
                new Vector3(0.1f, -0.05f, 0.02f),
                new Vector3(10f, 0, 0),
                new Vector3(0.75f, 0.75f, 0.75f));

            attachment.ConfigureForTest(defaultPos, defaultRot, defaultScale, autoMirror: false, new[] { dwarfOverride });

            // Create dwarf profile
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            profile.ConfigureForTest("Dwarf", 1f); // 1.0 global scale to isolate profile override check
            _bodyScale.BaseProfile = profile;

            attachment.ApplyOffsets(isLeftSlot: false, _bodyScale, EquipmentSlot.MainHand);

            Assert.AreEqual(new Vector3(0.1f, -0.05f, 0.02f), propObj.transform.localPosition);
            Assert.AreEqual(10f, propObj.transform.localRotation.eulerAngles.x, 0.01f);
            Assert.AreEqual(new Vector3(0.75f, 0.75f, 0.75f), propObj.transform.localScale);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(propObj);
        }

        [Test]
        public void HumanoidAttachmentPoint_WithPropScaleMultiplierAndSocketOverride_AppliesCompoundTransforms()
        {
            var propObj = new GameObject("WeaponPropCompound");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();
            attachment.ConfigureForTest(new Vector3(1, 0, 0), Vector3.zero, Vector3.one, autoMirror: false);

            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            var handSocketOverride = new SocketOffsetOverride(
                EquipmentSlot.MainHand,
                new Vector3(0, 0.5f, 0),
                new Vector3(0, 45f, 0),
                new Vector3(1f, 1f, 1f));

            // Global prop scale multiplier = 0.8
            profile.ConfigureForTest("Dwarf", 0.8f, new[] { handSocketOverride });
            _bodyScale.BaseProfile = profile;

            attachment.ApplyOffsets(isLeftSlot: false, _bodyScale, EquipmentSlot.MainHand);

            // Position: initial (1, 0, 0) + socket offset (0, 0.5, 0) = (1, 0.5, 0)
            Assert.AreEqual(new Vector3(1f, 0.5f, 0), propObj.transform.localPosition);
            // Scale: initial (1, 1, 1) * global prop scale (0.8) = (0.8, 0.8, 0.8)
            Assert.AreEqual(new Vector3(0.8f, 0.8f, 0.8f), propObj.transform.localScale);
            Assert.AreEqual(45f, propObj.transform.localRotation.eulerAngles.y, 0.01f);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(propObj);
        }

        [Test]
        public void HumanoidAttachmentPoint_LeftSlotMirroring_InvertsPositionXAndPreservesForwardBladeDirection()
        {
            var rightObj = new GameObject("RightAxeProp");
            var rightAttachment = rightObj.AddComponent<HumanoidAttachmentPoint>();
            var axeRot = new Vector3(-20f, -5f, 85f);
            rightAttachment.ConfigureForTest(
                new Vector3(0.02f, 0.1f, 0.02f),
                axeRot,
                Vector3.one,
                autoMirror: true);
            rightAttachment.ApplyOffsets(isLeftSlot: false, bodyScale: null, slot: EquipmentSlot.MainHand);

            var leftObj = new GameObject("LeftAxeProp");
            var leftAttachment = leftObj.AddComponent<HumanoidAttachmentPoint>();
            leftAttachment.ConfigureForTest(
                new Vector3(0.02f, 0.1f, 0.02f),
                axeRot,
                Vector3.one,
                autoMirror: true);
            leftAttachment.ApplyOffsets(isLeftSlot: true, bodyScale: null, slot: EquipmentSlot.OffHand);

            Assert.AreEqual(-0.02f, leftObj.transform.localPosition.x, 0.001f);
            Assert.AreEqual(0.1f, leftObj.transform.localPosition.y, 0.001f);
            Assert.AreEqual(0.02f, leftObj.transform.localPosition.z, 0.001f);

            // In hand bone space (where LeftHand X is inverted relative to RightHand mirror),
            // both the shaft (+Z) and forward cutting edge/blade (+X) must satisfy v_L = (-v_R.x, +v_R.y, +v_R.z)
            Vector3 rightBlade = rightObj.transform.localRotation * Vector3.right;
            Vector3 leftBlade = leftObj.transform.localRotation * Vector3.right;
            Assert.AreEqual(-rightBlade.x, leftBlade.x, 0.001f);
            Assert.AreEqual(rightBlade.y, leftBlade.y, 0.001f);
            Assert.AreEqual(rightBlade.z, leftBlade.z, 0.001f);

            Vector3 rightShaft = rightObj.transform.localRotation * Vector3.forward;
            Vector3 leftShaft = leftObj.transform.localRotation * Vector3.forward;
            Assert.AreEqual(-rightShaft.x, leftShaft.x, 0.001f);
            Assert.AreEqual(rightShaft.y, leftShaft.y, 0.001f);
            Assert.AreEqual(rightShaft.z, leftShaft.z, 0.001f);

            Object.DestroyImmediate(rightObj);
            Object.DestroyImmediate(leftObj);
        }

        [Test]
        public void WardrobeItemSO_GetPrefabForProfile_ResolvesMatchingVariantOrFallback()
        {
            var itemSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            var dwarfVariant = new ProfilePrefabOverride("Dwarf", _dwarfPrefab);

            itemSO.ConfigureForTest(
                "hammer",
                "War Hammer",
                _basePrefab,
                new[] { EquipmentSlot.MainHand },
                null,
                BodyPartMask.None,
                null,
                new[] { dwarfVariant });

            // 1. Matches Dwarf -> returns Dwarf prefab
            Assert.AreEqual(_dwarfPrefab, itemSO.GetPrefabForProfile("Dwarf"));
            Assert.AreEqual(_dwarfPrefab, itemSO.GetPrefabForProfile("dwarf")); // Case-insensitive!

            // 2. Unmatched profile (e.g. Orc or Elf) -> returns default base prefab
            Assert.AreEqual(_basePrefab, itemSO.GetPrefabForProfile("Orc"));
            Assert.AreEqual(_basePrefab, itemSO.GetPrefabForProfile(null));
            Assert.AreEqual(_basePrefab, itemSO.GetPrefabForProfile(""));

            Object.DestroyImmediate(itemSO);
        }

        [Test]
        public void WardrobeManager_Equip_OnDwarfCharacter_SpawnsProfileSpecificPrefab()
        {
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            profile.ConfigureForTest("Dwarf", 0.8f);
            _bodyScale.BaseProfile = profile;

            // Re-assign bodyScale to manager to ensure reference is active in test setup
            _wardrobeManager.SetBodyScale(_bodyScale);

            var itemSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            var dwarfVariant = new ProfilePrefabOverride("Dwarf", _dwarfPrefab);
            itemSO.ConfigureForTest(
                "axe",
                "Battle Axe",
                _basePrefab,
                new[] { EquipmentSlot.MainHand },
                null,
                BodyPartMask.None,
                null,
                new[] { dwarfVariant });

            var result = _wardrobeManager.Equip(itemSO, EquipmentSlot.MainHand);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Instance);
            Assert.IsNotNull(result.Instance.InstanceObject);
            // Verify that the spawned instance is named after the Dwarf variant prefab
            StringAssert.StartsWith(_dwarfPrefab.name, result.Instance.InstanceObject.name);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(itemSO);
        }
    }
}
