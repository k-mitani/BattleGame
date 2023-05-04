using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    [SerializeField] private Team team;
    public Team Team => team;

    [SerializeField] private float attackRange = 10;
    [SerializeField] private float attackRange2 = 3;
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private MeshRenderer visual;
    [SerializeField] private int hpMax = 3;
    [SerializeField] private int hp;
    [SerializeField] private float attackForce = 100;

    private NavMeshAgent agent;
    private CommanderAI commander;
    private Command command = WaitCommand.Instance;
    private UnitAction action = UnitActionObeyCommand.Instance;

    public Command Command => command;

    private void Start()
    {
        hp = hpMax;
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.3f);
            if (action is UnitActionObeyCommand)
            {
                if (command.CheckCompletion(this))
                {
                    UpdateCommand(WaitCommand.Instance);
                }
                if (command is OccupationCommand com)
                {
                    if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
                    {
                        try
                        {
                            agent.destination = com.TargetPost.transform.position;
                        }
                        catch (Exception ex)
                        {
                            Debug.Log(ex);
                            throw;
                        }
                    }
                }

                // 近くに敵がいたら攻撃する。
                var cols = Physics.OverlapSphere(transform.position, attackRange, unitLayer);
                for (int i = 0; i < cols.Length; i++)
                {
                    var target = cols[i].GetComponent<Unit>();
                    if (target == this) continue;
                    if (target.team == team) continue;
                    action = new UnitActionAttack { Target = target };
                    break;
                }
            }
            if (action is UnitActionAttack act)
            {
                var target = act.Target;
                if (target.IsDestroyed())
                {
                    action = UnitActionObeyCommand.Instance;
                }
                else if (Vector3.Distance(transform.position, target.transform.position) <= attackRange2)
                {
                    var force = (transform.position - target.transform.position).normalized * attackForce;
                    //target.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
                    transform.LookAt(target.transform.position);
                    target.OnAttacked(this);
                    if (target.IsDestroyed())
                    {
                        action = UnitActionObeyCommand.Instance;
                    }
                }
            }
        }
    }

    private void OnAttacked(Unit attacker)
    {
        hp--;
        if (hp <= 0)
        {
            Destroy(gameObject);
            commander.OnUnitDead(this);
        }
    }

    private void Update()
    {
        if (action is UnitActionAttack act)
        {
            if (!act.Target.IsDestroyed())
            {
                agent.destination = act.Target.transform.position;
                transform.LookAt(agent.destination);
            }
        }
    }

    internal void Initialize(CommanderAI commander)
    {
        this.commander = commander;
        team = commander.team;
        visual.material = Post.TeamToMaterial(team);
        TryGetComponent(out agent);
    }

    internal void UpdateCommand(Command command)
    {
        this.command = command;
        if (action is UnitActionObeyCommand)
        {
            if (command is OccupationCommand com)
            {
                agent.destination = com.TargetPost.transform.position;
            }
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.magenta;
        //Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private class UnitAction
    {

    }
    private class UnitActionAttack : UnitAction
    {
        public Unit Target { get; set; }
    }
    private class UnitActionObeyCommand : UnitAction
    {
        public static UnitActionObeyCommand Instance = new UnitActionObeyCommand();
    }
}
