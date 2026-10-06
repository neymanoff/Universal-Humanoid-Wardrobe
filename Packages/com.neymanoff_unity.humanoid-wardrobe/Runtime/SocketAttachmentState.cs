namespace Neymanoff.HumanoidWardrobe
{
    /// <summary>
    /// Visual attachment state of an equipped item (in-hand active vs holstered/stowed on hip, back, or belt).
    /// Used by action RPGs, tactical shooters, and survival games for drawing and stowing weapons.
    /// </summary>
    public enum SocketAttachmentState
    {
        /// <summary>
        /// Item is held actively in hand / primary socket.
        /// </summary>
        Drawn = 0,

        /// <summary>
        /// Item is stowed/sheathed on holster, back sling, chest rig, or belt.
        /// </summary>
        Holstered = 1
    }
}
