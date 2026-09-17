using WorkBoard.Domain.Entities;

namespace WorkBoard.Domain.Enums;

/// <summary>What kind of change an <see cref="Activity"/> row records.</summary>
public enum ActivityType
{
    WorkspaceCreated = 0,
    MemberAdded = 1,
    MemberRoleChanged = 2,
    MemberRemoved = 3,
    ProjectCreated = 10,
    ProjectArchived = 11,
    TaskCreated = 20,
    TaskMoved = 21,
    TaskUpdated = 22,
    TaskAssigned = 23,
    TaskUnassigned = 24,
    TaskCommented = 25,
}
