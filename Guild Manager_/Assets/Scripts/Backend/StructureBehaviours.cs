using System.Reflection;
using System.Globalization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class StructureBehaviours
{


    public static void Update_Door(Structure structure, float deltaTime)
    {
        Debug.Log("Update_Door");
        
    }

    public static MethodInfo GetMethodInfo(string methodName)
    {
        MethodInfo method = typeof(StructureBehaviours).GetMethod(methodName);

        if (method != null)
        {
            return method;
        }

        Debug.LogError(methodName + " is not a valid method name.");
        return null;
    }
}