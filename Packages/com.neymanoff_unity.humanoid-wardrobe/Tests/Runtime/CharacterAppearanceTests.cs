using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class CharacterAppearanceTests
    {
        private GameObject _characterA;
        private WardrobeManager _wardrobeA;
        private CharacterAppearance _appearanceA;

        private GameObject _characterB;
        private CharacterAppearance _appearanceB;

        private Material _sharedHairMaterial;
        private GameObject _hairShortPrefab;
        private GameObject _hairKnotPrefab;
        private GameObject _beardFullPrefab;
        private GameObject _equipmentDummyPrefab;

        private CosmeticOptionSO _hairShortOption;
        private CosmeticOptionSO _hairKnotOption;
        private CosmeticOptionSO _beardFullOption;
        private CosmeticCatalogSO _catalog;

        [SetUp]
        public void SetUp()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            _sharedHairMaterial = new Material(shader)
            {
                color = Color.white
            };

            _hairShortPrefab = CreateCosmeticPrefabWithRenderer("HairShortPrefab", _sharedHairMaterial);
            _hairKnotPrefab = CreateCosmeticPrefabWithRenderer("HairKnotPrefab", _sharedHairMaterial);
            _beardFullPrefab = CreateCosmeticPrefabWithRenderer("BeardFullPrefab", _sharedHairMaterial);
            _equipmentDummyPrefab = new GameObject("EquipmentDummyPrefab");

            _hairShortOption = ScriptableObject.CreateInstance<CosmeticOptionSO>();
            _hairShortOption.ConfigureForTest("hair_short", "Short Hair", CosmeticCategory.Hair, _hairShortPrefab);

            _hairKnotOption = ScriptableObject.CreateInstance<CosmeticOptionSO>();
            _hairKnotOption.ConfigureForTest("hair_knot", "Top Knot", CosmeticCategory.Hair, _hairKnotPrefab);

            _beardFullOption = ScriptableObject.CreateInstance<CosmeticOptionSO>();
            _beardFullOption.ConfigureForTest("beard_full", "Full Beard", CosmeticCategory.Beard, _beardFullPrefab);

            _catalog = ScriptableObject.CreateInstance<CosmeticCatalogSO>();
            _catalog.ConfigureForTest(
                new[] { _hairShortOption, _hairKnotOption },
                new[] { _beardFullOption },
                new[]
                {
                    new CosmeticColorPreset
                    {
                        presetId = "blonde",
                        displayName = "Blonde",
                        color = new Color(0.9f, 0.8f, 0.3f, 1f)
                    }
                });

            _characterA = new GameObject("CharacterA");
            _characterA.AddComponent<Animator>();
            _wardrobeA = _characterA.AddComponent<WardrobeManager>();
            _appearanceA = _characterA.AddComponent<CharacterAppearance>();
            _appearanceA.Catalog = _catalog;

            _characterB = new GameObject("CharacterB");
            _characterB.AddComponent<Animator>();
            _appearanceB = _characterB.AddComponent<CharacterAppearance>();
            _appearanceB.Catalog = _catalog;
        }

        [TearDown]
        public void TearDown()
        {
            if (_characterA != null) Object.DestroyImmediate(_characterA);
            if (_characterB != null) Object.DestroyImmediate(_characterB);
            if (_hairShortPrefab != null) Object.DestroyImmediate(_hairShortPrefab);
            if (_hairKnotPrefab != null) Object.DestroyImmediate(_hairKnotPrefab);
            if (_beardFullPrefab != null) Object.DestroyImmediate(_beardFullPrefab);
            if (_equipmentDummyPrefab != null) Object.DestroyImmediate(_equipmentDummyPrefab);
            if (_hairShortOption != null) Object.DestroyImmediate(_hairShortOption);
            if (_hairKnotOption != null) Object.DestroyImmediate(_hairKnotOption);
            if (_beardFullOption != null) Object.DestroyImmediate(_beardFullOption);
            if (_catalog != null) Object.DestroyImmediate(_catalog);
            if (_sharedHairMaterial != null) Object.DestroyImmediate(_sharedHairMaterial);
        }

        private static GameObject CreateCosmeticPrefabWithRenderer(string name, Material sharedMat)
        {
            GameObject root = new(name);
            root.AddComponent<HumanoidAttachmentPoint>();
            GameObject meshChild = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshChild.name = "CosmeticMesh";
            meshChild.transform.SetParent(root.transform, false);
            Renderer rend = meshChild.GetComponent<Renderer>();
            rend.sharedMaterial = sharedMat;
            return root;
        }

        [Test]
        public void EquipmentSlotEnum_ValuesRemainUnchanged()
        {
            Assert.AreEqual(0, (int)EquipmentSlot.Head);
            Assert.AreEqual(1, (int)EquipmentSlot.Shoulders);
            Assert.AreEqual(2, (int)EquipmentSlot.Chest);
            Assert.AreEqual(3, (int)EquipmentSlot.Hands);
            Assert.AreEqual(4, (int)EquipmentSlot.Legs);
            Assert.AreEqual(5, (int)EquipmentSlot.Feet);
            Assert.AreEqual(6, (int)EquipmentSlot.Neck);
            Assert.AreEqual(7, (int)EquipmentSlot.Back);
            Assert.AreEqual(8, (int)EquipmentSlot.MainHand);
            Assert.AreEqual(9, (int)EquipmentSlot.OffHand);
            Assert.AreEqual(10, (int)EquipmentSlot.LeftRing);
            Assert.AreEqual(11, (int)EquipmentSlot.RightRing);
            Assert.AreEqual(12, (int)EquipmentSlot.TacticalVest);
            Assert.AreEqual(13, (int)EquipmentSlot.Backpack);
            Assert.AreEqual(14, (int)EquipmentSlot.Holster);
            Assert.AreEqual(15, (int)EquipmentSlot.FaceMask);
            Assert.AreEqual(16, (int)EquipmentSlot.Belt);
        }

        [Test]
        public void SetHairAndBeard_IndependentSelectionAndClearing_CoexistsWithHeadAndFaceMaskEquipment()
        {
            // Select both hair and beard
            Assert.IsTrue(_appearanceA.SetHair("hair_short"));
            Assert.IsTrue(_appearanceA.SetBeard("beard_full"));

            Assert.AreEqual("hair_short", _appearanceA.CurrentHairId);
            Assert.AreEqual("beard_full", _appearanceA.CurrentBeardId);
            Assert.IsNotNull(_appearanceA.CurrentHairInstance);
            Assert.IsNotNull(_appearanceA.CurrentBeardInstance);

            // Equip Head and FaceMask items that do not hide cosmetics
            var circlet = ScriptableObject.CreateInstance<WardrobeItemSO>();
            circlet.ConfigureForTest("circlet", "Circlet", _equipmentDummyPrefab, new[] { EquipmentSlot.Head });

            var glasses = ScriptableObject.CreateInstance<WardrobeItemSO>();
            glasses.ConfigureForTest("glasses", "Glasses", _equipmentDummyPrefab, new[] { EquipmentSlot.FaceMask });

            Assert.IsTrue(_wardrobeA.Equip(circlet, EquipmentSlot.Head).IsSuccess);
            Assert.IsTrue(_wardrobeA.Equip(glasses, EquipmentSlot.FaceMask).IsSuccess);

            // Both equipment slots and both cosmetics coexist simultaneously
            Assert.IsTrue(_wardrobeA.IsSlotOccupied(EquipmentSlot.Head));
            Assert.IsTrue(_wardrobeA.IsSlotOccupied(EquipmentSlot.FaceMask));
            Assert.IsTrue(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsTrue(_appearanceA.CurrentBeardInstance.activeSelf);

            // Clear hair ("None") while beard and head equipment stay intact
            _appearanceA.ClearHair();
            Assert.AreEqual(string.Empty, _appearanceA.CurrentHairId);
            Assert.IsNull(_appearanceA.CurrentHairInstance);
            Assert.AreEqual("beard_full", _appearanceA.CurrentBeardId);
            Assert.IsNotNull(_appearanceA.CurrentBeardInstance);
            Assert.IsTrue(_wardrobeA.IsSlotOccupied(EquipmentSlot.Head));

            // Clear beard via empty string
            Assert.IsTrue(_appearanceA.SetBeard(string.Empty));
            Assert.AreEqual(string.Empty, _appearanceA.CurrentBeardId);
            Assert.IsNull(_appearanceA.CurrentBeardInstance);

            Object.DestroyImmediate(circlet);
            Object.DestroyImmediate(glasses);
        }

        [Test]
        public void ColorSelection_LinkedAndIndependentColors_DoNotAffectOtherCharactersOrSharedMaterial()
        {
            _appearanceA.SetHair("hair_short");
            _appearanceA.SetBeard("beard_full");

            _appearanceB.SetHair("hair_short");
            _appearanceB.SetBeard("beard_full");

            Color crimson = new(0.85f, 0.1f, 0.15f, 1f);
            Color teal = new(0.1f, 0.75f, 0.8f, 1f);
            Color silver = new(0.8f, 0.82f, 0.85f, 1f);

            // 1. Linked colors on Character A
            _appearanceA.LinkHairAndBeardColor = true;
            _appearanceA.SetHairColor(crimson);

            Assert.AreEqual(crimson, _appearanceA.HairColor);
            Assert.AreEqual(crimson, _appearanceA.BeardColor);

            // 2. Independent colors on Character B
            _appearanceB.SetColors(teal, silver, linkColors: false);

            Assert.AreEqual(teal, _appearanceB.HairColor);
            Assert.AreEqual(silver, _appearanceB.BeardColor);

            // 3. Verify per-instance MaterialPropertyBlock values on Character A vs Character B
            Renderer rendHairA = _appearanceA.CurrentHairInstance.GetComponentInChildren<Renderer>();
            Renderer rendBeardA = _appearanceA.CurrentBeardInstance.GetComponentInChildren<Renderer>();
            Renderer rendHairB = _appearanceB.CurrentHairInstance.GetComponentInChildren<Renderer>();
            Renderer rendBeardB = _appearanceB.CurrentBeardInstance.GetComponentInChildren<Renderer>();

            Assert.IsTrue(CharacterAppearance.TryGetAppliedRendererColor(rendHairA, out Color actualHairA));
            Assert.IsTrue(CharacterAppearance.TryGetAppliedRendererColor(rendBeardA, out Color actualBeardA));
            Assert.IsTrue(CharacterAppearance.TryGetAppliedRendererColor(rendHairB, out Color actualHairB));
            Assert.IsTrue(CharacterAppearance.TryGetAppliedRendererColor(rendBeardB, out Color actualBeardB));

            AssertColorApproximatelyEqual(crimson, actualHairA);
            AssertColorApproximatelyEqual(crimson, actualBeardA);
            AssertColorApproximatelyEqual(teal, actualHairB);
            AssertColorApproximatelyEqual(silver, actualBeardB);

            // 4. Shared project material must remain untouched (Color.white)
            AssertColorApproximatelyEqual(Color.white, _sharedHairMaterial.color);
        }

        [Test]
        public void VisibilitySuppression_MultipleSimultaneousSources_PreservesSelectionAndRestoresOnlyWhenAllCleared()
        {
            Color chosenHairColor = new(0.9f, 0.7f, 0.2f, 1f);
            Color chosenBeardColor = new(0.4f, 0.2f, 0.1f, 1f);

            _appearanceA.SetColors(chosenHairColor, chosenBeardColor, linkColors: false);
            _appearanceA.SetHair("hair_short");
            _appearanceA.SetBeard("beard_full");

            Assert.IsTrue(_appearanceA.IsHairVisible);
            Assert.IsTrue(_appearanceA.IsBeardVisible);

            // Source 1: Equip a helmet that hides Hair only
            var helmet = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helmet.ConfigureForTest(
                "closed_helm",
                "Closed Helmet",
                _equipmentDummyPrefab,
                new[] { EquipmentSlot.Head },
                hiddenCosmeticMask: CosmeticVisibilityMask.Hair);

            _wardrobeA.Equip(helmet, EquipmentSlot.Head);

            Assert.IsFalse(_appearanceA.IsHairVisible);
            Assert.IsFalse(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsTrue(_appearanceA.IsBeardVisible);
            Assert.IsTrue(_appearanceA.CurrentBeardInstance.activeSelf);

            // Selection IDs and colors must NOT be reset while hidden
            Assert.AreEqual("hair_short", _appearanceA.CurrentHairId);
            AssertColorApproximatelyEqual(chosenHairColor, _appearanceA.HairColor);

            // Source 2: External visual outfit adapter suppresses both Hair and Beard
            _appearanceA.SetVisibilityRestriction("ClassOutfit_Hood", CosmeticVisibilityMask.All);

            Assert.IsFalse(_appearanceA.IsHairVisible);
            Assert.IsFalse(_appearanceA.IsBeardVisible);
            Assert.IsFalse(_appearanceA.CurrentBeardInstance.activeSelf);

            // Removing Source 1 (unequipping helmet) must NOT reveal Hair because Source 2 still hides Hair
            _wardrobeA.Unequip(EquipmentSlot.Head);

            Assert.IsFalse(_appearanceA.IsHairVisible);
            Assert.IsFalse(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsFalse(_appearanceA.IsBeardVisible);

            // Removing Source 2 restores both Hair and Beard with their chosen models and colors
            Assert.IsTrue(_appearanceA.RemoveVisibilityRestriction("ClassOutfit_Hood"));

            Assert.IsTrue(_appearanceA.IsHairVisible);
            Assert.IsTrue(_appearanceA.IsBeardVisible);
            Assert.IsTrue(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsTrue(_appearanceA.CurrentBeardInstance.activeSelf);
            Assert.AreEqual("hair_short", _appearanceA.CurrentHairId);
            Assert.AreEqual("beard_full", _appearanceA.CurrentBeardId);

            Renderer hairRenderer = _appearanceA.CurrentHairInstance.GetComponentInChildren<Renderer>();
            Assert.IsTrue(CharacterAppearance.TryGetAppliedRendererColor(hairRenderer, out Color restoredColor));
            AssertColorApproximatelyEqual(chosenHairColor, restoredColor);

            Object.DestroyImmediate(helmet);
        }

        [Test]
        public void HideHelmetVisual_WhenHelmetEquipped_KeepsHelmetEquippedWhileShowingHeadHairAndBeard()
        {
            // Configure a modular Head body part renderer on Character A
            GameObject headMeshObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMeshObj.name = "CharacterHeadMesh";
            headMeshObj.transform.SetParent(_characterA.transform, false);
            Renderer headRenderer = headMeshObj.GetComponent<Renderer>();

            _wardrobeA.ConfigureModularPartsForTest(new[]
            {
                new WardrobeManager.ModularBodyPart
                {
                    part = BodyPartMask.Head,
                    renderer = headRenderer
                }
            });

            _appearanceA.SetHair("hair_short");
            _appearanceA.SetBeard("beard_full");

            var helmet = ScriptableObject.CreateInstance<WardrobeItemSO>();
            helmet.ConfigureForTest(
                "full_plate_helm",
                "Full Plate Helmet",
                _equipmentDummyPrefab,
                new[] { EquipmentSlot.Head },
                hiddenParts: BodyPartMask.Head,
                hiddenCosmeticMask: CosmeticVisibilityMask.Hair);

            EquipResult equipResult = _wardrobeA.Equip(helmet, EquipmentSlot.Head);
            Assert.IsTrue(equipResult.IsSuccess);
            GameObject helmetInstance = _wardrobeA.GetEquippedItem(EquipmentSlot.Head);
            Assert.IsNotNull(helmetInstance);

            // By default (HideHelmetVisual == false): helmet is shown, head mesh and hair are hidden
            Assert.IsFalse(_wardrobeA.HideHelmetVisual);
            Assert.IsTrue(helmetInstance.activeSelf);
            Assert.IsFalse(headRenderer.enabled);
            Assert.IsFalse(_appearanceA.IsHairVisible);
            Assert.IsFalse(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsTrue(_appearanceA.IsBeardVisible);

            // Enable "Hide Helmet": helmet stays equipped in Head slot, but its 3D mesh is hidden and Head + Hair remain visible
            _appearanceA.SetHelmetVisualHidden(true);

            Assert.IsTrue(_wardrobeA.HideHelmetVisual);
            Assert.IsTrue(_appearanceA.HideHelmetVisual);
            Assert.IsTrue(_wardrobeA.IsSlotOccupied(EquipmentSlot.Head));
            Assert.AreSame(helmet, _wardrobeA.GetEquippedItemData(EquipmentSlot.Head));
            Assert.IsFalse(helmetInstance.activeSelf);
            Assert.IsTrue(headRenderer.enabled);
            Assert.IsTrue(_appearanceA.IsHairVisible);
            Assert.IsTrue(_appearanceA.CurrentHairInstance.activeSelf);
            Assert.IsTrue(_appearanceA.IsBeardVisible);
            Assert.IsTrue(_appearanceA.CurrentBeardInstance.activeSelf);

            // Disable "Hide Helmet": helmet 3D mesh reappears and hides Head + Hair again
            _wardrobeA.ToggleHelmetVisual();

            Assert.IsFalse(_wardrobeA.HideHelmetVisual);
            Assert.IsTrue(helmetInstance.activeSelf);
            Assert.IsFalse(headRenderer.enabled);
            Assert.IsFalse(_appearanceA.IsHairVisible);
            Assert.IsFalse(_appearanceA.CurrentHairInstance.activeSelf);

            Object.DestroyImmediate(helmet);
        }

        private static void AssertColorApproximatelyEqual(Color expected, Color actual, float tolerance = 1e-3f)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance, $"Red channel mismatch: expected {expected}, got {actual}");
            Assert.AreEqual(expected.g, actual.g, tolerance, $"Green channel mismatch: expected {expected}, got {actual}");
            Assert.AreEqual(expected.b, actual.b, tolerance, $"Blue channel mismatch: expected {expected}, got {actual}");
            Assert.AreEqual(expected.a, actual.a, tolerance, $"Alpha channel mismatch: expected {expected}, got {actual}");
        }

        [Test]
        public void Persistence_SaveAndRestoreRoundtrip_RestoresExactOptionsAndColors()
        {
            Color savedHairColor = new(0.65f, 0.2f, 0.1f, 1f);
            Color savedBeardColor = new(0.15f, 0.15f, 0.18f, 1f);

            _appearanceA.SetHair("hair_knot");
            _appearanceA.SetBeard("beard_full");
            _appearanceA.SetColors(savedHairColor, savedBeardColor, linkColors: false);

            CharacterAppearanceData savedDto = _appearanceA.GetAppearanceData();
            string json = savedDto.ToJson();

            // Clear Character A completely
            _appearanceA.ClearAllCosmetics();
            _appearanceA.SetColors(Color.white, Color.white, linkColors: true);
            Assert.AreEqual(string.Empty, _appearanceA.CurrentHairId);
            Assert.AreEqual(string.Empty, _appearanceA.CurrentBeardId);

            // Restore from JSON onto Character A
            CharacterAppearanceData loadedDto = CharacterAppearanceData.FromJson(json);
            AppearanceApplyResult result = _appearanceA.ApplyAppearanceData(loadedDto);

            Assert.IsTrue(result.IsFullyResolved);
            Assert.IsFalse(result.HairIdMissing);
            Assert.IsFalse(result.BeardIdMissing);
            Assert.AreEqual("hair_knot", _appearanceA.CurrentHairId);
            Assert.AreEqual("beard_full", _appearanceA.CurrentBeardId);
            Assert.AreEqual(savedHairColor, _appearanceA.HairColor);
            Assert.AreEqual(savedBeardColor, _appearanceA.BeardColor);
            Assert.IsFalse(_appearanceA.LinkHairAndBeardColor);
        }

        [Test]
        public void Persistence_WhenOptionIdIsMissing_HandlesGracefullyWithoutExceptionOrDataLoss()
        {
            Color customHairColor = new(0.3f, 0.5f, 0.9f, 1f);
            Color customBeardColor = new(0.8f, 0.4f, 0.2f, 1f);

            CharacterAppearanceData dtoWithMissingHair = new(
                "non_existent_hair_id",
                "beard_full",
                customHairColor,
                customBeardColor,
                linkColors: false);

            LogAssert.Expect(
                LogType.Warning,
                "[CharacterAppearance] Saved hair option 'non_existent_hair_id' could not be resolved on CharacterA. Falling back to 'None'.");

            AppearanceApplyResult result = _appearanceA.ApplyAppearanceData(dtoWithMissingHair);

            Assert.IsFalse(result.IsFullyResolved);
            Assert.IsTrue(result.HairIdMissing);
            Assert.AreEqual("non_existent_hair_id", result.MissingHairId);
            Assert.IsFalse(result.BeardIdMissing);

            // Missing hair falls back to "None", while valid beard and both colors are preserved
            Assert.AreEqual(string.Empty, _appearanceA.CurrentHairId);
            Assert.IsNull(_appearanceA.CurrentHairInstance);
            Assert.AreEqual("beard_full", _appearanceA.CurrentBeardId);
            Assert.IsNotNull(_appearanceA.CurrentBeardInstance);
            Assert.AreEqual(customHairColor, _appearanceA.HairColor);
            Assert.AreEqual(customBeardColor, _appearanceA.BeardColor);
        }
    }
}
