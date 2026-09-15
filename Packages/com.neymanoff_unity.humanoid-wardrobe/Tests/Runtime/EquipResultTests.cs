using NUnit.Framework;
using UnityEngine;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class EquipResultTests
    {
        [Test]
        public void Succeeded_SetsIsSuccessTrueAndRetainsInstance()
        {
            var item = ScriptableObject.CreateInstance<WardrobeItemSO>();
            var dummyObj = new GameObject("DummyInstance");
            var instance = new EquippedItemInstance(item, dummyObj, EquipmentSlot.Head, new[] { EquipmentSlot.Head });

            var result = EquipResult.Succeeded(instance);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(EquipResultStatus.Success, result.Status);
            Assert.AreSame(instance, result.Instance);
            Assert.IsNull(result.ErrorMessage);

            Object.DestroyImmediate(dummyObj);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void Failed_SetsIsSuccessFalseAndCapturesErrorMessage()
        {
            var result = EquipResult.Failed(EquipResultStatus.InvalidSlot, "Requested slot is not valid for this item.");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(EquipResultStatus.InvalidSlot, result.Status);
            Assert.IsNull(result.Instance);
            Assert.AreEqual("Requested slot is not valid for this item.", result.ErrorMessage);
        }

        [Test]
        public void Failed_WithMissingBone_ReflectsMissingBoneStatus()
        {
            var result = EquipResult.Failed(EquipResultStatus.MissingBone, "Target bone LeftHand not found on avatar.");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(EquipResultStatus.MissingBone, result.Status);
            Assert.IsTrue(result.ErrorMessage.Contains("LeftHand"));
        }
    }
}
