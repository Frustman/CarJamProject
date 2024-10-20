using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class CustomizingItem
{
    public string key;
    public string name;
    public int imageIndex;
}


[Serializable]
public class Theme : CustomizingItem
{
}
