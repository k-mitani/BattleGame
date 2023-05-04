using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    [SerializeField] private Team team;
    public Team Team => team;

    private NavMeshAgent agent;
    private CommanderAI commander;
    private Command command = WaitCommand.Instance;

    public Command Command => command;

    private void Start()
    {
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (true)
        {
            if (command.CheckCompletion(this))
            {
                UpdateCommand(WaitCommand.Instance);
            }
            yield return new WaitForSeconds(1);
        }
    }

    internal void Initialize(CommanderAI commander)
    {
        this.commander = commander;
        team = commander.team;
        TryGetComponent(out agent);
    }

    internal void UpdateCommand(Command command)
    {
        this.command = command;
        if (command is OccupationCommand com)
        {
            agent.destination = com.TargetPost.transform.position;
        }
    }
}
