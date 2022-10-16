using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;



 public enum EventTypes
{
    Hit,
    SetCanMove,
    SetCanChain,
    SetArmored,
    SpawnObject,
    PlaySound
}

[Serializable]
public class MoveSet
{
    public List<BaseMoveSetAction> actionTrees = new List<BaseMoveSetAction>();
    public List<BaseMoveSetAction> secondaryActionTrees = new List<BaseMoveSetAction>();
}

[System.Serializable]
public class ActionEvent
{
    public float startFrame, endFrame;
    public EventTypes eventType;

    #region variables to use
    //hit event
    public Vector3 vector3Var;
    public float floatVar, floatVar2, floatVar3, floatVar4;
    public GameObject objectVar1, objectVar2;
    public AudioClip clip;
    public bool boolVar1, boolVar2, boolVar3;
    #endregion

    public ActionEvent(float _startFrame = 0, float _duration = 0)
    {
        startFrame = _startFrame;
        endFrame = _duration;
        eventType = EventTypes.Hit;
        objectVar1 = null;
    }
}

public enum TransitionTypes
{
    Attack1,
    Attack2,
    Jump,
    Finish
}

[Serializable]
public class ActionTransition
{
    public MoveSetAction nextAction;
    public int commandIndex;
    public string command;
    public MoveStartType transitionType;
}

public class ActionCommand
{
    public List<InputTypes> command;
}

[Serializable]
public class MoveSetAction
{
    #region variables
    public string actionName;
    public int animIndex;
    public bool canShiftDirection;

    public string actionAnimation;
    public AnimationCurve movementX = new AnimationCurve(), movementY = new AnimationCurve(), movementZ = new AnimationCurve();
    public MoveSetAction parent;

    [SerializeField]
    public List<ActionTransition> transitions;
    [SerializeField]
    public List<ActionEvent> events;
    #endregion

    #region node variables
    public Rect rect;
    public string title, s;
    public GUIStyle style;
    public GUIStyle defaultNodeStyle;
    public GUIStyle selectedNodeStyle;
    public GUIStyle labelStyle;
    public bool isDragged;
    public bool isSelected;
    public Action<MoveSetAction> OnRemoveNode, OnAddNode;
    #endregion

    public MoveSetAction(Vector2 position, Vector2 nodeSize, Action<MoveSetAction> removeNodeAction, Action<MoveSetAction> addNodeAction)
    {

        actionName = "newAction";
        //node drawing
        rect = new Rect(position.x, position.y, nodeSize.x, nodeSize.y);
        style = defaultNodeStyle;
        OnRemoveNode = removeNodeAction;
        OnAddNode = addNodeAction;
    }

    public virtual void Draw()
    {
        GUI.Box(rect, "", style);
        GUI.Label(rect, actionName, labelStyle);
    }

    #region GUI functionality
    public void Drag(Vector2 delta)
    {
        rect.position += delta;
    }

    public bool ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (rect.Contains(e.mousePosition))
                    {
                        isDragged = true;
                        GUI.changed = true;
                        isSelected = true;
                        style = selectedNodeStyle;
                    }
                    else
                    {
                        GUI.changed = true;
                        isSelected = false;
                        style = defaultNodeStyle;
                    }
                }

                if (e.button == 1 && isSelected && rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }

                break;

            case EventType.MouseUp:
                isDragged = false;
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        return false;
    }

    protected virtual void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.AddItem(new GUIContent("Add Linked node"), false, OnClickAddLinkedNode);
        genericMenu.ShowAsContext();
    }

    protected virtual void OnClickRemoveNode()
    {
        foreach(ActionTransition at in parent.transitions)
        {
            if(at.nextAction == this)
            {
                parent.transitions.Remove(at);
                break;
            }
        }
    }

    protected void OnClickAddLinkedNode()
    {
        if (OnAddNode != null)
        {
            OnAddNode(this);
        }
    }
    #endregion
}

[Serializable]
public class BaseMoveSetAction : MoveSetAction
{
    public string command;
    public int commandIndex;
    public MoveStartType start;
    public BaseMoveSetAction( Vector2 position, Vector2 nodeSize, Action<MoveSetAction> removeNodeAction, Action<MoveSetAction> addNodeAction) : base(position, nodeSize,removeNodeAction, addNodeAction)
    {

    }

    public override void Draw()
    {
        GUI.Label(rect, "B:"+actionName, labelStyle);
    }

    protected override void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.AddItem(new GUIContent("Add Linked node"), false, OnClickAddLinkedNode);
        genericMenu.ShowAsContext();
    }

    protected override void OnClickRemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    }
}

public enum MoveStartType
{
    Action1,
    Action2,
    command
}

public class NodeData
{
    public float width, height;
    public Action<MoveSetAction> onClickRemoveNode, onAddNode;
}

public class MMHelperFunctions
{
    public static float RoundToDecimal(float _input, int decimalPlaces)
    {
        return Mathf.Round(_input * Mathf.Pow(10, decimalPlaces)) / Mathf.Pow(10, decimalPlaces);
    }


    public static Texture2D GetDefaultNodeSkin()
    {
        return Resources.Load<Texture2D>("Textures/roundedsquare");
    }

    public static Texture2D GetSelectedNodeSkin()
    {
        return Resources.Load<Texture2D>("Textures/roundedsquare");
    }

    public static Texture2D GetArrowTex()
    {
        return Resources.Load<Texture2D>("Textures/ArrowHead");
    }
}

public class Hitbox
{
    public float scale, damageValue;
    public Vector3 position;
    public bool blockable;

    public Hitbox(float _scale, Vector3 _position, bool _blockable, float _damageValue)
    {
        scale = _scale;
        position = _position;
        blockable = _blockable;
        damageValue = _damageValue;
    }
}