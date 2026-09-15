namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Status codes indicating the result of an equipment operation.
    /// </summary>
    public enum EquipResultStatus
    {
        Success = 0,
        InvalidSlot = 1,
        SlotOccupied = 2,
        MissingPrefab = 3,
        MissingBone = 4,
        IncompatibleRig = 5,
        ItemNull = 6
    }

    /// <summary>
    /// Explicit result object returned by equipment operations.
    /// Provides status, visual instance, and contextual error messages.
    /// </summary>
    public readonly struct EquipResult
    {
        public EquipResultStatus Status { get; }
        public EquippedItemInstance Instance { get; }
        public string ErrorMessage { get; }

        public bool IsSuccess => Status == EquipResultStatus.Success;

        public EquipResult(EquipResultStatus status, EquippedItemInstance instance = null, string errorMessage = null)
        {
            Status = status;
            Instance = instance;
            ErrorMessage = errorMessage;
        }

        public static EquipResult Succeeded(EquippedItemInstance instance) =>
            new EquipResult(EquipResultStatus.Success, instance);

        public static EquipResult Failed(EquipResultStatus status, string errorMessage) =>
            new EquipResult(status, null, errorMessage);
    }
}
