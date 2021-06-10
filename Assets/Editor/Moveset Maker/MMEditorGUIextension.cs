using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MMEditorGUIextension
{
    [MenuItem("Move-set Maker/Add Blank Character")]
    static void CreateBlankCharacter()
    {
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath("Assets/Prefabs/BlankMMChar.prefab", typeof(GameObject)));

    }

}