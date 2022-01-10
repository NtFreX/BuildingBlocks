﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NtFreX.BuildingBlocks.Desktop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LightInfo
    {
        public const int MaxLights = 4;

        public int ActivePointLights;
        public Vector3 _buffer1;

        public Vector3 AmbientLight;
        public int _buffer2;

        public PointLightInfo LightInfo0;
        public PointLightInfo LightInfo1;
        public PointLightInfo LightInfo2;
        public PointLightInfo LightInfo3;

        public LightInfo()
        {
            AmbientLight = Vector3.One;
            ActivePointLights = 0;
            LightInfo0 = default(PointLightInfo);
            LightInfo1 = default(PointLightInfo);
            LightInfo2 = default(PointLightInfo);
            LightInfo3 = default(PointLightInfo);
            _buffer1 = Vector3.One;
            _buffer2 = 0;
        }

        public PointLightInfo this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return LightInfo0;
                    case 1: return LightInfo1;
                    case 2: return LightInfo2;
                    case 3: return LightInfo3;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0: LightInfo0 = value; break;
                    case 1: LightInfo1 = value; break;
                    case 2: LightInfo2 = value; break;
                    case 3: LightInfo3 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}