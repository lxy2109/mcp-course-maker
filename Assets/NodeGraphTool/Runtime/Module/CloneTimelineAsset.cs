using UnityEngine;
using UnityEngine.Playables;



using UnityEditor;

public class CloneTimelineAsset : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("TimelineAsset所在的timeline")]
    public GameObject clonedTimeline;

    private int count = 0;

    public void GetAssetInEditor()
    {
        DuplicateWithBindings();
    }

    public bool DuplicateWithBindingsValidate()
    {
        if (UnityEditor.Selection.activeGameObject == null)
            return false;

        var playableDirector = clonedTimeline.GetComponent<PlayableDirector>();

        if (playableDirector == null)
            return false;

        var playableAsset = playableDirector.playableAsset;
        if (playableAsset == null)
            return false;

        var path = AssetDatabase.GetAssetPath(playableAsset);
        if (string.IsNullOrEmpty(path))
            return false;

        return true;
    }

    public void DuplicateWithBindings()
    {
        if (UnityEditor.Selection.activeGameObject == null)
        { return; }

        var playableDirector = clonedTimeline.GetComponent<PlayableDirector>();
        PlayableAsset asset = clonedTimeline.GetComponent<PlayableDirector>().playableAsset;
        if (playableDirector == null) { return; }

        var playableAsset = playableDirector.playableAsset;
        if (playableAsset == null) { return; }

        count++;
        var path = AssetDatabase.GetAssetPath(playableAsset);

        if (string.IsNullOrEmpty(path)) { return; }

        string newPath = path.Replace(".", count.ToString() + ".");
        if (!AssetDatabase.CopyAsset(path, newPath))
        {
            Debug.LogError("Couldn't Clone Asset");
            return;
        }

        var newPlayableAsset = AssetDatabase.LoadMainAssetAtPath(newPath) as PlayableAsset;
        var newPlayableDirector = clonedTimeline.GetComponent<PlayableDirector>();
        newPlayableDirector.playableAsset = newPlayableAsset;

        var oldBindings = playableAsset.outputs.GetEnumerator();
        var newBindings = newPlayableAsset.outputs.GetEnumerator();

        while (oldBindings.MoveNext())
        {
            var oldBindings_sourceObject = oldBindings.Current.sourceObject;

            newBindings.MoveNext();

            var newBindings_sourceObject = newBindings.Current.sourceObject;

            newPlayableDirector.SetGenericBinding(
                newBindings_sourceObject,
                playableDirector.GetGenericBinding(oldBindings_sourceObject)
            );
        }
        clonedTimeline.GetComponent<PlayableDirector>().playableAsset = asset;
    }
#endif
}

