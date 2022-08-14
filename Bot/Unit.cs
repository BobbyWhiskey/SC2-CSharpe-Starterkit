using System.Numerics;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot;

public class Unit
{
    public CloakState Cloak { get; }
    public DisplayType DisplayType { get; }
    public SC2APIProtocol.Unit Original { get; }
    private UnitTypeData UnitTypeData { get; }
    public int AssignedWorkers { get; }
    public float BuildProgress { get; }
    public float Energy { get; }
    public float EnergyMax { get; }
    public int IdealWorkers { get; }
    public float Integrity { get; }
    public bool IsVisible { get; }
    public string Name { get; }
    public UnitOrder Order { get; }
    public RepeatedField<UnitOrder> Orders { get; }
    public Vector3 Position { get; }
    public int Supply { get; }
    public ulong Tag { get; }
    public uint UnitType { get; }
    public bool IsBurrowed { get; }

    public Unit(SC2APIProtocol.Unit unit)
    {
        Original = unit;
        UnitTypeData = Controller.GameData.Units[(int)unit.UnitType];

        Name = UnitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        Integrity = (unit.Health + unit.Shield) / (unit.HealthMax + unit.ShieldMax);
        BuildProgress = unit.BuildProgress;
        IdealWorkers = unit.IdealHarvesters;
        AssignedWorkers = unit.AssignedHarvesters;
        Energy = unit.Energy;
        EnergyMax = unit.EnergyMax;

        Order = unit.Orders.Count > 0 ? unit.Orders[0] : new UnitOrder();
        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible;
        DisplayType = unit.DisplayType;
        IsBurrowed = unit.IsBurrowed;
        Cloak = unit.Cloak;

        Supply = (int)UnitTypeData.FoodRequired;
    }

    public ulong GetAddonTag()
    {
        return Original.AddOnTag;
    }

    public uint? GetAddonType()
    {
        var tag = GetAddonTag();
        var unit = Controller.GetUnits(Units.AddOns).FirstOrDefault(x => x.Tag == tag);
        return unit?.UnitType;
    }

    public double GetDistance(Unit otherUnit)
    {
        return Vector3.Distance(Position, otherUnit.Position);
    }

    public double GetDistance(Vector3 location)
    {
        return Vector3.Distance(Position, location);
    }

    public void Ability(int abilityID)
    {
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
        Controller.AddAction(action);

        //var targetName = Controller.GetUnitName(unitType);
        //Logger.Info("Started research on {0}", targetName);
    }

    public void Ability(int abilityID, Vector3 target)
    {
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
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
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
        action.ActionRaw.UnitCommand.TargetUnitTag = target.Tag;
        Controller.AddAction(action);

        //var targetName = Controller.GetUnitName(unitType);
        //Logger.Info("Started research on {0}", targetName);
    }

    public void Train(uint unitType, bool queue = false)
    {
        if (!queue && Orders.Count > 0)
        {
            return;
        }

        var abilityID = Abilities.GetID(unitType);
        var action = Controller.CreateRawUnitCommand(abilityID);
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
        Controller.AddAction(action);

        var targetName = Controller.GetUnitName(unitType);
    }

    private void FocusCamera()
    {
        var action = new Action();
        action.ActionRaw = new ActionRaw();
        action.ActionRaw.CameraMove = new ActionRawCameraMove();
        action.ActionRaw.CameraMove.CenterWorldSpace = new Point();
        action.ActionRaw.CameraMove.CenterWorldSpace.X = Position.X;
        action.ActionRaw.CameraMove.CenterWorldSpace.Y = Position.Y;
        action.ActionRaw.CameraMove.CenterWorldSpace.Z = Position.Z;
        Controller.AddAction(action);
    }


    public void Move(Vector3 target)
    {
        var action = Controller.CreateRawUnitCommand(Abilities.MOVE);
        action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D();
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.X = target.X;
        action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y = target.Y;
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
        Controller.AddAction(action);
    }

    public void Smart(Unit unit)
    {
        var action = Controller.CreateRawUnitCommand(Abilities.SMART);
        action.ActionRaw.UnitCommand.TargetUnitTag = unit.Tag;
        action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
        Controller.AddAction(action);
    }
}