using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JobQueue
{

    Queue<Job> jobQueue;
    Action<Job> jobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job job) 
    {
        
        if (jobQueue.Contains(job))
        {
            // Debug.LogError("Job is being requeued, not triggering jobCreated.");
            return;
        }
        jobQueue.Enqueue(job);

        if (jobCreated != null) {
            jobCreated(job);
        }
    }

    public void RegisterJobCreationCallback(Action<Job> callback) {
        jobCreated += callback;
    }

    public Job Dequeue() {

        if (jobQueue.Count == 0) return null;
        return jobQueue.Dequeue();
    }
}
