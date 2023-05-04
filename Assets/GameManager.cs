using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private NavMeshAgent player;
    [SerializeField] private NavMeshSurface floor;
    [SerializeField] private Post[] posts;

    public Post[] Posts => posts;

    internal Post[] GetTeamPosts(Team team)
    {
        return posts.Where(p => p.TeamOccupied == team).ToArray();
    }

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit))
            {
                player.SetDestination(hit.point);
            }
        }
    }
}
