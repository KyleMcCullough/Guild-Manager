using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JobQueue
{

    LinkedList<Job> jobList;
    Action<Job> jobCreated;
    public int Count {private set{} get {
        return jobList.Count;
    }}

    public JobQueue()
    {
        jobList = new LinkedList<Job>();
    }

    public void AddLast(Job job) 
    {

        if (jobList.Contains(job))
        {
            return;
        }
        jobList.AddLast(job);

        if (jobCreated != null) {
            jobCreated(job);
        }
    }

    public void AddFirst(Job job)
    {
        if (jobList.Contains(job))
        {
            return;
        }
        jobList.AddFirst(job);

        if (jobCreated != null) {
            jobCreated(job);
        }
    }

    public void RegisterJobCreationCallback(Action<Job> callback) {
        jobCreated += callback;
    }

    public Job Dequeue() {

        if (jobList.Count == 0) return null;

        Job j = jobList.First.Value;
        jobList.RemoveFirst();
        return j;
    }

    public Job Peek() {
        return jobList.First.Value;
    }

    public List<Job> ToArray()
    {
        return new List<Job>(jobList);
    }

    public void Clear()
    {
        jobList.Clear();
    }
}
