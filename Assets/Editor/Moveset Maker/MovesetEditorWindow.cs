using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MovesetEditorWindow : EditorWindow
{
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle labelStyle;
    
    private Vector2 scrollPos;
    private Vector2 drag;
    static MoveSetAction selectedAction;
    private static MoveSet moveSet;
    private static MMCharController character;

    private Rect workSpace = new Rect(10,10, 990, 710), detailSpace = new Rect(0,0,1280,690);
    private static Rect startWorkSpace = new Rect(10, 10, 1010, 720);
    private bool showMove;

    static NodeData nData,nDataB;
    static int currentTab;
    string[] tabs = { "primary", "secondary"};
    static List<BaseMoveSetAction> currentList;

    public static void OpenWindow(MoveSet _moveset)
    {
        MovesetEditorWindow window = GetWindowWithRect<MovesetEditorWindow>(startWorkSpace);
        window.titleContent = new GUIContent("Move-Set Editor");
        moveSet = _moveset;

        selectedAction = null;
        
    }
    public static void OpenWindow(MMCharController _movesetChar)
    {
        MovesetEditorWindow window = GetWindowWithRect<MovesetEditorWindow>(startWorkSpace);
        window.titleContent = new GUIContent("Move-Set Editor");
        character = _movesetChar;
        moveSet = _movesetChar.currentMoveset;

        selectedAction = null;
        currentTab = 0;
        currentList = moveSet.secondaryActionTrees;

        
    }

    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = MMHelperFunctions.GetDefaultNodeSkin();
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = MMHelperFunctions.GetSelectedNodeSkin();
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.alignment = TextAnchor.MiddleCenter;

        nData = new NodeData();
        nDataB = new NodeData();

        nData.height = nDataB.height = 50;
        nData.width = nDataB.width = 200;
        nData.onClickRemoveNode = nDataB.onClickRemoveNode = OnClickRemoveNode;
        nData.onAddNode = nDataB.onAddNode = OnClickAddLinkingNode;

    }

    private void OnGUI()
    {
        InitNodes();

        GUILayout.BeginArea(workSpace);
        currentTab = GUILayout.Toolbar(currentTab, tabs);
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        //GUILayout.BeginArea(detailSpace);
        
        EditorGUILayout.BeginHorizontal();
        GUILayoutUtility.GetRect(workSpace.width * 0.6f, workSpace.height * 0.9f);
        if (currentTab == 0)
        {
            currentList = moveSet.actionTrees;
        } else
        {
            currentList = moveSet.secondaryActionTrees;
        }
        if (currentList != null && currentList.Count > 0)
        {
            foreach (BaseMoveSetAction ba in currentList) { DrawTransitionLines(ba); }
            foreach (BaseMoveSetAction ba in currentList) { DrawNodes(ba); }
        }
  

        //only show side section while a tile is selected
        if (selectedAction != null)
        {
            EditorGUILayout.BeginVertical("box");
            DrawActionData();

            //events
            EditorGUILayout.LabelField("--Events--");
            DrawActionEvents();
            if (GUILayout.Button("+"))
            {
                if (selectedAction.events == null) 
                {
                    selectedAction.events = new List<ActionEvent>();
                }
                selectedAction.events.Add(new ActionEvent()); 
            }
            
            

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();

        //button to save  and clear moveset
        if (GUILayout.Button("Clear All"))
        {
            currentList.Clear();
        }
        if (GUILayout.Button("Save"))
        {
            EditorUtility.SetDirty(character);
            EditorUtility.SetDirty(this);
            //EditorUtility.SetDirty(character.currentMoveset);
        }

        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        if (currentList != null && currentList.Count > 0) { foreach (BaseMoveSetAction ba in currentList) { ProcessNodeEvents(Event.current, ba); } }
            
        ProcessEvents(Event.current);

        if (GUI.changed){Repaint();}
    }

    private void DrawNodes(MoveSetAction action)
    {
        action.Draw();
        if (action.transitions != null)
        {
            foreach (ActionTransition _at in action.transitions)
            {
                DrawNodes(_at.nextAction);
            }
        }
    }
    private void DrawActionData()
    {
        selectedAction.actionName = EditorGUILayout.TextField("Action Name", selectedAction.actionName);
        if(selectedAction.GetType()== typeof(BaseMoveSetAction))
        {
            BaseMoveSetAction a = selectedAction as BaseMoveSetAction;
            a.start = (MoveStartType)EditorGUILayout.EnumPopup(a.start);
            if (a.start == MoveStartType.command)
            {
                a.commandIndex = EditorGUILayout.Popup("Command", a.commandIndex, character.GetAllCommands().ToArray());
                a.command = character.GetAllCommands()[a.commandIndex];
            }
        }

        selectedAction.canShiftDirection = EditorGUILayout.Toggle("Can Influence Direction", selectedAction.canShiftDirection);

        showMove = EditorGUILayout.Foldout(showMove, "Movement");
        if (showMove) { 
            selectedAction.movementX = EditorGUILayout.CurveField("X", selectedAction.movementX);
            selectedAction.movementY = EditorGUILayout.CurveField("Y", selectedAction.movementY);
            selectedAction.movementZ = EditorGUILayout.CurveField("Z", selectedAction.movementZ);
        }


        //animations

        //check is there are any animations or if there is a animation component attached

        if (!character.animationController)
        {
            EditorGUILayout.LabelField("No animation components found");
        } else
        {

            if (character.GetAllAnimsB().Count < 1)
            {
                EditorGUILayout.LabelField("No Animations to use");
            }
            else
            {
                //make sure the index is in range
                selectedAction.animIndex = selectedAction.animIndex >= character.GetAllAnimsB().Count ? 0 : selectedAction.animIndex;
                //select index with a popup
                selectedAction.animIndex = EditorGUILayout.Popup(selectedAction.animIndex, character.GetAllAnimsB().ToArray());
                selectedAction.actionAnimation = character.GetAllAnimsB()[selectedAction.animIndex];
            }
        }
        

        //transitioms
        if(selectedAction.transitions != null && selectedAction.transitions.Count > 0)
        {
            
            foreach(ActionTransition at in selectedAction.transitions) 
            {
                //horizontal layout to make sure the delete button is side by side
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(at.nextAction.actionName);
                at.transitionType = (MoveStartType)EditorGUILayout.EnumPopup(at.transitionType);
                EditorGUILayout.EndHorizontal();
                if (at.transitionType == MoveStartType.command)
                {
                    at.commandIndex = EditorGUILayout.Popup("Command", at.commandIndex, character.GetAllCommands().ToArray());
                    at.command = character.GetAllCommands()[at.commandIndex];
                }
               
            }
            

        }        
    }

    private void DrawActionEvents()
    {
        if(selectedAction.events != null && selectedAction.events.Count > 0)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach(ActionEvent _ae in selectedAction.events)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"start frame: {(int)_ae.startFrame}, end frame: {(int)_ae.endFrame, 2}");
                if (GUILayout.Button("-"))
                {
                    selectedAction.events.Remove(_ae);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.MinMaxSlider(ref _ae.startFrame, ref _ae.endFrame, 0, GetAnimLen(selectedAction.actionAnimation));
                _ae.eventType = (EventTypes)EditorGUILayout.EnumPopup("Event Type:", _ae.eventType);


                //show different fields depending on the event
                switch (_ae.eventType)
                {
                    case EventTypes.Hit:
                        _ae.vector3Var = EditorGUILayout.Vector3Field("HitBoxPos:", _ae.vector3Var);
                        _ae.floatVar = EditorGUILayout.FloatField("HitBoxSize:", _ae.floatVar);
                        _ae.floatVar2 = EditorGUILayout.FloatField("Knockback amount:", _ae.floatVar2);
                        _ae.floatVar3 = EditorGUILayout.FloatField("Hit-stun amount:", _ae.floatVar3);
                        if (!_ae.boolVar2)
                        {
                            _ae.floatVar4 = EditorGUILayout.FloatField("Damage:", _ae.floatVar4);
                        }
                        _ae.boolVar1 = EditorGUILayout.Toggle("Bypass Armor:", _ae.boolVar1);
                        _ae.boolVar2 = EditorGUILayout.Toggle("Is WindBox:", _ae.boolVar2);
                        _ae.boolVar3 = EditorGUILayout.Toggle("Radial KnockBack:", _ae.boolVar3);
                        _ae.objectVar1 = (GameObject)EditorGUILayout.ObjectField("HitSpark:",_ae.objectVar1, typeof(GameObject), false);
                        break;
                    case EventTypes.SetCanMove:
                        _ae.boolVar1 = EditorGUILayout.Toggle("Toggle:", _ae.boolVar1);
                        break;
                    case EventTypes.SetCanChain:
                        _ae.boolVar1 = EditorGUILayout.Toggle("Toggle:", _ae.boolVar1);
                        break;
                    case EventTypes.SetArmored:
                        _ae.boolVar1 = EditorGUILayout.Toggle("Toggle:", _ae.boolVar1);
                        break;
                    case EventTypes.SpawnObject:
                        _ae.vector3Var = EditorGUILayout.Vector3Field("Effect Position:", _ae.vector3Var);
                        _ae.objectVar1 = (GameObject)EditorGUILayout.ObjectField("Effect Prefab", _ae.objectVar1, typeof(GameObject), false);
                        _ae.boolVar1 = EditorGUILayout.Toggle("Parented:", _ae.boolVar1);
                        break;
                    case EventTypes.PlaySound:
                        _ae.clip = (AudioClip)EditorGUILayout.ObjectField(_ae.clip, typeof(AudioClip), false);
                        break;
                }

                GUILayout.Space(5.0f);
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawTransitionLines(MoveSetAction action)
    {
        if(action.transitions != null)
        {
            Vector3 pos1 = action.rect.center;
            foreach (ActionTransition at in action.transitions)
            {
                Vector3 pos2 = at.nextAction.rect.center;
                Handles.DrawLine(pos1, pos2);
                
                var centerPoint = (pos2+pos1)/2;
                var offset = pos2 - pos1;
                var arrowOffset = new Vector3(10, 10);
                var centerRect = new Rect(centerPoint - arrowOffset / 2, arrowOffset);
                var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

                Matrix4x4 matrixBackup = GUI.matrix;

                
                GUIUtility.RotateAroundPivot(angle, centerPoint);
                GUI.DrawTexture(centerRect, MMHelperFunctions.GetArrowTex());

                GUI.matrix = matrixBackup;
                DrawTransitionLines(at.nextAction);
            }
            GUI.changed = true;
        }
    }

    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (currentList != null && currentList.Count > 0)
                        foreach (BaseMoveSetAction ba in currentList) { 
                            selectedAction = CheckforNodes(e.mousePosition, ba);
                            if(selectedAction != null) { GUIUtility.keyboardControl = 0; break; }
                        }
                    
                }
                else if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;
            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    if(currentList != null && currentList.Count > 0)
                        foreach (BaseMoveSetAction ba in currentList){OnDrag(e.delta, ba);}
                }
                break;
            case EventType.Repaint:

                break;
        }
    }

    MoveSetAction CheckforNodes(Vector2 _mousePos, MoveSetAction action)
    {   
        if (action.rect.Contains(_mousePos)) 
        {
            GUI.changed = true;
            return action; 
        } else
        {
            if (action.transitions != null && action.transitions.Count > 0)
            {
                foreach (ActionTransition at in action.transitions)
                {
                    MoveSetAction toReturn = CheckforNodes(_mousePos, at.nextAction);

                    if(toReturn != null)
                    {
                        return toReturn;
                    }
                }
            }
        }
        
        return null;
    }

    private void ProcessNodeEvents(Event e, MoveSetAction action)
    {
        bool guiChanged = action.ProcessEvents(e);
        if (action.transitions != null && action.transitions.Count > 0)
        {
            foreach (ActionTransition at in action.transitions)
            {
                ProcessNodeEvents(e, at.nextAction);
            }
        }

        if (guiChanged)
        {
            GUI.changed = true;
        }
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add node"), false, () => OnClickAddNode(mousePosition));
        genericMenu.ShowAsContext();
    }

    private void OnClickAddNode(Vector2 mousePosition)
    {
        
        if (currentList == null)
        {
            currentList = new List<BaseMoveSetAction>();
        }

        BaseMoveSetAction act = new BaseMoveSetAction(mousePosition, new Vector2(200,50), OnClickRemoveNode, OnClickAddLinkingNode);
        currentList.Add(act);

        GUI.changed = true;
    }


    private void OnClickRemoveNode(MoveSetAction node)
    {

        currentList.Remove(node as BaseMoveSetAction);    
    }


    private void OnClickAddLinkingNode(MoveSetAction action)
    {
        if (action.transitions == null)
        {
            action.transitions = new List<ActionTransition>();
        }

        ActionTransition at = new ActionTransition();
        at.nextAction = new MoveSetAction(action.rect.position + new Vector2(500, 0), new Vector2(200, 50), OnClickRemoveNode, OnClickAddLinkingNode);
        action.transitions.Add(at);
    }

    private int GetAnimLen(string animName)
    {
        return (int)(character.animationController[animName].length * character.animationController[animName].clip.frameRate);
    }

    private void OnDrag(Vector2 delta, MoveSetAction action)
    {
        drag = delta;
        action.Drag(delta);
        if (action.transitions != null && action.transitions.Count > 0)
        {
            foreach(ActionTransition at in action.transitions) {
                OnDrag(delta, at.nextAction); 
            }
        }

        GUI.changed = true;
    }

    private void RefreshNodes(MoveSetAction action)
    {
        action.OnAddNode = OnClickAddLinkingNode;
        action.OnRemoveNode = OnClickRemoveNode;
        if (action.style == null)
        {
            action.defaultNodeStyle = new GUIStyle();
            action.defaultNodeStyle.normal.background = MMHelperFunctions.GetDefaultNodeSkin();
            action.defaultNodeStyle.border = new RectOffset(12, 12, 12, 12);
            action.style = action.defaultNodeStyle;
        }

        if (action.labelStyle == null)
        {
            action.labelStyle = new GUIStyle(GUI.skin.label);
            action.labelStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (selectedNodeStyle == null)
        {
            action.selectedNodeStyle = new GUIStyle();
            action.selectedNodeStyle.normal.background = MMHelperFunctions.GetSelectedNodeSkin();
            action.selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
        }
        if (action.transitions != null && action.transitions.Count > 0)
        {
            foreach(ActionTransition at in action.transitions)
            {
                at.nextAction.parent = action;
                at.nextAction.OnAddNode = OnClickAddLinkingNode;
                at.nextAction.OnRemoveNode = OnClickRemoveNode;
                RefreshNodes(at.nextAction);
            }
        }
    }

    private void InitNodes()
    {
        //make sure the actions have a reference to the action funtions
        if (moveSet.actionTrees.Count > 0)
        {
            foreach (MoveSetAction ma in moveSet.actionTrees)
            {
                RefreshNodes(ma);
            }
        }

        if (moveSet.secondaryActionTrees.Count > 0)
        {
            foreach (MoveSetAction ma in moveSet.secondaryActionTrees)
            {
                RefreshNodes(ma);
            }
        }
    }
}