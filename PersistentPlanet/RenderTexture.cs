﻿using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PersistentPlanet
{
    public class RenderTexture : IDisposable
    {
        private Texture2D _texture;
        private RenderTargetView _renderTargetView;
        private ShaderResourceView _shaderResourceView;

        public void Initialise(InitialiseContext context)
        {
            int width = 800;
            int height = 600;

            var textureDescription = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _texture = new Texture2D(context.Device, textureDescription);

            var renderTargetViewDescription = new RenderTargetViewDescription
            {
                Format = textureDescription.Format,
                Dimension = RenderTargetViewDimension.Texture2D,
                Texture2D = new RenderTargetViewDescription.Texture2DResource {MipSlice = 0}
            };

            _renderTargetView = new RenderTargetView(context.Device, _texture, renderTargetViewDescription);

            var shaderResourceViewDescription = new ShaderResourceViewDescription
            {
                Format = textureDescription.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource
                {
                    MostDetailedMip = 0,
                    MipLevels = 1
                }
            };

            _shaderResourceView = new ShaderResourceView(context.Device, _texture, shaderResourceViewDescription);
        }

        public void Dispose()
        {
            _shaderResourceView.Dispose();
            _renderTargetView.Dispose();
            _texture.Dispose();
        }

        public void Apply(RenderContext context)
        {
            //context.Context.OutputMerger.SetRenderTargets(_);
        }
    }
}