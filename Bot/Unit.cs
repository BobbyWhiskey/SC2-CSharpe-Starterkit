using System.Numerics;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot;

public class Unit
{
    public readonly CloakState cloak;
    public readonly DisplayType displayType;
    private readonly SC2APIProtocol.Unit original;
    private readonly UnitTypeData unitTypeData;
    public int assignedWorkers;
    public float buildProgress;
    public float energy;
    public float energyMax;
    public int idealWorkers;
    public float integrity;
    public bool isVisible;

    public string name;
    public UnitOrder order;
    public RepeatedField<UnitOrder> orders;
    public Vector3 position;
    public int supply;
    public ulong tag;
    public uint unitType;

    public Unit(SC2APIProtocol.Unit unit)
    {
        original = unit;
        unitTypeData = Controller.gameData.Units[(int)unit.UnitType];

        name = unitTypeData.Name;
        tag = unit.Tag;
        unitType = unit.UnitType;
        position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        integrity = (unit.Health + unit.Shield) / (unit.HealthMax + unit.ShieldMax);
        buildProgress = unit.BuildProgress;
        idealWorkers = unit.IdealHarvesters;
        assignedWorkers = unit.AssignedHarvesters;
        energy = unit.Energy;
        energyMax = unit.EnergyMax;

        order = unit.Orders.Count > 0 ? unit.Orders[0] : new UnitOrder();
        orders = unit.Orders;
        isVisible = unit.DisplayType == DisplayType.Visible;
        displayType = unit.DisplayType;
        cloak = unit.Cloak;

        supply = (int)unitTypeData.FoodRequired;
    }

    public ulong GetAddonTag()
    {
        return original.AddOnTag;
    }

    public uint? GetAddonType()
    {
        var tag = GetAddonTag();
        var unit = Controller.GetUnits(Units.AddOns).FirstOrDefault(x => x.tag == tag);
        return unit?.unitType;
    }

    public double GetDistance(Unit otherUnit)
    {
        return Vector3.Distance(position, otherUnit.position);
    }

    public double GetDistance(Vector3 location)
    {
        return Vector3.Distance(position, location);
    }

    public void Ability(int abilityID)
    {
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        Controller.AddAction(action);

        //var targetName = Controller.GetUnitName(unitType);
        //Logger.Info("Started research on {0}", targetName);
    }

    public void Ability(int abilityID, Vector3 target)
    {
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        Controller.AddAction(action);

        //var targetName = Controller.GetUnitName(unitType);
        //Logger.Info("Started research on {0}", targetName);
    }

    public void Ability(int abilityID, Unit target)
    {
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        action.ActionRaw.UnitCommand.TargetUnitTag = target.tag;
        Controller.AddAction(action);

        //var targetName = Controller.GetUnitName(unitType);
        //Logger.Info("Started research on {0}", targetName);
    }

    public void Train(uint unitType, bool queue = false)
    {
        if (!queue && orders.Count > 0)
        {
            return;
        }

        var abilityID = Abilities.GetID(unitType);
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        Controller.AddAction(action);

        var targetName = Controller.GetUnitName(unitType);
        Logger.Info("Started training: {0}", targetName);
    }

    private void FocusCamera()
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.CameraMove = new ActionRawCameraMove();
        action.ActionRaw.CameraMove.CenterWorldSpace = new Point();
        action.ActionRaw.CameraMove.CenterWorldSpace.X = position.X;
        action.ActionRaw.CameraMove.CenterWorldSpace.Y = position.Y;
        action.ActionRaw.CameraMove.CenterWorldSpace.Z = position.Z;
        Controller.AddAction(action);
    }


    public void Move(Vector3 target)
    {
        var action = Controller.CreateRawUnitCommand(Abilities.MOVE);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        Controller.AddAction(action);
    }

    public void Smart(Unit unit)
    {
        var action = Controller.CreateRawUnitCommand(Abilities.SMART);
        action.ActionRaw.UnitCommand.TargetUnitTag = unit.tag;
        action.ActionRaw.UnitCommand.UnitTags.Add(tag);
        Controller.AddAction(action);
    }
}