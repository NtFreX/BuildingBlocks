namespace NtFreX.BuildingBlocks.Light
{

    internal struct PointLightCollectionInfo
    {
        public static int MaxLights => 12;

        public PointLightInfo LightInfo0;
        public PointLightInfo LightInfo1;
        public PointLightInfo LightInfo2;
        public PointLightInfo LightInfo3;
        public PointLightInfo LightInfo4;
        public PointLightInfo LightInfo5;
        public PointLightInfo LightInfo6;
        public PointLightInfo LightInfo7;
        public PointLightInfo LightInfo8;
        public PointLightInfo LightInfo9;
        public PointLightInfo LightInfo10;
        public PointLightInfo LightInfo11;
        public int ActivePointLights;
        private float padding_0 = 0;
        private float padding_1 = 0;
        private float padding_2 = 0;

        public PointLightCollectionInfo()
        {
            ActivePointLights = 0;
            LightInfo0 = default;
            LightInfo1 = default;
            LightInfo2 = default;
            LightInfo3 = default;
            LightInfo4 = default;
            LightInfo5 = default;
            LightInfo6 = default;
            LightInfo7 = default;
            LightInfo8 = default;
            LightInfo9 = default;
            LightInfo10 = default;
            LightInfo11 = default;
        }

        public PointLightInfo this[int index]
        {
            get
            {
                return index switch
                {
                    0 => LightInfo0,
                    1 => LightInfo1,
                    2 => LightInfo2,
                    3 => LightInfo3,
                    4 => LightInfo4,
                    5 => LightInfo5,
                    6 => LightInfo6,
                    7 => LightInfo7,
                    8 => LightInfo8,
                    9 => LightInfo9,
                    10 => LightInfo10,
                    11 => LightInfo11,
                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0: LightInfo0 = value; break;
                    case 1: LightInfo1 = value; break;
                    case 2: LightInfo2 = value; break;
                    case 3: LightInfo3 = value; break;
                    case 4: LightInfo4 = value; break;
                    case 5: LightInfo5 = value; break;
                    case 6: LightInfo6 = value; break;
                    case 7: LightInfo7 = value; break;
                    case 8: LightInfo8 = value; break;
                    case 9: LightInfo9 = value; break;
                    case 10: LightInfo10 = value; break;
                    case 11: LightInfo11 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}
