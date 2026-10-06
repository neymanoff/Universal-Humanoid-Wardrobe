using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class TacticalAndSocketStateTests
    {
        private GameObject _characterObj;
        private Animator _animator;
        private WardrobeManager _wardrobeManager;
        private Transform _hipsBone;
        private Transform _rightHandBone;

        [SetUp]
        public void SetUp()
        {
            _characterObj = new GameObject("TestTacticalCharacter");
            _animator = _characterObj.AddComponent<Animator>();
            _wardrobeManager = _characterObj.AddComponent<WardrobeManager>();

            // Setup greybox bone hierarchy
            _hipsBone = new GameObject("Hips").transform;
            _hipsBone.SetParent(_characterObj.transform, false);

            _rightHandBone = new GameObject("RightHand").transform;
            _rightHandBone.SetParent(_characterObj.transform, false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterObj != null)
            {
                Object.DestroyImmediate(_characterObj);
            }
        }

        [Test]
        public void EquipmentSlot_TacticalAndSurvivalSlots_ExistAndMapToDefaultBones()
        {
            Assert.AreEqual(HumanBodyBones.Chest, WardrobeManager.GetDefaultBoneForSlot(EquipmentSlot.TacticalVest));
            Assert.AreEqual(HumanBodyBones.Chest, WardrobeManager.GetDefaultBoneForSlot(EquipmentSlot.Backpack));
            Assert.AreEqual(HumanBodyBones.Head, WardrobeManager.GetDefaultBoneForSlot(EquipmentSlot.FaceMask));
            Assert.AreEqual(HumanBodyBones.RightUpperLeg, WardrobeManager.GetDefaultBoneForSlot(EquipmentSlot.Holster));
            Assert.AreEqual(HumanBodyBones.Hips, WardrobeManager.GetDefaultBoneForSlot(EquipmentSlot.Belt));
        }

        [Test]
        public void WardrobeLoadout_TacticalSlots_RoundtripSerialization()
        {
            var loadout = new WardrobeLoadout();
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.TacticalVest, "plate_carrier_heavy"));
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.Backpack, "tactical_rucksack_35l"));
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.Holster, "kydex_pistol_holster"));
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.FaceMask, "ballistic_mask_v2"));
            loadout.entries.Add(new EquippedSlotEntry(EquipmentSlot.Belt, "utility_molle_belt"));

            string json = loadout.ToJson();
            Assert.IsNotNull(json);

            var restored = WardrobeLoadout.FromJson(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(5, restored.entries.Count);

            Assert.AreEqual(EquipmentSlot.TacticalVest, restored.entries[0].slot);
            Assert.AreEqual("plate_carrier_heavy", restored.entries[0].itemId);

            Assert.AreEqual(EquipmentSlot.Backpack, restored.entries[1].slot);
            Assert.AreEqual("tactical_rucksack_35l", restored.entries[1].itemId);

            Assert.AreEqual(EquipmentSlot.Holster, restored.entries[2].slot);
            Assert.AreEqual("kydex_pistol_holster", restored.entries[2].itemId);

            Assert.AreEqual(EquipmentSlot.FaceMask, restored.entries[3].slot);
            Assert.AreEqual("ballistic_mask_v2", restored.entries[3].itemId);

            Assert.AreEqual(EquipmentSlot.Belt, restored.entries[4].slot);
            Assert.AreEqual("utility_molle_belt", restored.entries[4].itemId);
        }

        [Test]
        public void HumanoidAttachmentPoint_SetSocketState_WhenHolsterNotSupported_ReturnsFalse()
        {
            var propObj = new GameObject("SidearmProp");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();

            attachment.ConfigureHolsteredForTest(
                supports: false,
                bone: HumanBodyBones.Hips,
                pos: Vector3.zero,
                rot: Vector3.zero,
                scale: Vector3.one);

            bool result = attachment.SetSocketState(SocketAttachmentState.Holstered, _animator);

            Assert.IsFalse(result);
            Assert.AreEqual(SocketAttachmentState.Drawn, attachment.CurrentSocketState);

            Object.DestroyImmediate(propObj);
        }

        [Test]
        public void HumanoidAttachmentPoint_SetSocketState_WhenAnimatorNull_ReturnsFalse()
        {
            var propObj = new GameObject("SidearmProp");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();

            attachment.ConfigureHolsteredForTest(
                supports: true,
                bone: HumanBodyBones.Hips,
                pos: Vector3.zero,
                rot: Vector3.zero,
                scale: Vector3.one);

            bool result = attachment.SetSocketState(SocketAttachmentState.Holstered, null);

            Assert.IsFalse(result);
            Assert.AreEqual(SocketAttachmentState.Drawn, attachment.CurrentSocketState);

            Object.DestroyImmediate(propObj);
        }

        [Test]
        public void HumanoidAttachmentPoint_SetSocketState_TransitionsBetweenDrawnAndHolstered_ReparentsAndAppliesOffsets()
        {
            var propObj = new GameObject("SidearmProp");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();

            var drawnPos = new Vector3(0.01f, -0.02f, 0.05f);
            var drawnRot = new Vector3(10f, 0, 0);
            var drawnScale = Vector3.one;

            var holsteredPos = new Vector3(0.18f, -0.1f, 0.04f);
            var holsteredRot = new Vector3(0, 90f, 0);
            var holsteredScale = new Vector3(0.95f, 0.95f, 0.95f);

            attachment.ConfigureForTest(drawnPos, drawnRot, drawnScale, autoMirror: false);
            attachment.ConfigureHolsteredForTest(
                supports: true,
                bone: HumanBodyBones.Hips,
                pos: holsteredPos,
                rot: holsteredRot,
                scale: holsteredScale);

            // 1. Transition to Holstered
            bool toHolsteredResult = attachment.SetSocketState(
                SocketAttachmentState.Holstered,
                _animator,
                bodyScale: null,
                slot: EquipmentSlot.MainHand);

            Assert.IsTrue(toHolsteredResult);
            Assert.AreEqual(SocketAttachmentState.Holstered, attachment.CurrentSocketState);
            Assert.AreEqual(_hipsBone, propObj.transform.parent);
            Assert.AreEqual(holsteredPos.x, propObj.transform.localPosition.x, 0.001f);
            Assert.AreEqual(holsteredPos.y, propObj.transform.localPosition.y, 0.001f);
            Assert.AreEqual(holsteredPos.z, propObj.transform.localPosition.z, 0.001f);

            // 2. Transition back to Drawn
            bool toDrawnResult = attachment.SetSocketState(
                SocketAttachmentState.Drawn,
                _animator,
                bodyScale: null,
                slot: EquipmentSlot.MainHand);

            Assert.IsTrue(toDrawnResult);
            Assert.AreEqual(SocketAttachmentState.Drawn, attachment.CurrentSocketState);
            Assert.AreEqual(_rightHandBone, propObj.transform.parent);
            Assert.AreEqual(drawnPos.x, propObj.transform.localPosition.x, 0.001f);
            Assert.AreEqual(drawnPos.y, propObj.transform.localPosition.y, 0.001f);
            Assert.AreEqual(drawnPos.z, propObj.transform.localPosition.z, 0.001f);

            Object.DestroyImmediate(propObj);
        }

        [Test]
        public void HumanoidAttachmentPoint_SetSocketState_WithHolsteredProfileOverride_AppliesProfileTunedOffsets()
        {
            var propObj = new GameObject("SidearmProp");
            var attachment = propObj.AddComponent<HumanoidAttachmentPoint>();

            var defaultHolsterPos = new Vector3(0.18f, -0.1f, 0.04f);
            var dwarfHolsterPos = new Vector3(0.12f, -0.06f, 0.02f);
            var dwarfHolsterRot = new Vector3(0, 45f, 0);
            var dwarfHolsterScale = new Vector3(0.8f, 0.8f, 0.8f);

            var dwarfOverride = new ProfileTransformOverride("Dwarf", dwarfHolsterPos, dwarfHolsterRot, dwarfHolsterScale);

            attachment.ConfigureHolsteredForTest(
                supports: true,
                bone: HumanBodyBones.Hips,
                pos: defaultHolsterPos,
                rot: Vector3.zero,
                scale: Vector3.one,
                profileOverrides: new[] { dwarfOverride });

            var bodyScale = _characterObj.AddComponent<CharacterBodyScale>();
            var dwarfProfile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            dwarfProfile.ConfigureForTest("Dwarf", 1f);
            bodyScale.BaseProfile = dwarfProfile;

            bool success = attachment.SetSocketState(
                SocketAttachmentState.Holstered,
                _animator,
                bodyScale,
                EquipmentSlot.MainHand);

            Assert.IsTrue(success);
            Assert.AreEqual(dwarfHolsterPos.x, propObj.transform.localPosition.x, 0.001f);
            Assert.AreEqual(dwarfHolsterPos.y, propObj.transform.localPosition.y, 0.001f);
            Assert.AreEqual(dwarfHolsterPos.z, propObj.transform.localPosition.z, 0.001f);

            Object.DestroyImmediate(propObj);
            Object.DestroyImmediate(dwarfProfile);
        }

        [Test]
        public void WardrobeManager_SetItemSocketState_SwitchesStateAndInvokesEvent()
        {
            var weaponPrefab = new GameObject("WeaponWithSocket");
            var attachment = weaponPrefab.AddComponent<HumanoidAttachmentPoint>();
            attachment.ConfigureForTest(Vector3.zero, Vector3.zero, Vector3.one, autoMirror: false);
            attachment.ConfigureHolsteredForTest(
                supports: true,
                bone: HumanBodyBones.Hips,
                pos: new Vector3(0.2f, -0.1f, 0),
                rot: Vector3.zero,
                scale: Vector3.one);

            var weaponSO = ScriptableObject.CreateInstance<WardrobeItemSO>();
            weaponSO.ConfigureForTest("assault_rifle", "Assault Rifle", weaponPrefab, new[] { EquipmentSlot.MainHand });

            var equipResult = _wardrobeManager.Equip(weaponSO, EquipmentSlot.MainHand);
            Assert.IsTrue(equipResult.IsSuccess);
            Assert.AreEqual(SocketAttachmentState.Drawn, _wardrobeManager.GetItemSocketState(EquipmentSlot.MainHand));

            EquipmentSlot eventSlot = EquipmentSlot.Head;
            SocketAttachmentState eventState = SocketAttachmentState.Drawn;
            bool eventFired = false;

            _wardrobeManager.OnSocketStateChanged += (slot, state) =>
            {
                eventSlot = slot;
                eventState = state;
                eventFired = true;
            };

            // Transition to Holstered
            bool holsterSuccess = _wardrobeManager.SetItemSocketState(EquipmentSlot.MainHand, SocketAttachmentState.Holstered);
            Assert.IsTrue(holsterSuccess);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(EquipmentSlot.MainHand, eventSlot);
            Assert.AreEqual(SocketAttachmentState.Holstered, eventState);
            Assert.AreEqual(SocketAttachmentState.Holstered, _wardrobeManager.GetItemSocketState(EquipmentSlot.MainHand));

            // Setting state on empty slot returns false
            bool emptyResult = _wardrobeManager.SetItemSocketState(EquipmentSlot.TacticalVest, SocketAttachmentState.Holstered);
            Assert.IsFalse(emptyResult);

            Object.DestroyImmediate(weaponPrefab);
            Object.DestroyImmediate(weaponSO);
        }
    }
}
