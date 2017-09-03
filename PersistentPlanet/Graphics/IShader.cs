using System;

namespace PersistentPlanet.Graphics
{
    public interface IShader : IDisposable
    {
        void Initialise(IInitialiseContext context);
        void Apply(IRenderContext renderContext);
    }
}