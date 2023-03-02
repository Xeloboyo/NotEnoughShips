using UnityEngine;

namespace NotEnoughShips;

public static class Utils
{
    public static void PrintObject(this GameObject g,int layer = 0, int limit = 999)
    {
        if(!g)
        {
            Debug.Log($"NULL >=== ");
        }
        string pad = "";
        for (int i = 0; i < layer; i++)
        {
            pad += " ";
        }
        Debug.Log($"{pad}{g.name} >=== ");
        Debug.Log($"{pad}Components ");
        foreach (var comp in g.GetComponents<Component>())
        {
            Debug.Log($"{pad} - {comp.GetType().Name}");
        }
        if(layer==limit)
        {
            return;
        }
        foreach (Transform child in g.transform)
        {
            Debug.Log($"{pad}>=== ");
            PrintObject(child.gameObject, layer+2);
        }
    }
    public static GameObject GetChild(this GameObject m, String name)
    {
        foreach (Transform child in m.transform)
        {
            if(child.gameObject.name.ToLower()==name.ToLower())
            {
                return child.gameObject;
            }
        }
        return null;
    }
    public static GameObject GetChild(this MonoBehaviour m, String name)
    {
        return GetChild(m.gameObject,name);
    }
}
