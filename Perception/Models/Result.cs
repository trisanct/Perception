namespace Perception.Models
{
    public class Result
    {
        public int Id { get; set; }
        public PredictedClass Class { get; set; }
        public float Score { get; set; }
        public int RecordId { get; set; }
        public enum PredictedClass
        {
            AppleScabLeaf,
            AppleLeaf,
            AppleRustLeaf,
            BellPepperLeaf,
            BellPepperLeafSpot,
            BlueberryLeaf,
            CherryLeaf,
            CornGrayLeafSpot,
            CornLeafBlight,
            CornRustLeaf,
            PeachLeaf,
            PotatoLeaf,
            PotatoLeafEarlyBlight,
            PotatoLeafLateBlight,
            RaspberryLeaf,
            SoyabeanLeaf,
            SoybeanLeaf,
            SquashPowderyMildewLeaf,
            StrawberryLeaf,
            TomatoEarlyBlightLeaf,
            TomatoSeptoriaLeafSpot,
            TomatoLeaf,
            TomatoLeafBacterialSpot,
            TomatoLeafLateBlight,
            TomatoLeafMosaicVirus,
            TomatoLeafYellowVirus,
            TomatoMoldLeaf,
            TomatoTwoSpottedSpiderMitesLeaf,
            GrapeLeaf,
            GrapeLeafBlackRot,
            NotDetected
        }
    }
}