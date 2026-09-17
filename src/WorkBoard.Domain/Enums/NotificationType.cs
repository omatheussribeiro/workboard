
namespace WorkBoard.Domain.Enums;

/// <summary>What prompted a notification.</summary>
public enum NotificationType
{
    /// <summary>The recipient was assigned to a card.</summary>
    TaskAssigned = 0,

    /// <summary>Someone commented on a card the recipient is assigned to.</summary>
    TaskCommented = 1,

    /// <summary>A card assigned to the recipient is approaching its deadline.</summary>
    TaskDueSoon = 2,

    /// <summary>The recipient was mentioned in a comment.</summary>
    MentionedInComment = 3,

    /// <summary>The recipient was added to a workspace.</summary>
    AddedToWorkspace = 4,
}
