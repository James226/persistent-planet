namespace PersistentPlanet.Graphics
{
    public interface IGenericShader<in TInitialiseContext, in TRenderContext>
        where TInitialiseContext : IInitialiseContext
        where TRenderContext : IRenderContext
    {
        void Initialise(TInitialiseContext context);
        void Apply(TRenderContext context);
    }
}