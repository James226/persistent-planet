using System;

namespace PersistentPlanet
{
    public interface IShader : IDisposable
    {
        void Initialise(IInitialiseContext context);
        void Apply(IRenderContext renderContext, GameObject gameObject);
    }
}