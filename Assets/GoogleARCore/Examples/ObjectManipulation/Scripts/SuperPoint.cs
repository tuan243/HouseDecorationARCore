using UnityEngine;
using System.Collections.Generic;

public class SuperPoint
{
    public Vector3 loc;
    public Dictionary<int, float> list_score;
    public List<Vector3> list_view;
    public List<float> list_scale;
    public int id;

    public SuperPoint(int id, Vector3 l, int label, float score, Vector3 view, float scale)
    {
        this.id = id;
        loc = l;
        list_score = new Dictionary<int, float>();
        list_score.Add(label, score);
        list_view = new List<Vector3>();
        list_view.Add(view);
        list_scale = new List<float>();
        list_scale.Add(scale);
    }
}