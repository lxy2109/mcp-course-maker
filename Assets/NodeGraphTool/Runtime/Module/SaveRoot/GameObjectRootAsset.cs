using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//[CreateAssetMenu(menuName = "root asset",fileName ="root asset",order =0)]
public class GameObjectRootAsset : SerializedScriptableObject
{
    public List<string> objectNames=new List<string>();
    public List<string> eventNames=new List<string>();
    public List<GameObjectState>  gameObjectStates=new List<GameObjectState>();

    public Dictionary<GameObjectStateKey, GameObjectState> ContactKeyValuePair()
    {
        Dictionary<GameObjectStateKey, GameObjectState> pool = new Dictionary<GameObjectStateKey, GameObjectState>();

        for (int i = 0; i < objectNames.Count; i++)
        {
            GameObjectStateKey key =new GameObjectStateKey();
            key.obj = SerializeSceneGameobject.GetGameObject(objectNames[i]);
            Debug.Log(key.obj);
            key.eventName = eventNames[i];
            KeyValuePair<GameObjectStateKey, GameObjectState> pair = new KeyValuePair<GameObjectStateKey, GameObjectState>(key, gameObjectStates[i]);
            pool.Add(key, gameObjectStates[i]);
        }
        return pool;
    }

}
