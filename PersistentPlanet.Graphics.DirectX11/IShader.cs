using System;

namespace PersistentPlanet.Graphics.DirectX11
{
    public interface IShader : IDisposable
    {
        void Initialise(D11InitialiseContext context);
        void Apply(D11RenderContext renderContext);
    }
}