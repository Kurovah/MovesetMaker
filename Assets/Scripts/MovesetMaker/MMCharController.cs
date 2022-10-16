using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MasterStates
{
    Normal,
    Airborne,
    Hurt
}

public class MMCharController : MonoBehaviour
{
    public event Action OnCharacterHurt = () => { };
    public Vector3 velocity, knockbackVel;
    Vector3 moveVel;
    [HideInInspector, SerializeField]
    public MoveSet currentMoveset;

    [HideInInspector]
    public MoveSetAction currentAction;

    CharacterController character;
    float speed;
    public Animation animationController;
    public bool canMove = true;
    public bool invincible = false;
    public bool grounded = false;
    public bool stunned = false;
    public bool canChain = true;
    public bool armoured = false;
    public bool characterActive = true;
    bool inTree;
    public float hitlag, jumPow = 12, characterHealth = 100;
    public Transform spawnerParent;
    public MasterStates currentMasterState;
    InputHandler ih;
    float stunTime;
    public string currentAnimation;
    int prevTime;
    [HideInInspector]
    public string idleAnimation, moveAnimation, airborneAnimation, hurtAnimation;
    [HideInInspector]
    public bool showConstantAnimations,showBannedAnimations;
    [HideInInspector]
    public int idleIndex, moveIndex, airborneIndex, hurtIndex;
    [HideInInspector]
    public List<int> bannedIndexs;
    Coroutine removeKnockbackCoroutine;

    List<MMCharController> hasHit = new List<MMCharController>();

    [HideInInspector]
    public bool shownonAnims;
    [HideInInspector]
    public List<string> nonusableanims;
    [HideInInspector]
    public AudioSource audioSource;
    [HideInInspector]
    List<BaseMoveSetAction> actionStarts;

    private void Awake()
    {
        inTree = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        animationController = GetComponentInChildren<Animation>();
        currentMasterState = MasterStates.Normal; 
        speed = 15;
        character = GetComponent<CharacterController>();
        ih = GetComponent<InputHandler>();
        audioSource = GetComponent<AudioSource>();
        currentAction = null;
    }


    // Update is called once per frame
    void Update()
    {
        switch (currentMasterState)
        {
            case MasterStates.Normal:
                CheckInput();
                if (currentAction != null)
                    UpdateAction();
                    

                if (!inTree)
                {
                    //becoming airborne
                    if (!grounded)
                    {
                        currentMasterState = MasterStates.Airborne;
                    }

                    if (canMove) {
                        //use camera as reference when controlled by player
                        if (ih.playerControlled)
                        {
                            Vector3 frontVector = Vector3.Cross(Camera.main.transform.right, Vector3.up);
                            moveVel = Camera.main.transform.right*ih.moveAxis.x + frontVector*ih.moveAxis.y;
                        } else
                        {
                            moveVel = new Vector3(ih.moveAxis.x, 0, ih.moveAxis.y);
                        }

                        moveVel = moveVel.normalized;
                        velocity = Vector3.Lerp(velocity, moveVel * speed, 0.2f);
                    }

                    if (Vector3.Magnitude(moveVel) > 0)
                    {
                        transform.rotation = Quaternion.LookRotation(-moveVel);
                        animationController.Play(moveAnimation);
                    }
                    else
                    {
                        animationController.Play(idleAnimation);
                    }
                }
                else
                {
                    if (currentAction != null && hitlag <= 0) {
                        float time = animationController[currentAction.actionAnimation].normalizedTime;
                        if (currentAction.movementX != null)
                        {
                            moveVel.x = currentAction.movementX.Evaluate(time);
                        }
                        if (currentAction.movementY != null)
                        {
                            moveVel.y = currentAction.movementZ.Evaluate(time);
                        }
                        if (currentAction.movementZ != null)
                        {
                            moveVel.z = currentAction.movementZ.Evaluate(time);
                        }

                        Vector3 newMoveVel = -transform.forward * moveVel.z + transform.right * moveVel.x;

                        velocity = Vector3.Lerp(velocity, newMoveVel * speed, 0.5f);
                    }
                }
                break;
            case MasterStates.Airborne:
                CheckInput();
                if (currentAction != null)
                    UpdateAction();

                if (ih.playerControlled)
                {
                    Vector3 frontVector = Vector3.Cross(Camera.main.transform.right, Vector3.up);
                    moveVel = Camera.main.transform.right * ih.moveAxis.x + frontVector * ih.moveAxis.y;
                }
                else
                {
                    moveVel = new Vector3(ih.moveAxis.x, 0, ih.moveAxis.y);
                }

                moveVel = moveVel.normalized;
                velocity = Vector3.Lerp(velocity, moveVel * speed, 0.2f);

                if (!inTree)
                {
                    animationController.Play(airborneAnimation);
                    if (grounded)
                    {
                        inTree = false;
                        currentAction = null;
                        currentMasterState = MasterStates.Normal;
                    }
                }
                break;
            case MasterStates.Hurt:
                if (animationController[hurtAnimation].normalizedTime > 0.9f)
                {
                    animationController.Play(idleAnimation);
                    currentMasterState = MasterStates.Normal;
                }
                break;
                
                
        }

        character.Move((velocity + knockbackVel) * Time.deltaTime);
    }

