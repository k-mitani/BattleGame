using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PostUtil
{
    public static (Post, float) GetNearest(Post target, IEnumerable<Post> cands)
    {
        var nearest = default(Post);
        var nearestDistance = float.MaxValue;
        foreach (var c in cands)
        {
            var d = Vector3.Distance(c.transform.position, target.transform.position);
            if (d < nearestDistance)
            {
                nearest = c;
                nearestDistance = d;
            }
        }
        return (nearest, nearestDistance);
    }
}
