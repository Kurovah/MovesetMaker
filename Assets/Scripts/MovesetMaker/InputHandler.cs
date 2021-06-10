using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AIStates
{
    idle,
    moveTowards,
    orbit,
    attack1
}

public enum InputTypes
{
    left,
    right,
    up,
    down,
    light,
    heavy,
    none
}

public class InputHandler : MonoBehaviour
{
    public bool playerControlled,lightPressed,heavyPressed;
    public Vector2 moveAxis,lastAxis;
    public List<InputTypes> buffer;
    public KeyCode LightButton;
    public KeyCode HeavyButton;
    public int buffersize;
    public Dictionary<string, List<InputTypes>> commands = new Dictionary<string, List<InputTypes>>
    {
        { "Back Forward", new List<InputTypes>{ InputTypes.down, InputTypes.up, InputTypes.light} },
        { "Double Up", new List<InputTypes>{ InputTypes.up, InputTypes.up, InputTypes.light } },
        { "Double Down", new List<InputTypes>{ InputTypes.down, InputTypes.down, InputTypes.light } },
        { "Forward Punch L", new List<InputTypes>{ InputTypes.up, InputTypes.light}},
        { "Back Punch L", new List<InputTypes>{ InputTypes.down, InputTypes.light}},
        { "Forward Punch H", new List<InputTypes>{ InputTypes.up, InputTypes.heavy}},
        { "Back Punch H", new List<InputTypes>{ InputTypes.down, InputTypes.heavy}},
    };
    // Start is called before the first frame update
    void Start()
    {
        //initialise buffer
        buffer = new List<InputTypes>();
        for(int i = 0; i < buffersize; i++)
        {
            buffer.Add(InputTypes.none);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerControlled)
        {
            moveAxis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            lightPressed = Input.GetKeyDown(LightButton);
            heavyPressed = Input.GetKeyDown(HeavyButton);
            UpdateBuffer();
            lastAxis = moveAxis;
        }
        else
        {
            moveAxis = Vector2.zero;
        }   
    }

    void UpdateBuffer()
    {
        //for movement
        if(moveAxis != lastAxis)
        {
            float dotP = Vector2.Dot(Vector2.up, moveAxis);
            float dotP2 = Vector2.Dot(Vector2.right, moveAxis);
            switch (dotP)
            {
                //left and right
                case 0:
                    switch (dotP2)
                    {
                        //left and right
                        case 1:
                            AddInput(InputTypes.right);
                            break;
                        case -1:
                            AddInput(InputTypes.left);
                            break;
                    }
                    break;
                case 1:
                    AddInput(InputTypes.up);
                    break;

                case -1:
                    AddInput(InputTypes.down);
                    break;
            }
        } else if (Input.GetKeyDown(LightButton))
        {
            AddInput(InputTypes.light);
        } else if (Input.GetKeyDown(HeavyButton))
        {
            AddInput(InputTypes.heavy);
        }
        else
        {
            AddInput(InputTypes.none);
        }
    }

    void AddInput(InputTypes input)
    {
        buffer.Add(input);
        if (buffer.Count >= buffersize) { buffer.RemoveAt(0); }
    }

    public bool checkforCommand(List<InputTypes> inputList)
    {
        if(buffer[0] != inputList[0]) { return false; }
        int matchingIndex = 1;
        for(int i = buffersize - 1; i > 0; i--)
        {
            if(buffer[i] == inputList[matchingIndex]) {
                matchingIndex++;
                if (matchingIndex == inputList.Count) { return true; }
            }
        }

        return false;
    }
}
