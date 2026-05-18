using Leopotam.EcsLite;

namespace _Project.Infrastructure
{
    public static class EcsEntityUtils
    {
        public static bool IsAlive(EcsWorld world, int entity)
        {
            return world != null
                   && entity >= 0
                   && entity < world.GetUsedEntitiesCount()
                   && world.GetEntityGen(entity) > 0;
        }
    }
}
