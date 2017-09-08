using System;
using MemBus;

namespace PersistentPlanet.Graphics
{
    public interface IGenericRenderer<TInitialiseContext, TRenderContext> 
        where TInitialiseContext : IInitialiseContext 
        where TRenderContext : IRenderContext
    {
        IGenericShader<TInitialiseContext, TRenderContext> CreateShader();

        (TInitialiseContext, Func<TRenderContext>) Initialise(IRenderWindow renderWindow, IBus bus);
        void Render(TRenderContext context, Action render);
    }
}