
namespace nickmaltbie.Treachery.Equipment
{
    public enum WeaponType
    {
        Melee,
        Ranged,
    }

    public interface IWeapon : IEquipment
    {
        WeaponType WeaponType { get; }
    }
}