    public List<string> GetAllAnims()
    {
        List<string> anims = new List<string>();
        foreach(AnimationState ac in animationController)
        {
            anims.Add(ac.name);
        }
        return anims;
    }

    public List<string> GetAllAnimsB()
    {
        List<string> anims = new List<string>();
        List<string> banned = GetBannedAnims();
        foreach (AnimationState ac in animationController)
        {
            if (banned.Contains(ac.name)) { continue; }
            anims.Add(ac.name);
        }
        return anims;
    }

    public List<string> GetAllCommands()
    {
        List<string> coms= new List<string>();
        foreach (KeyValuePair<string, List<InputTypes>> command in GetComponent<InputHandler>().commands)
        {
            coms.Add(command.Key);
        }

        return coms;
    }

    public List<string> GetBannedAnims()
    {
        List<string> anims = new List<string>();
        foreach(int i in bannedIndexs)
        {
            anims.Add(GetAllAnims()[i]);
        }
        return anims;
    }

    void CheckInput()
    {
        bool commandInput = false;
        //check through all input commands
        foreach (KeyValuePair<string, List<InputTypes>> comms in ih.commands)
        {
            if (ih.checkforCommand(comms.Value))
            {
                ////Debug.Log(comms.Key);
                TryChangeAction(MoveStartType.command, comms.Key);
                commandInput = true;
            }
        }

        if (!commandInput)
        {
            //get first action and do it
            if (ih.lightPressed)
            {
                TryChangeAction(MoveStartType.Action1);
            }

            if (ih.heavyPressed)
            {
                TryChangeAction(MoveStartType.Action2);
            }
        }
    }

    public void TryChangeAction(MoveStartType _type, string command = default)
    {
        switch (currentMasterState)
        {
            case MasterStates.Normal:
                actionStarts = currentMoveset.actionTrees;
                break;
            case MasterStates.Airborne:
                actionStarts = currentMoveset.secondaryActionTrees;
                break;
        }

        if (!inTree && actionStarts.Count> 0 && actionStarts != null)
        {

            foreach (BaseMoveSetAction ba in actionStarts) { 
                if(ba.start == _type) 
                {
                    //skip if it's  command type
                    if(ba.start == MoveStartType.command && ba.command != command) { continue; }
                    inTree = true;
                    currentAction = ba;
                    animationController.Play(currentAction.actionAnimation);
                    currentAnimation = currentAction.actionAnimation;
                    prevTime = 0;
                    hasHit.Clear();
                    ////Debug.Log("Action changed"); 
                }
            }
        } else
        {
            if(currentAction.transitions != null && currentAction.transitions.Count > 0 && canChain)
            {
                foreach (ActionTransition at in currentAction.transitions)
                {
                    if(at.transitionType == _type)
                    {
                        if (at.transitionType == MoveStartType.command && at.command != command) { continue; }
                        hitlag = 0;
                        EndHitLag();
                        currentAction = at.nextAction;
                        animationController.Stop();
                        animationController.Play(currentAction.actionAnimation);
                        currentAnimation = currentAction.actionAnimation;
                        hasHit.Clear();
                        //Debug.Log("Action changed");
                        prevTime = 0;
                        break;
                    }
                }
            }
        }
    }

