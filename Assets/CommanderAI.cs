using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CommanderAI : MonoBehaviour
{
    [SerializeField] private GameManager gm;
    [SerializeField] public Team team;
    [SerializeField] private int unitPoint = 100;
    [SerializeField] private int unitMax = 10;
    [SerializeField] private List<Unit> units = new();
    [SerializeField] private Unit unitPrefab;

    private List<TargetPostInfo> targetPosts = new();
    private int targetPostsMax = 4;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(MainLoop());
    }

    private IEnumerator MainLoop()
    {
        while (true)
        {
            UpdateTargets();

            GenerateUnits();

            SendCommands();

            yield return new WaitForSeconds(0.3f);
        }
    }

    private void UpdateTargets()
    {
        foreach (var post in targetPosts.ToList())
        {
            // 占領済みの拠点を攻撃対象から取り除く。
            if (post.TargetPost.TeamOccupied == team) targetPosts.Remove(post);
            // 出撃拠点が占領されていたら取り除く。
            if (post.StartPost.TeamOccupied != team) targetPosts.Remove(post);
        }
        // 攻撃対象が足りていれば何もしない。
        if (targetPosts.Count >= targetPostsMax) return;

        // 未占領・非攻撃対象の拠点を取得する。
        var postsTeam = gm.GetTeamPosts(team);
        var postsRemain = gm.Posts.ToList();
        foreach (var p in postsTeam) postsRemain.Remove(p);
        foreach (var p in targetPosts) postsRemain.Remove(p.TargetPost);

        // 未占領の拠点の距離を求める。
        var postsRemainDistances = new Dictionary<Post, (Post near, float distance)>();
        foreach (var p in postsRemain)
        {
            var nearest = PostUtil.GetNearest(p, postsTeam);
            postsRemainDistances[p] = nearest;
        }

        // 未占領の拠点の距離が近い順にソートして追加していく。
        var cands = postsRemainDistances.OrderBy(kv => kv.Value.distance);
        foreach (var kv in cands)
        {
            targetPosts.Add(new TargetPostInfo()
            {
                TargetPost = kv.Key,
                StartPost = kv.Value.near,
            });
            // 攻撃対象が足りていれば終了する。
            if (targetPosts.Count >= targetPostsMax) return;
        }
    }

    private void GenerateUnits()
    {
        if (units.Count == unitMax) return;
        var newUnitCount = unitMax - units.Count;
        for (int i = 0; i < newUnitCount; i++)
        {
            var unit = Instantiate(unitPrefab);
            units.Add(unit);
            unit.Initialize(this);

            // 攻撃ユニットが足りていない攻撃対象へ送り込む。
            var targetPostInfo = targetPosts.OrderBy(p => p.AssignedUnitCount).First();
            // 出撃拠点に生成する。
            unit.transform.position = 
                targetPostInfo.StartPost.transform.position +
                new Vector3(Random.value, 0, Random.value);
            // 攻撃対象をセットする。
            unit.UpdateCommand(new OccupationCommand()
            {
                TargetPost = targetPostInfo.TargetPost,
            });
            targetPostInfo.AssignedUnitCount++;
        }
    }

    private void SendCommands()
    {
        foreach (var unit in units)
        {
            // 待機中のユニットがいれば、近くの拠点を占領しにいく。
            if (unit.Command is WaitCommand)
            {
                // 攻撃対象拠点のなかで一番近いところに向かう。
                var cands = targetPosts.OrderBy(p => Vector3.Distance(p.TargetPost.transform.position, unit.transform.position));
                var cands1 = cands.Where(p => p.AssignedUnitCount <= unitMax * 0.2f);
                var target = cands1.FirstOrDefault() ?? cands.FirstOrDefault();
                if (target != null)
                {
                    target.AssignedUnitCount++;
                    unit.UpdateCommand(new OccupationCommand()
                    {
                        TargetPost = target.TargetPost,
                    });
                }
            }
        }
    }

    internal void OnUnitDead(Unit unit)
    {
        units.Remove(unit);
        if (unit.Command is OccupationCommand cmd)
        {
            var info = targetPosts.FirstOrDefault(p => p.TargetPost == cmd.TargetPost);
            if (info != null)
            {
                info.AssignedUnitCount--;
            }
        }
    }
}

public class TargetPostInfo
{
    public Post TargetPost { get; set; }
    public Post StartPost { get; set; }
    public int AssignedUnitCount { get; set; } = 0;
}

public class Command
{
    public virtual bool CheckCompletion(Unit unit)
    {
        return false;
    }
    public virtual Command NextCommand { get; } = WaitCommand.Instance;
}

public class WaitCommand : Command
{
    public static WaitCommand Instance { get; } = new WaitCommand();
}

public class OccupationCommand : Command
{
    public Post TargetPost { get; set; }
    public override bool CheckCompletion(Unit unit)
    {
        return unit.Team == TargetPost.TeamOccupied;
    }
}
