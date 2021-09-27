using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Job
{
    public Tile tile {get; protected set;}
    float jobTime;

    Action<Job> jobFinished;
    Action<Job> jobCancelled;

    public Job(Tile tile, Action<Job> jobFinished, float jobTime = 0.1f) {
        this.tile = tile;
        this.jobFinished += jobFinished;
        this.jobTime = jobTime;
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

    public void CancelJob() {
        if (jobCancelled != null) {
            jobCancelled(this);
        }
    }
}
