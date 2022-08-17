using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Job : IXmlSerializable
{
    Action<Job> jobCancelled;
    Action<Job> jobFinished;
    public JobType jobType {private set; get; }
    public List<buildingRequirement> requiredMaterials;
    public Tile tile {get; protected set;}
    public bool manuallySetJobTime {get; private set;}
    public float jobTime {get; private set;}

    public Job(Tile tile, Action<Job> jobFinished, JobType jobType, List<buildingRequirement> requiredMaterials = null, float jobTime = 0.1f, bool manuallySetJobTime = false) {
        this.tile = tile;
        this.jobFinished += jobFinished;
        this.jobTime = jobTime;
        this.jobType = jobType;
        this.manuallySetJobTime = manuallySetJobTime;

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

    public void SetJobTime(float time)
    {
        this.jobTime = time;
        this.manuallySetJobTime = true;
    }

    public bool HasNoRequirements()
    {
        return requiredMaterials == null || requiredMaterials.Count == 0;
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

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("x", tile.x.ToString());
		writer.WriteAttributeString("y", tile.y.ToString());
        writer.WriteAttributeString("jobTime", this.jobTime.ToString());
        writer.WriteAttributeString("jobType", this.jobType.ToString());
        writer.WriteAttributeString("hasSetJobTime", this.manuallySetJobTime.ToString());

        if (requiredMaterials == null) return;
        
        string itemString = "";
        string amounts = "";

        foreach (buildingRequirement item in requiredMaterials)
        {
            itemString += item.material + "/";
            amounts += item.amount + "/";
        }

        writer.WriteAttributeString("items", itemString);
        writer.WriteAttributeString("amounts", amounts);
    }
}
