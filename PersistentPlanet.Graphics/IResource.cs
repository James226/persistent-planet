namespace PersistentPlanet.Graphics
{
    public interface IResource<in T> where T : IRenderContext
    {
        void Render(T context);
    }
}