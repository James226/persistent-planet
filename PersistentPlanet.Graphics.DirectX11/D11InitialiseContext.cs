﻿using SharpDX.Direct3D11;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class D11InitialiseContext : InitialiseContext, IInitialiseContext
    {
        public DeviceContext DeviceContext { get; set; }
    }
}