    void UpdateAction()
    {
        //update the frames of the action
        if (inTree)
        {
            if (currentAction.canShiftDirection) 
            { 
            
            }
            //check for events needs second check just in case it becomes null
            if (currentAction.events != null && currentAction.events.Count > 0)
            {
                int time = (int)(animationController[currentAction.actionAnimation].time * animationController[currentAction.actionAnimation].clip.frameRate);
                foreach (ActionEvent _ae in currentAction.events)
                {
                    if ( time >= Mathf.CeilToInt(_ae.startFrame) && time < Mathf.CeilToInt(_ae.endFrame) && time != prevTime)
                    {
                        DoEvent(_ae.eventType, _ae.vector3Var, 
                            _floatSlot1: _ae.floatVar, _floatSlot2: _ae.floatVar2, _floatSlot3: _ae.floatVar3, 
                            _boolSlot1: _ae.boolVar1, _boolSlot2:_ae.boolVar2, _boolSlot3: _ae.boolVar3, _gameObject: _ae.objectVar1, clip: _ae.clip);
                    }
                }
                prevTime = time;
            }

            //when animation is finished
            if (animationController[currentAction.actionAnimation].normalizedTime >= 0.9f)
            {
                inTree = false;
                currentAction = null;
                canMove = true;
                canChain = true;
                return;
            }
        }
        
    }

    #region For Events
    public void DoEvent(EventTypes eventType,
        Vector3 _vector3Slot1 = default, Vector3 _vector3Slot2 = default,
        float _floatSlot1 = 0,float _floatSlot2 = 0, float _floatSlot3 = 0, float _floatSlot4 = 0, bool _boolSlot1 = false, bool _boolSlot2 = false, bool _boolSlot3 = false, GameObject _gameObject = null,
        AudioClip clip = null)
    {
        switch (eventType)
        {
            case EventTypes.Hit:
                CreateHitBox(transform.position+_vector3Slot1,_floatSlot1, _floatSlot2, _floatSlot3, _floatSlot4, _boolSlot1, _boolSlot2, _boolSlot3, _gameObject);
                break;
            case EventTypes.SetCanMove:
                canMove = _boolSlot1;
                break;
            case EventTypes.SetCanChain:
                canChain = _boolSlot1;
                break;
            case EventTypes.SetArmored:
                armoured = _boolSlot1;
                break;
            case EventTypes.SpawnObject:
                CreateObject(_vector3Slot1,_gameObject, _boolSlot1);
                break;
            case EventTypes.PlaySound:
                PlaySound(clip);
                break;
        }
    }

    public void CreateHitBox(Vector3 position, float hitBoxScale = 1, float knockBackScale = 15, float hitlag = 0.1f,float damage = 0 ,bool armorBypass = false, bool isWindBox = false, bool radialKnockBack = false,  GameObject hitspark = null)
    {
        Vector3 knockBack = new Vector3();
        //Gizmos.DrawSphere(position, scale);
        //Debug.Log("hitbox");
        Collider[] hits = Physics.OverlapSphere(position, hitBoxScale);
        if (hits.Length > 0)
        {
            foreach (Collider c in hits)
            {
                
                MMCharController other = c.gameObject.GetComponent<MMCharController>();
                if (c.gameObject == this.gameObject || other == null) { continue; }
                //lnock back will either be based on position from htbox or where the user is facing
                if (radialKnockBack)
                {
                    knockBack = other.gameObject.transform.position - position;
                    knockBack.y = 0;
                    knockBack = knockBack.normalized * knockBackScale;
                } else
                {
                    knockBack = -transform.forward * knockBackScale;
                }

                if (other != null && other != this && !hasHit.Contains(other))
                {
                    //if it 's a wind box, just apply knockback
                    hasHit.Add(other);
                    if (isWindBox)
                    {
                        other.ApplyKnockBack(knockBack);
                    } else
                    {
                        //pause the actors for a bit
                        ApplyHitLag(hitlag);
                        other.GetHurt(knockBack, armorBypass, hitlag, hitspark);
                        other.characterHealth -= damage;
                    }
                }
            }
        }
    }

