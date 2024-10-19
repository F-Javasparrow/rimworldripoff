using Godot;
using System;

public partial class PawnAI : Node
{
    private TaskManager taskManager;
    private ItemManager itemManager;
    private Terrain terrain;
    private Node charController;
    private Node hungerBar;

    private enum PawnAction { Idle, DoingSubTask }
    private PawnAction currentAction = PawnAction.Idle;

    private Task currentTask = null;

    private float foodNeed = 1f; // 0 = min, 1 = max
    private float eatSpeed = 0.5f;
    private float foodNeedDepleteSpeed = 0.001f;

    private float harvestSkill = 1f;
    private float buildSkill = 1f;

    private Item inHand;

    public override void _Ready()
    {
        taskManager = GetNode<TaskManager>("../../TaskManager");
        itemManager = GetNode<ItemManager>("../../ItemManager");
        terrain = GetNode<Terrain>("../../Terrain");
        charController = GetNode<Node>("..");
        hungerBar = GetNode<Node>("../hungerBar");
    }

    public override void _Process(double delta)
    {
        if (currentTask != null)
        {
            DoCurrentTask((float)delta);
        }
        else
        {
            if (foodNeed < 0.5f)
            {
                currentTask = taskManager.RequestFindAndEatFoodTask();
            }
            else
            {
                currentTask = taskManager.RequestTask();
            }
        }
    }

    private void OnPickupItem(Item item, int amount)
    {
        Item inHandTemp = (Item)item.Duplicate();

        if (item.count < amount)
        {
            amount = inHandTemp.count;
        }
        inHandTemp.count = amount;

        if (inHand != null)
        {
            if (inHand.Name == inHandTemp.Name)
            {
                inHand.count += inHandTemp.count;
            }
            else
            {
                GD.Print("ERROR: did you just pick up " + inHandTemp.Name + " while you were already carrying " + inHand.Name + "?");
            }
        }
        else
        {
            inHand = inHandTemp;
        }

        GD.Print("picked up " + amount + " wood, now carrying " + inHand.count + " wood");
        terrain.RemoveItemFromWorld(item, amount);
    }

    private void DeliverRequirement(Target target)
    {
        if (inHand == null)
        {
            GD.Print("ERROR: cannot deliver, no item in hand");
            return;
        }

        int dropAmount = currentTask.GetDeliveryAmount();
        if (inHand.count < dropAmount)
        {
            dropAmount = inHand.count;
        }

        target.DeliverRequirement(inHand.Name, dropAmount);
        taskManager.ReportItemDeliveredTo(target, inHand);
        inHand.count -= dropAmount;

        GD.Print("dropped off " + dropAmount + " wood, now carrying " + inHand.count + " wood");

        if (inHand.count <= 0)
        {
            inHand = null;
        }
    }

    private void OnFinishedSubTask()
    {
        currentAction = PawnAction.Idle;

        if (currentTask.IsFinished())
        {
            if (currentTask.GroupedTasks != null)
            {
                currentTask = currentTask.GetNextTaskInGroup();
                GD.Print("Got next task in group");
            }
            else
            {
                currentTask = null;
            }
        }
    }

    private void DoCurrentTask(float delta)
    {
        var subTask = currentTask.GetCurrentSubTask();

        if (currentAction == PawnAction.Idle)
        {
            StartCurrentSubTask(subTask);
        }
        else
        {
            switch (subTask.TaskType)
            {
                case Task.BaseTaskType.WalkTo:
                case Task.BaseTaskType.WalkNextTo:
                    if (charController.HasReachedDestination())
                    {
                        currentTask.OnReachedDestination();
                        OnFinishedSubTask();
                    }
                    break;

                case Task.BaseTaskType.Eat:
                    if (inHand.Nutrition > 0 && foodNeed < 1)
                    {
                        inHand.Nutrition -= eatSpeed * delta;
                        foodNeed += eatSpeed * delta;
                    }
                    else
                    {
                        GD.Print("finished eating food");
                        inHand = null;

                        currentTask.OnFinishSubTask();
                        OnFinishedSubTask();
                    }
                    break;

                case Task.BaseTaskType.Harvest:
                    var targetItem = currentTask.GetCurrentSubTask().TargetItem;
                    if (targetItem.TryHarvest(harvestSkill * delta))
                    {
                        currentTask.OnFinishSubTask();
                        OnFinishedSubTask();
                    }
                    else
                    {
                        GD.Print(targetItem.HarvestProgress);
                    }
                    break;

                case Task.BaseTaskType.Build:
                    targetItem = currentTask.GetCurrentSubTask().TargetItem;
                    if (targetItem.TryBuild(buildSkill * delta))
                    {
                        currentTask.OnFinishSubTask();
                        OnFinishedSubTask();
                    }
                    break;
            }
        }
    }

    private bool HasItemInHand(string itemName)
    {
        return inHand != null && inHand.Name == itemName;
    }

    private void StartCurrentSubTask(Task.SubTask subTask)
    {
        GD.Print("Starting subtask: " + Task.BaseTaskType.Keys[(int)subTask.TaskType]);

        switch (subTask.TaskType)
        {
            case Task.BaseTaskType.FindItem:
                if (HasItemInHand(subTask.TargetItemType))
                {
                    GD.Print("Hey, I'm already holding some " + subTask.TargetItemType);
                    currentTask.SkipAheadToSubTask(Task.BaseTaskType.WalkNextTo);
                    return;
                }

                var targetItem = terrain.FindNearestItem(subTask.TargetItemType, charController.GlobalPosition);
                if (targetItem == null)
                {
                    GD.Print("no item, force task to finish");
                    taskManager.ReturnTaskUnfinished(currentTask);
                    currentTask = null;
                    currentAction = PawnAction.Idle;
                    return;
                }
                else
                {
                    currentTask.OnFoundItem(targetItem);
                }

                OnFinishedSubTask();
                break;

            case Task.BaseTaskType.WalkTo:
                charController.SetMoveTarget(subTask.TargetItem.Position);
                currentAction = PawnAction.DoingSubTask;
                break;

            case Task.BaseTaskType.WalkNextTo:
                charController.SetMoveTarget(subTask.TargetItem.Position, true);
                currentAction = PawnAction.DoingSubTask;
                break;

            case Task.BaseTaskType.Pickup:
                OnPickupItem(subTask.TargetItem, subTask.TargetItemAmount);

                if (inHand.Count < subTask.TargetItemAmount)
                {
                    targetItem = terrain.FindNearestItem(inHand.Name, charController.GlobalPosition);
                    if (targetItem != null)
                    {
                        currentTask.GoBackToSubTask(Task.BaseTaskType.FindItem);
                        currentTask.OnFoundItem(targetItem);
                    }
                    else
                    {
                        currentTask.OnFinishSubTask();
                    }
                }
                else
                {
                    currentTask.OnFinishSubTask();
                }
                OnFinishedSubTask();
                break;

            case Task.BaseTaskType.Eat:
            case Task.BaseTaskType.Harvest:
            case Task.BaseTaskType.Build:
                currentAction = PawnAction.DoingSubTask;
                break;

            case Task.BaseTaskType.RequirementDelivery:
                DeliverRequirement(subTask.TargetItem);
                currentTask.OnFinishSubTask();
                OnFinishedSubTask();
                break;
        }
    }
}
