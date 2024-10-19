using Godot;
using System;
using System.Collections.Generic;

public partial class Task : Node
{
    public string TaskName { get; set; }
    public BaseTaskType TaskType { get; set; } = BaseTaskType.BaseTask;

    private List<Task> subTasks = new List<Task>();
    private int currentSubTask = 0;

    public List<Task> GroupedTasks { get; set; } = null;

    public Node TargetItem { get; set; }
    public string TargetItemType { get; set; }
    public int TargetItemAmount { get; set; } = 1;

    public int LastAttemptTime { get; set; } = 0;

    public enum BaseTaskType
    {
        BaseTask,
        FindItem,
        WalkTo,
        WalkNextTo,
        Pickup,
        Eat,
        Manipulate,
        Harvest,
        Build,
        Haul,
        RequirementDelivery
    }

    public enum Orders
    {
        Cancel,
        Deconstruct,
        Mine,
        Chop,
        Harvest
    }

    public bool IsFinished() => currentSubTask == subTasks.Count;

    public void Finish() => currentSubTask = subTasks.Count;

    public void Restart()
    {
        currentSubTask = 0;
        LastAttemptTime = (int)Time.GetTicksMsec();
    }

    public Task GetCurrentSubTask() => subTasks[currentSubTask];

    public void OnFinishSubTask() => currentSubTask++;

    public string GetTargetItemName() => GetSubTask(BaseTaskType.FindItem)?.TargetItemType;

    public int GetDeliveryAmount() => GetSubTask(BaseTaskType.RequirementDelivery)?.TargetItemAmount ?? 0;

    public Task GetSubTask(BaseTaskType subTaskType)
    {
        foreach (var task in subTasks)
        {
            if (task.TaskType == subTaskType)
                return task;
        }
        return null;
    }

    public Task GetNextTaskInGroup()
    {
        GD.Print("获取下一个任务...");
        if (GroupedTasks != null && GroupedTasks.Count > 0)
        {
            var nextTask = GroupedTasks[0];
            GroupedTasks.RemoveAt(0);
            nextTask.GroupedTasks = GroupedTasks;
            return nextTask;
        }
        return null;
    }

    public void GoBackToSubTask(BaseTaskType subTaskType)
    {
        while (currentSubTask >= 0)
        {
            currentSubTask--;
            if (GetCurrentSubTask().TaskType == subTaskType)
                break;
        }
    }

    public void SkipAheadToSubTask(BaseTaskType subTaskType)
    {
        while (!IsFinished())
        {
            OnFinishSubTask();
            if (GetCurrentSubTask().TaskType == subTaskType)
                break;
        }
    }

    public void GroupSimilarRequirementDeliveries(List<Task> similarDeliveryTasks)
    {
        int totalResourcesNeeded = 0;
        foreach (var task in similarDeliveryTasks)
        {
            totalResourcesNeeded += task.GetSubTask(BaseTaskType.Pickup)?.TargetItemAmount ?? 0;
        }
        GetSubTask(BaseTaskType.Pickup).TargetItemAmount += totalResourcesNeeded;
        GroupedTasks = similarDeliveryTasks;
    }

    public void OnFoundItem(Node item)
    {
        OnFinishSubTask();
        GetCurrentSubTask().TargetItem = item;
    }

    public void OnReachedDestination()
    {
        OnFinishSubTask();
        GetCurrentSubTask().TargetItem = subTasks[currentSubTask - 1].TargetItem;
    }

    public void InitDeliveryTask(Node target, string[] requirement)
    {
        TaskType = BaseTaskType.RequirementDelivery;

        var subTask = new Task { TaskType = BaseTaskType.FindItem, TargetItemType = requirement[0] };
        subTasks.Add(subTask);

        subTasks.Add(new Task { TaskType = BaseTaskType.WalkTo });
        subTasks.Add(new Task { TaskType = BaseTaskType.Pickup, TargetItemAmount = int.Parse(requirement[1]) });
        subTasks.Add(new Task { TaskType = BaseTaskType.WalkNextTo, TargetItem = target });
        subTasks.Add(new Task { TaskType = BaseTaskType.RequirementDelivery, TargetItem = target, TargetItemAmount = int.Parse(requirement[1]) });
    }

    public void InitHarvestPlantTask(Node target)
    {
        TaskType = BaseTaskType.Harvest;

        subTasks.Add(new Task { TaskType = BaseTaskType.WalkTo, TargetItem = target });
        subTasks.Add(new Task { TaskType = BaseTaskType.Harvest, TargetItem = target });
    }

    public void InitBuildTask(Node target)
    {
        subTasks.Add(new Task { TaskType = BaseTaskType.WalkNextTo, TargetItem = target });
        subTasks.Add(new Task { TaskType = BaseTaskType.Build, TargetItem = target });
    }

    public void InitFindAndEatFoodTask()
    {
        TaskName = "Find and eat some food";

        subTasks.Add(new Task { TaskType = BaseTaskType.FindItem, TargetItemType = "FOOD" });
        subTasks.Add(new Task { TaskType = BaseTaskType.WalkTo });
        subTasks.Add(new Task { TaskType = BaseTaskType.Pickup });
        subTasks.Add(new Task { TaskType = BaseTaskType.Eat });
    }
}
