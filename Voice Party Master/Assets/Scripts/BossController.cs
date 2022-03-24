using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossController : MonoBehaviour
{
         // COMPONENTS
    ///////////////////////////
    private Rigidbody rb; 
    private NavMeshAgent nmAgent;   
    private Animator animator;

    public Animator GetAnimator() { return animator; }

#region Enums

    public enum ACTION { IDLE, MOVE, ENGAGE, ATTACK }
    
    public enum DIRECTION { FORWARD, BACKWARD, LEFT, RIGHT, TARGET }
    
    public enum MOVEMENT { IDLE, WALK }

#endregion

// STATES
    [SerializeField] private ACTION currentAction = ACTION.IDLE; // --------  The current state of the character controller.

// COMBAT SYSTEMS
    public CharacterStats stats = new CharacterStats() 
    {
        // Base Stats
        Movement_Speed = 1.0f,
        Attack_Range = 3.5f,
        
        // Defensive Stats
        Health = 100.0f, 
        Health_Regen = 1.0f,
        Armor = 1,
        Resistance = 1,

        // Physical Stats
        Attack_Power = 5,
        Attack_Speed = 1.0f,

        // Magical Stats
        Spell_Power = 1,
        Mana = 100.0f, 
        Mana_Regen = 1.0f,

        // Offensive Stats
        Critical_Rate = 0.1f,
        Critical_Damage = 2.0f,
    };

    public Entity entity; // ----------------------------------------------  Manages Health
    public GameObject target; // ------------------  The Current Target of the Controller
    private float basicAttackTimer; // ------------------------------------  The timer that determines when the character can attack. 

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        nmAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInParent<Animator>();

        // Initialize Entity
        entity = new Entity(1);
        entity.SetAnimator(animator);
    }

    private void Update()
    {
        // TEMP CONTROLS TO TEST ANIMATIONS
        transform.position = new Vector3(animator.transform.position.x, animator.transform.position.y, animator.transform.position.z - 0.5f);

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animator.SetInteger("MoveType", 0);
            animator.SetInteger("ActionType", 0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            animator.SetInteger("MoveType", 1);
            animator.SetInteger("ActionType", 1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) 
        {
            animator.SetTrigger("Attack");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) 
        {
            animator.SetTrigger("Roar");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            animator.SetTrigger("Powerup");
        }

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            animator.SetTrigger("TossAttack");
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            animator.SetTrigger("Dance");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8)) 
        {
            animator.SetTrigger("Death");
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            animator.SetTrigger("Resurrect");
        }

        if (entity.IsDead) return;

        // HANDLE MOTION
        ///////////////////////////////////////
        if (currentAction == ACTION.MOVE) 
        {                               
            // Set movement to idle when destination has been reached
            if (nmAgent.remainingDistance <= nmAgent.stoppingDistance && nmAgent.velocity.sqrMagnitude == 0f) 
            {   
                SetAction(ACTION.IDLE);         
            }         
        
        } else if (currentAction == ACTION.ENGAGE) 
        {       
            // While out of range, move towards the target     
            if (Vector3.Distance(transform.position, target.transform.position) > stats.Attack_Range) 
            {
                nmAgent.SetDestination(target.transform.position);
            } 
            
            // When in range, switch to attack action.
            else if (Vector3.Distance(transform.position, target.transform.position) <= stats.Attack_Range) 
            {
                SetAction(ACTION.ATTACK);              
            }
        }
            // HANDLE COMBAT
        ///////////////////////////////////////

        // Attack Speed Timer
        if (basicAttackTimer > 0) { 
            basicAttackTimer -= Time.deltaTime;
        }

        // Look At Target
        if (currentAction == ACTION.ENGAGE || currentAction == ACTION.ATTACK) {
            transform.LookAt(transform.position + GetDirection(DIRECTION.TARGET));
        }

        // Declare an attack
        if (currentAction == ACTION.ATTACK) {            
            if (basicAttackTimer <= 0) {
                // Damage the target
                //float amount = (character.GetDamageType() == PrimaryDamageType.Physical) ? character.GetStats().Attack_Power * 1.0f : character.GetStats().Spell_Power * 0.35f;                
                //target.GetComponent<PlayerController>().entity.DealDamage(amount);
                basicAttackTimer = stats.Attack_Speed;
                animator.SetTrigger("Attack");

                
            }
        }
    }

    ////////////////////////////////////////////////////
    // STATE MANAGEMENT
    ////////////////////////////////////////////////////

    // Set Action
    public void SetAction(ACTION action) {

        if (entity.IsDead) return;

        currentAction = action;
        animator.SetInteger("ActionType", (int)currentAction);

        switch (currentAction) {
            default:
            case ACTION.IDLE: // Do Nothing.  
                SetMovementSpeed(MOVEMENT.IDLE);  
                break;

            // Begin Motion
            case ACTION.MOVE:
                SetMovementSpeed(MOVEMENT.WALK);
                break;

            // When engaging, switch to jog movement.
            case ACTION.ENGAGE: 
                SetMovementSpeed(MOVEMENT.WALK);
                if (target == null) {
                    Debug.Log("I don't have a target.");
                    SetAction(ACTION.IDLE);
                }                
                break;

            // When attacking, switch to idle movement.
            case ACTION.ATTACK:
                SetMovementSpeed(MOVEMENT.IDLE);
                break;     
        }
    }

    public ACTION GetAction() {
        return currentAction;
    }

    ////////////////////////////////////////////////////
    // MOTION CONTROLS
    ////////////////////////////////////////////////////
    
    // Set NavMeshAgent target destination
    ///////////////////////////////////////////
    public void SetDestinationTarget(Vector3 target) {

        if (entity.IsDead) return;

        nmAgent.SetDestination(target);
        currentAction = ACTION.MOVE;
        SetMovementSpeed(MOVEMENT.WALK);
    }

    // Set the type of movement being made
    ///////////////////////////////////////////
    public void SetMovementSpeed(MOVEMENT mov) 
    {
        if (entity.IsDead) return;

        switch(mov) {
            case MOVEMENT.IDLE:                
                nmAgent.speed = 0.0f;          
                break;
            case MOVEMENT.WALK:
                nmAgent.speed = 3.5f;
                break;
        }

        animator.SetInteger("MoveType", (int)mov);
    }

    ////////////////////////////////////////////////////
    // COMBAT CONTROLS
    ////////////////////////////////////////////////////

    // Set Object Target
    ///////////////////////////////////////////
    public void SetTarget(GameObject obj) 
    {
        if (entity.IsDead) return;

        target = obj;             
    }

    ////////////////////////////////////////////////////
    // UTILITIES
    ////////////////////////////////////////////////////

    // Get a relative direction based on target direction
    private Vector3 GetDirection(DIRECTION direction) {
        switch (direction) {
            default:
            case DIRECTION.FORWARD:
                return transform.forward;
            case DIRECTION.BACKWARD:
                return -transform.forward;
            case DIRECTION.RIGHT:
                return transform.right;
            case DIRECTION.LEFT: 
                return -transform.right;
            case DIRECTION.TARGET:
                if (target == null) return transform.forward;
                return (target.transform.position - transform.position).normalized;
        }
    }

    private void OnDrawGizmos() {
        if (stats != null) {
            Gizmos.DrawWireSphere(transform.position, stats.Attack_Range);
        }
        
    }
}