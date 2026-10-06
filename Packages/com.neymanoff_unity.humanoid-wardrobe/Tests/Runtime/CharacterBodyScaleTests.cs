using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class CharacterBodyScaleTests
    {
        private GameObject _characterObj;
        private CharacterBodyScale _bodyScale;

        [SetUp]
        public void SetUp()
        {
            _characterObj = new GameObject("TestCharacterWithBodyScale");
            _bodyScale = _characterObj.AddComponent<CharacterBodyScale>();
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
        public void CharacterBodyScale_WhenNoProfileAssigned_ReturnsDefaultFallbacks()
        {
            Assert.AreEqual("Default", _bodyScale.EffectiveProfileId);
            Assert.AreEqual(1f, _bodyScale.EffectivePropScale);

            bool found = _bodyScale.TryGetSocketOverride(EquipmentSlot.Head, out var socketOverride);
            Assert.IsFalse(found);

            var morphs = _bodyScale.GetEffectiveMorphWeights();
            Assert.AreEqual(0, morphs.Count);
        }

        [Test]
        public void CharacterBodyScale_WithBaseProfile_ReturnsProfileProperties()
        {
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            var headOverride = new SocketOffsetOverride(
                EquipmentSlot.Head,
                new Vector3(0, -0.05f, 0),
                Vector3.zero,
                new Vector3(0.85f, 0.85f, 0.85f));

            profile.ConfigureForTest(
                "Dwarf",
                0.8f,
                new[] { headOverride },
                new[] { new BlendShapeWeightEntry("Shape_Dwarf", 100f) });

            _bodyScale.BaseProfile = profile;

            Assert.AreEqual("Dwarf", _bodyScale.EffectiveProfileId);
            Assert.AreEqual(0.8f, _bodyScale.EffectivePropScale);

            bool found = _bodyScale.TryGetSocketOverride(EquipmentSlot.Head, out var result);
            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(0, -0.05f, 0), result.positionOffset);
            Assert.AreEqual(new Vector3(0.85f, 0.85f, 0.85f), result.scaleMultiplier);

            var morphs = _bodyScale.GetEffectiveMorphWeights();
            Assert.IsTrue(morphs.ContainsKey("Shape_Dwarf"));
            Assert.AreEqual(100f, morphs["Shape_Dwarf"]);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CharacterBodyScale_InstancePropScaleOverride_TakesPrecedenceOverProfile()
        {
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            profile.ConfigureForTest("Orc", 1.25f);

            _bodyScale.BaseProfile = profile;
            _bodyScale.OverridePropScale = true;
            _bodyScale.InstancePropScaleMultiplier = 1.4f;

            Assert.AreEqual("Orc", _bodyScale.EffectiveProfileId);
            Assert.AreEqual(1.4f, _bodyScale.EffectivePropScale);

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CharacterBodyScale_CascadingSocketOverrides_InstanceOverridesProfile()
        {
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            var profileHead = new SocketOffsetOverride(
                EquipmentSlot.Head,
                new Vector3(0, -0.05f, 0),
                Vector3.zero,
                Vector3.one);
            var profileMainHand = new SocketOffsetOverride(
                EquipmentSlot.MainHand,
                new Vector3(0.1f, 0, 0),
                Vector3.zero,
                Vector3.one);

            profile.ConfigureForTest("Dwarf", 0.8f, new[] { profileHead, profileMainHand });
            _bodyScale.BaseProfile = profile;

            // Instance overrides MainHand with a unique offset, leaving Head to inherit from profile
            var instanceMainHand = new SocketOffsetOverride(
                EquipmentSlot.MainHand,
                new Vector3(0.5f, 0.2f, 0),
                Vector3.zero,
                new Vector3(1.1f, 1.1f, 1.1f));

            _bodyScale.ConfigureForTest(profile, false, 1f, new[] { instanceMainHand });

            // 1. Head is inherited from profile
            Assert.IsTrue(_bodyScale.TryGetSocketOverride(EquipmentSlot.Head, out var headResult));
            Assert.AreEqual(new Vector3(0, -0.05f, 0), headResult.positionOffset);

            // 2. MainHand is overridden by the specific character instance
            Assert.IsTrue(_bodyScale.TryGetSocketOverride(EquipmentSlot.MainHand, out var mainHandResult));
            Assert.AreEqual(new Vector3(0.5f, 0.2f, 0), mainHandResult.positionOffset);
            Assert.AreEqual(new Vector3(1.1f, 1.1f, 1.1f), mainHandResult.scaleMultiplier);

            // 3. Unconfigured slot returns false
            Assert.IsFalse(_bodyScale.TryGetSocketOverride(EquipmentSlot.Chest, out _));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void CharacterBodyScale_MorphWeights_MergesProfileAndInstanceOverrides()
        {
            var profile = ScriptableObject.CreateInstance<BodyScaleProfileSO>();
            profile.ConfigureForTest(
                "Dwarf",
                0.8f,
                null,
                new[]
                {
                    new BlendShapeWeightEntry("Shape_Dwarf", 100f),
                    new BlendShapeWeightEntry("Shape_Bulk", 40f)
                });

            // Instance overrides Shape_Bulk to 80 and adds Shape_Nose = 60
            _bodyScale.ConfigureForTest(
                profile,
                false,
                1f,
                null,
                new[]
                {
                    new BlendShapeWeightEntry("Shape_Bulk", 80f),
                    new BlendShapeWeightEntry("Shape_Nose", 60f)
                });

            var morphs = _bodyScale.GetEffectiveMorphWeights();

            Assert.AreEqual(3, morphs.Count);
            Assert.AreEqual(100f, morphs["Shape_Dwarf"]);
            Assert.AreEqual(80f, morphs["Shape_Bulk"]); // Overridden!
            Assert.AreEqual(60f, morphs["Shape_Nose"]);

            Object.DestroyImmediate(profile);
        }
    }
}
