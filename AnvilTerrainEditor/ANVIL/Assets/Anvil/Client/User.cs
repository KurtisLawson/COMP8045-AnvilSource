using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class User
{
    public int userId { get; set; }
    public int id { get; set; }
    public string title { get; set; }
    public float[] verts { get; set; }
    public bool completed { get; set; }
}
