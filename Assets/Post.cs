using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;

public class Post : MonoBehaviour
{
    [Header("Child References")]
    [SerializeField] private MeshRenderer visual;
    [SerializeField] private TextMeshPro occupationProgressLabel;

    [Header("Static")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material team1Material;
    [SerializeField] private Material team2Material;
    [SerializeField] private float areaRange = 3;
    [SerializeField] private LayerMask unitLayer;

    [Header("Dynamic")]
    [SerializeField] private Team teamOccupied;
    [SerializeField] private Team teamOccupying;
    [SerializeField] private int occupationProgressMax = 10;
    [SerializeField] private int occupationProgress;

    public Team TeamOccupied => teamOccupied;

    private void Awake()
    {
        visual.material = TeamToMaterial(teamOccupied);
    }

    // Start is called before the first frame update
    void Start()
    {
        occupationProgressLabel.gameObject.SetActive(false);
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            TickOccupation();
        }
    }

    private void TickOccupation()
    {
        // 周辺にいるUnitを取得する。
        var cols = Physics.OverlapSphere(transform.position, areaRange, unitLayer);
        // 一つもいない場合
        if (cols.Length == 0)
        {
            // 占領中だったら、占領度を1減らす。
            if (teamOccupying != Team.None)
            {
                occupationProgress--;
                occupationProgressLabel.text = occupationProgress.ToString();
                if (occupationProgress <= 0)
                {
                    teamOccupying = Team.None;
                    occupationProgress = 0;
                    occupationProgressLabel.gameObject.SetActive(false);
                }
            }
            return;
        }

        // Unitがいる場合
        var units = new Unit[cols.Length];
        var team1Count = 0;
        var team2Count = 0;
        for (int i = 0; i < cols.Length; i++)
        {
            var unit = cols[i].GetComponent<Unit>();
            if (unit.Team == Team.Team1) team1Count++;
            else team2Count++;
            units[i] = unit;
        }

        // 違うチームが混ざっている場合
        if (team1Count != 0 && team2Count != 0)
        {
            // TODO 点滅か何かさせる。
            return;
        }

        // 単一のチームの場合
        var team = team1Count != 0 ? Team.Team1 : Team.Team2;
        // すでに占領済みなら何もしない。
        if (team == teamOccupied) return;

        // 占領継続中の場合
        var prevTeam = teamOccupying;
        if (team == prevTeam)
        {
            // 占領度を1増やす。
            occupationProgress++;
            occupationProgressLabel.text = occupationProgress.ToString();
            // 占領完了した場合
            if(occupationProgress > occupationProgressMax)
            {
                teamOccupied = team;
                teamOccupying = Team.None;
                occupationProgress = 0;
                occupationProgressLabel.gameObject.SetActive(false);
                visual.material = TeamToMaterial(teamOccupied);
            }
            return;
        }
        // 占領開始した場合
        teamOccupying = team;
        occupationProgress = 0;
        occupationProgressLabel.gameObject.SetActive(true);
        occupationProgressLabel.text = occupationProgress.ToString();
        occupationProgressLabel.color = TeamToColor(teamOccupying);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = TeamToColor(teamOccupied);
        Gizmos.DrawWireSphere(transform.position, areaRange);
    }

    public Color TeamToColor(Team team)
    {
        switch (team)
        {
            case Team.None: return Color.yellow;
            case Team.Team1: return Color.blue;
            case Team.Team2: return Color.red;
            default: return Color.white;
        }
    }
    public Material TeamToMaterial(Team team)
    {
        switch (team)
        {
            case Team.None: return defaultMaterial;
            case Team.Team1: return team1Material;
            case Team.Team2: return team2Material;
            default: return defaultMaterial;
        }
    }
}

public enum Team
{
    None,
    Team1,
    Team2,
}
