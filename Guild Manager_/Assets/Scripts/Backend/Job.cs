using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Job
{
    public Tile tile {get; protected set;}
    float jobTime;

    Action<Job> jobFinished;
    Action<Job> jobCancelled;
    public List<buildingRequirement> requiredMaterials;

    public Job(Tile tile, Action<Job> jobFinished, List<buildingRequirement> requiredMaterials = null, float jobTime = 0.1f) {
        this.tile = tile;
        this.jobFinished += jobFinished;
        this.jobTime = jobTime;

        if (requiredMaterials != null)
        {
            // Hard copies building requirements.
            this.requiredMaterials = requiredMaterials.ConvertAll(material => new buildingRequirement(material.material, material.amount));
        }

        else
        {
            this.requiredMaterials = null;
        }
    }

    public void RegisterJobCompleteCallback(Action<Job> callback) {
        this.jobFinished += callback;
    }

    public void RegisterJobCancelCallback(Action<Job> callback) {
        this.jobCancelled += callback;
    }

    public void UnregisterJobCompleteCallback(Action<Job> callback) {
        this.jobFinished -= callback;
    }

    public void UnregisterJobCancelCallback(Action<Job> callback) {
        this.jobCancelled -= callback;
    }

    public void DoWork(float workTime) {
        jobTime -= workTime;

        if (jobTime <= 0 && jobFinished != null) {
            jobFinished(this);
        }
    }

    public int GiveMaterial(string material, int amount)
    {
        foreach (buildingRequirement requirement in this.requiredMaterials)
        {
            if (requirement.material == material)
            {
                if (amount > requirement.amount)
                {
                    amount = amount - requirement.amount;
                    requirement.amount = 0;
                }

                else
                {
                    int difference = requirement.amount - amount;
                    requirement.amount -= amount;

                    amount = difference;
                }

                if (requirement.amount == 0)
                {
                    this.requiredMaterials.Remove(requirement);
                }

                return amount;
            }
        }

        return amount;
    }

    public bool IsRequiredType(string type)
    {
        foreach (buildingRequirement requirement in this.requiredMaterials.ToArray())
        {
            if (requirement.material == type)
            {
                return true;
            }
        }
        return false;
    }

    public void CancelJob() {
        if (jobCancelled != null) {
            jobCancelled(this);
        }
    }
}
