using System.Collections.Generic;
using Repository.ExtendedRepositories;
using System.Linq;
using Models.DataModels;
using BusinessLogic.Interfaces;
using Models;
using Models.DataModels.RoleSystem;
using Models.Helpers;
using Repository.ExtendedRepositories.RoleSystem;
using Services.DTOs;
using Services.RoleSystem;

namespace BusinessLogic.Initializers;

// ReSharper disable once UnusedType.Global
public class RoleInitializer(
    IRolesRepository roleRepository,
    IPermissionsRepository permissionsRepository,
    IAccountLogic accountLogic,
    IUserRepository userRepository)
    : BaseInitializer
{
    private readonly List<(RoleNames, List<PermissionNames>)> _rolesPermissions =
    [
        (RoleNames.Admin,
        [
            PermissionNames.CreateAccount, PermissionNames.GenerateReports,
            PermissionNames.ManageRoles, PermissionNames.ViewAnalytics
        ])
    ];

    protected override void Initialize()
    {
        var modelVerbs = typeof(CrudVerb).GetEnumNames();

        var currentPermissions = IncludedEntities.Types
            .SelectMany(u => modelVerbs.Select(x => $"{x} {u.Item1.Name}"))
            .Concat(typeof(PermissionNames).GetEnumNames()).ToArray();
        var existingPermissions = permissionsRepository.GetAll().Select(u => u.Name).ToArray();
        var missingPermissions = currentPermissions.Where(u => !existingPermissions.Contains(u)).ToArray();
        if (missingPermissions.Length > 0)
        {
            permissionsRepository.InsertRange(missingPermissions.Select(u => new Permission { Name = u })).Wait();
        }

        if (!roleRepository.GetAll().Any())
        {
            var admin = new Role { Name = RoleNames.Admin.ToString() };
            roleRepository.Insert(admin).Wait();

            var dataManager = new Role { Name = RoleNames.DataManager.ToString() };
            roleRepository.Insert(dataManager).Wait();

            var permissions = new[] { "Read *", "Create *", "Update *", "Delete *" };
            foreach (var permission in permissions)
            {
                var permObj = new Permission { Name = permission };
                permissionsRepository.Insert(permObj).Wait();
                permissionsRepository.AssignPermissionToRole(permObj.Id, dataManager.Id);
                permissionsRepository.AssignPermissionToRole(permObj.Id, admin.Id);
            }
        }

        var currentRoles = typeof(RoleNames).GetEnumNames();
        var existingRoles = roleRepository.GetAll().Select(u => u.Name).ToArray();
        var missingRoles = currentRoles.Where(u => !existingRoles.Contains(u)).ToArray();
        if (missingRoles.Length > 0)
        {
            roleRepository.InsertRange(missingRoles.Select(u => new Role { Name = u })).Wait();
        }

        foreach (var permission in missingPermissions)
        {
            permissionsRepository.AssignPermissionToRole(permission, RoleNames.Admin.ToString());
        }

        foreach (var rolePermissionPair in _rolesPermissions)
        {
            var curPermissions = permissionsRepository.GetPermissionsOfRole(rolePermissionPair.Item1.ToString());
            var desiredPermissions = rolePermissionPair.Item2.Select(u => u.ToString()).ToArray();
            var toBeAdded = desiredPermissions.Where(u => !curPermissions.Any(x => x.Name == u));
            var toBeRemoved = curPermissions.Where(u => !desiredPermissions.Contains(u.Name));
            foreach (var permission in toBeAdded)
            {
                var p = permissionsRepository.GetPermission(permission);
                if (p == null)
                {
                    p = new Permission { Name = permission };
                    permissionsRepository.Insert(p).Wait();
                }

                permissionsRepository.AssignPermissionToRole(permission, rolePermissionPair.Item1.ToString());
            }

            if (toBeRemoved.Any())
            {
                permissionsRepository.HardDeleteRange(toBeRemoved).Wait();
            }
        }

        if (userRepository.GetAll().Any()) return;
        accountLogic.Register(new UserAuthenticationRequest { Username = "Admin", Password = "123123123" },
            "Admin");
    }
}