using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MMCharController))]
public class CharEditor : Editor
{
    List<string> anims;
    private void OnEnable()
    {
        MMCharController t = (MMCharController)target;
        anims = t.GetAllAnims();
    }
    public override void OnInspectorGUI()
    {
        MMCharController t = (MMCharController)target;
        base.OnInspectorGUI();


        //moveset validation
        if (t.currentMoveset == null)
        {
            EditorGUILayout.LabelField("No Move-set Found");
        }
        else
        {
            if (GUILayout.Button("Edit Movesets"))
            {
                MovesetEditorWindow.OpenWindow((MMCharController)target);
            }
        }



        //animator validation
        if (!t.animationController)
        {
            EditorGUILayout.LabelField("No Animation Component Found");
        }
        else
        {

            if (t.animationController.GetClipCount() <= 0)
            {
                EditorGUILayout.LabelField("No Animations");
            }
            else
            {
                if (GUILayout.Button("Update Animation List"))
                {
                    UpdateList();
                }

                t.showConstantAnimations = EditorGUILayout.Foldout(t.showConstantAnimations, "Animation Constants");
                if (t.showConstantAnimations)
                {
                    //idle
                    t.idleIndex = EditorGUILayout.Popup("Idle:", t.idleIndex, anims.ToArray());
                    Mathf.Clamp(t.idleIndex, 0, anims.Count - 1);

                    t.idleAnimation = anims[t.idleIndex];

                    //move
                    t.moveIndex = EditorGUILayout.Popup("Moving:", t.moveIndex, anims.ToArray());
                    Mathf.Clamp(t.moveIndex, 0, anims.Count - 1);
                    t.moveAnimation = anims[t.moveIndex];

                    //hurt
                    t.hurtIndex = EditorGUILayout.Popup("Hurt:", t.hurtIndex, anims.ToArray());
                    Mathf.Clamp(t.hurtIndex, 0, anims.Count - 1);
                    t.hurtAnimation = anims[t.hurtIndex];

                    //airborne
                    t.airborneIndex = EditorGUILayout.Popup("Airborne:", t.airborneIndex, anims.ToArray());
                    Mathf.Clamp(t.airborneIndex, 0, anims.Count - 1);
                    t.airborneAnimation = anims[t.airborneIndex];
                }

                t.showBannedAnimations = EditorGUILayout.Foldout(t.showBannedAnimations, "Banned Animations");
                if (t.showBannedAnimations)
                {
                    if (t.bannedIndexs.Count > 0 && t.bannedIndexs != null)
                    {
                        for (int i = 0; i < t.bannedIndexs.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            t.bannedIndexs[i] = EditorGUILayout.Popup("Anim Name:", t.bannedIndexs[i], anims.ToArray());
                            if (GUILayout.Button("X"))
                            {
                                t.bannedIndexs.Remove(t.bannedIndexs[i]);
                                break;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    if (GUILayout.Button("Add new Banned Animation"))
                    {
                        if (t.bannedIndexs == null)
                        {
                            t.bannedIndexs = new List<int>();
                        }

                        t.bannedIndexs.Add(0);
                    }
                }
            }
        }
    }

    void UpdateList()
    {
        MMCharController t = (MMCharController)target;
        anims = t.GetAllAnims();
    }
}
