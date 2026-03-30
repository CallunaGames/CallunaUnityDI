
namespace Calluna.DI
{
    public enum InstanceCreationMode
    {
        Undefined = 0,
        FromNew = 1,
        FromInstance = 2,
        FromMethod = 3,
        FromFactory = 4,
        FromPrefabInstance = 5,
        FromResourcePrefabInstance = 6,
		FromNewComponentOn = 7,
		FromNewComponentOnNewGameObject = 8,
		FromResources = 9,
	}
}