using PilotApp.Models;

namespace PilotApp.Services;

public static class PermissionService
{
    public static bool CanCreateTask(UserRole role) => role is UserRole.Admin or UserRole.Manager;
    public static bool CanDeleteTask(UserRole role) => role is UserRole.Admin;
    public static bool CanEditTask(UserRole role) => role is UserRole.Admin or UserRole.Manager;

    public static bool CanCreateDocument(UserRole role) => role is UserRole.Admin or UserRole.Manager;
    public static bool CanDeleteDocument(UserRole role) => role is UserRole.Admin;
    public static bool CanEditDocument(UserRole role) => role is UserRole.Admin or UserRole.Manager;

    public static bool CanCreateTimeEntry(UserRole role) => role != UserRole.User || true;
    public static bool CanDeleteTimeEntry(UserRole role) => role is UserRole.Admin;

    public static bool CanViewAuditLog(UserRole role) => role is UserRole.Admin;
    public static bool CanManageUsers(UserRole role) => role is UserRole.Admin;
}