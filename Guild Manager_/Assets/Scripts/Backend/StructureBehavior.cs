using System.Reflection;
using System.Globalization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class StructureBehavior
{


    public static void Update_Door(Structure structure, float deltaTime)
    {
        float openness = Convert.ToSingle(structure.optionalParameters["openness"]);
        if ((bool) structure.optionalParameters["doorIsOpening"])
        {
            openness += deltaTime / (float) structure.optionalParameters["doorOpenTime"];
        }

        else
        {
            openness -= deltaTime / (float) structure.optionalParameters["doorOpenTime"];
        }

        openness = Mathf.Clamp01(openness);
        structure.optionalParameters["openness"] = openness;
    }

    public static MethodInfo GetMethodInfo(string methodName)
    {
        MethodInfo method = typeof(StructureBehavior).GetMethod(methodName);

        if (method != null)
        {
            return method;
        }

        Debug.LogError(methodName + " is not a valid method name.");
        return null;
    }
}