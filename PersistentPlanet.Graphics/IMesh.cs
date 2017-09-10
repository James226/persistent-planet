namespace PersistentPlanet.Graphics
{
    public interface IMesh<in T> : IResource<T>, IMesh
        where T : IRenderContext
    {
    }
    public interface IMesh
    {
    }
}