    public void CreateObject(Vector3 position, GameObject _obj, bool parented)
    {
        GameObject spawnedObj;
        spawnedObj = Instantiate(_obj, spawnerParent);
        spawnedObj.transform.localPosition = position;
        if (!parented)
        {
            spawnedObj.transform.parent = null;
        }
        
    }

    public void PlaySound(AudioClip audioClip)
    {
        //Debug.Log("sound Playing");
        audioSource.PlayOneShot(audioClip);
    }

    public void GetHurt(Vector3 knockBack, bool armourBypass, float hitLagTime = 0.1f, GameObject _hitspark = null)
    {
        if (armoured && armourBypass || !armoured) {
            inTree = false;
            animationController[hurtAnimation].time = 0;
            animationController.Play(hurtAnimation, PlayMode.StopSameLayer);

            if(_hitspark != null)
                Instantiate(_hitspark, spawnerParent.transform.position + Vector3.up * 1.5f, spawnerParent.transform.rotation);

            ResetMoveset();
            currentMasterState = MasterStates.Hurt;
            ApplyHitLagWithKnockBack(hitLagTime, knockBack);

            //look at damage source
            transform.rotation = Quaternion.LookRotation(-knockBack);
        } else
        {
            Instantiate(_hitspark, spawnerParent.transform.position + Vector3.up * 1.5f, spawnerParent.transform.rotation);
        }
    }

    private void ResetMoveset()
    {
        currentAction = null;
    }
    #endregion

    //Hitlag
    public void ApplyHitLag(float amount = .5f)
    {
        foreach (AnimationState state in animationController)
        {
            state.speed = 0;
        }
        StartCoroutine(RemoveHitLag(amount));
    }

    IEnumerator RemoveHitLag(float amount = 0.5f)
    {
        Vector3 storedVel = velocity;
        velocity = Vector3.zero;
        hitlag = amount;
        //Debug.Log(name + "losing hitlag");
        while (hitlag > 0)
        {
            hitlag -= 1;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        velocity = storedVel;
        EndHitLag();
    }


    //hitlag + knockback
    public void ApplyHitLagWithKnockBack(float amount = .5f, Vector3 knockBackDir = default)
    {
        foreach (AnimationState state in animationController)
        {
            state.speed = 0;
        }
        StartCoroutine(RemoveHitLagwWithKnockBack(knockBackDir, amount));
    }
    

    IEnumerator RemoveHitLagwWithKnockBack(Vector3 knockBack, float time = 0.2f)
    {
        Vector3 storedVel = velocity;
        velocity = Vector3.zero;
        hitlag = time;
        //Debug.Log(name + "losing hitlag");
        while (hitlag > 0)
        {
            hitlag -= 1;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        velocity = storedVel;
        EndHitLag();
        ApplyKnockBack(knockBack);
    }

    void EndHitLag()
    {
        foreach (AnimationState state in animationController)
        {
            state.speed = 1;
        }
    }

    //knockback
    public void ApplyKnockBack(Vector3 Dir)
    {
        knockbackVel = Dir;

        if(removeKnockbackCoroutine != null)
            StopCoroutine(removeKnockbackCoroutine);

        removeKnockbackCoroutine = StartCoroutine(RemoveKnockBack());
    }


    IEnumerator RemoveKnockBack()
    {
        while (knockbackVel.magnitude > 0.1f)
        {
            knockbackVel = Vector3.Lerp(knockbackVel, Vector3.zero, 0.1f);
            yield return null;
        }

        knockbackVel = Vector3.zero;
    }

    

    public void GetDefeated()
    {
        characterActive = false;
    }
}
