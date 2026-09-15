using NUnit.Framework;

namespace Neymanoff.HumanoidWardrobe.Tests
{
    [TestFixture]
    public class BodyPartMaskTests
    {
        [Test]
        public void Flags_None_HasZeroValue()
        {
            BodyPartMask mask = BodyPartMask.None;
            Assert.AreEqual(0, (int)mask);
        }

        [Test]
        public void Flags_SingleZone_HasFlagIsTrue()
        {
            BodyPartMask mask = BodyPartMask.UpperTorso;
            Assert.IsTrue(mask.HasFlag(BodyPartMask.UpperTorso));
            Assert.IsFalse(mask.HasFlag(BodyPartMask.LowerTorso));
            Assert.IsFalse(mask.HasFlag(BodyPartMask.Head));
        }

        [Test]
        public void Flags_CombinedZones_RecognizesAllIndividualFlags()
        {
            BodyPartMask cuirassMask = BodyPartMask.UpperTorso | BodyPartMask.LowerTorso;
            Assert.IsTrue(cuirassMask.HasFlag(BodyPartMask.UpperTorso));
            Assert.IsTrue(cuirassMask.HasFlag(BodyPartMask.LowerTorso));
            Assert.IsFalse(cuirassMask.HasFlag(BodyPartMask.LowerLegs));
        }

        [Test]
        public void Flags_BitwiseAggregation_CorrectlyAccumulatesMultipleItems()
        {
            // Item 1 (cuirass): hides UpperTorso + LowerTorso
            BodyPartMask item1 = BodyPartMask.UpperTorso | BodyPartMask.LowerTorso;
            // Item 2 (gauntlets): hides Hands + LowerArms
            BodyPartMask item2 = BodyPartMask.Hands | BodyPartMask.LowerArms;
            // Item 3 (greaves): hides UpperLegs + LowerLegs + Feet
            BodyPartMask item3 = BodyPartMask.UpperLegs | BodyPartMask.LowerLegs | BodyPartMask.Feet;

            BodyPartMask aggregated = item1 | item2 | item3;

            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.UpperTorso));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.LowerTorso));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.Hands));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.LowerArms));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.UpperLegs));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.LowerLegs));
            Assert.IsTrue(aggregated.HasFlag(BodyPartMask.Feet));
            // Head was not covered by any item
            Assert.IsFalse(aggregated.HasFlag(BodyPartMask.Head));
        }
    }
}
