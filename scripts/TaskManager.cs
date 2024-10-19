using Godot;
using System;
using System.Collections.Generic;

public partial class TaskManager : Node
{
    private List<Task> taskQueue = new List<Task>();

    public Task RequestTask()
    {
        if (taskQueue.Count > 0)
        {
            var task = taskQueue[0];
            taskQueue.RemoveAt(0);

            if (task.TaskType == Task.BaseTaskType.RequirementDelivery)
            {
                var groupTasks = FindResourceDeliveryTasksWithSameRequirements(task);
                if (groupTasks.Count > 0)
                {
                    task.GroupSimilarRequirementDeliveries(groupTasks);
                    foreach (var t in groupTasks)
                    {
                        taskQueue.Remove(t);
                    }
                }
            }

            if ((int)Time.GetTicksMsec() - task.LastAttemptTime > 2000)
            {
                return task;
            }
            else
            {
                taskQueue.Add(task);
            }
        }
        return null;
    }

    private List<Task> FindResourceDeliveryTasksWithSameRequirements(Task task)
    {
        var list = new List<Task>();
        foreach (var t in taskQueue)
        {
            if (t.TaskType == Task.BaseTaskType.RequirementDelivery && task.GetTargetItemName() == t.GetTargetItemName())
            {
                list.Add(t);
            }
        }
        return list;
    }

    public Task RequestFindAndEatFoodTask()
    {
        var task = new Task();
        task.InitFindAndEatFoodTask();
        return task;
    }

    public void ReturnTaskUnfinished(Task task)
    {
        task.Restart();
        taskQueue.Add(task);

        if (task.GroupedTasks != null)
        {
            foreach (var t in task.GroupedTasks)
            {
                t.Restart();
                taskQueue.Add(t);
            }
        }
    }

    public void AddBuildOrder(Node targetItem)
    {
        targetItem.InitRequirements();  // 确保此方法在目标项上有效
        CreateResourceDeliveryTask(targetItem);
    }

    public void CreateResourceDeliveryTask(Node targetItem)
    {
        foreach (var requirement in targetItem.GetRequirements())
        {
            var newTask = new Task();
            newTask.InitDeliveryTask(targetItem, targetItem.GetRequirement(requirement));
            taskQueue.Add(newTask);
        }
    }

    public void ReportItemDeliveredTo(Node targetItem, Node item)
    {
        GD.Print("DELIVERED");
        GD.Print(targetItem.HasAllRequirements());
        if (targetItem.HasAllRequirements())
        {
            AddTask(Task.BaseTaskType.Build, targetItem);
        }
        else if (targetItem.DoesNeedRequirement(item.Name))
        {
            var newTask = new Task();
            newTask.InitDeliveryTask(targetItem, targetItem.GetRequirement(item.Name));
            taskQueue.Add(newTask);
        }
    }

    public void AddOrder(Task.Orders order, Node targetItem)
    {
        switch (order)
        {
            case Task.Orders.Cancel:
                break;
            case Task.Orders.Deconstruct:
                break;
            case Task.Orders.Mine:
                break;
            case Task.Orders.Chop:
                break;
            case Task.Orders.Harvest:
                AddTask(Task.BaseTaskType.Harvest, targetItem);
                break;
        }
    }

    public void AddTask(Task.BaseTaskType taskType, Node targetItem)
    {
        var newTask = new Task();

        switch (taskType)
        {
            case Task.BaseTaskType.Harvest:
                newTask.InitHarvestPlantTask(targetItem);
                taskQueue.Add(newTask);
                break;
            case Task.BaseTaskType.Build:
                newTask.InitBuildTask(targetItem);
                taskQueue.Add(newTask);
                break;
        }
    }
}
