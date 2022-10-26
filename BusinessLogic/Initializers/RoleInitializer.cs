using System.Collections.Generic;
using Repository.ExtendedRepositories;
using System.Linq;
using Models.DataModels;
using BusinessLogic.Interfaces;
using Models;
using Models.Helpers;
using Services.DTOs;
using Services.RoleSystem;

namespace BusinessLogic.Initializers;

// ReSharper disable once UnusedType.Global
public class RoleInitializer : BaseInitializer
{
    private readonly IRolesRepository _roleRepository;
    private readonly IPermissionsRepository _permissionsRepository;
    private readonly IAccountLogic _accountLogic;
    private readonly IUserRepository _userRepository;

    private readonly List<(RoleNames, List<PermissionNames>)> _rolesPermissions = new()
    {
        (RoleNames.Admin,
            new List<PermissionNames>
            {
                PermissionNames.CreateAccount, PermissionNames.GenerateReports,
                PermissionNames.ManageRoles, PermissionNames.ViewAnalytics
            })
    };

    public RoleInitializer(IRolesRepository roleRepository, IPermissionsRepository permissionsRepository,
        IAccountLogic accountLogic, IUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _permissionsRepository = permissionsRepository;
        _accountLogic = accountLogic;
        _userRepository = userRepository;
    }

    protected override void Initialize()
    {
        var modelVerbs = typeof(CrudVerb).GetEnumNames();

        var currentPermissions = IncludedEntities.Types
            .SelectMany(u => modelVerbs.Select(x => $"{x} {u.Item1.Name}"))
            .Concat(typeof(PermissionNames).GetEnumNames()).ToArray();
        var existingPermissions = _permissionsRepository.GetAll().Select(u => u.Name).ToArray();
        var missingPermissions = currentPermissions.Where(u => !existingPermissions.Contains(u)).ToArray();
        if (missingPermissions.Length > 0)
        {
            _permissionsRepository.InsertRange(missingPermissions.Select(u => new Permission { Name = u }));
        }

        if (!_roleRepository.GetAll().Any())
        {
            var admin = new Role { Name = RoleNames.Admin.ToString() };
            _roleRepository.Insert(admin).Wait();

            var dataManager = new Role { Name = RoleNames.DataManager.ToString() };
            _roleRepository.Insert(dataManager).Wait();

            var permissions = new[] { "Read *", "Create *", "Update *", "Delete *" };
            foreach (var permission in permissions)
            {
                var permObj = new Permission { Name = permission };
                _permissionsRepository.Insert(permObj).Wait();
                _permissionsRepository.AssignPermissionToRole(permObj.Id, dataManager.Id);
                _permissionsRepository.AssignPermissionToRole(permObj.Id, admin.Id);
            }
        }

        var currentRoles = typeof(RoleNames).GetEnumNames();
        var existingRoles = _roleRepository.GetAll().Select(u => u.Name).ToArray();
        var missingRoles = currentRoles.Where(u => !existingRoles.Contains(u)).ToArray();
        if (missingRoles.Length > 0)
        {
            _roleRepository.InsertRange(missingRoles.Select(u => new Role { Name = u }));
        }

        foreach (var permission in missingPermissions)
        {
            _permissionsRepository.AssignPermissionToRole(permission, RoleNames.Admin.ToString());
        }

        foreach (var rolePermissionPair in _rolesPermissions)
        {
            var curPermissions = _permissionsRepository.GetPermissionsOfRole(rolePermissionPair.Item1.ToString());
            var desiredPermissions = rolePermissionPair.Item2.Select(u => u.ToString()).ToArray();
            var toBeAdded = desiredPermissions.Where(u => !curPermissions.Any(x => x.Name == u));
            var toBeRemoved = curPermissions.Where(u => !desiredPermissions.Contains(u.Name));
            foreach (var permission in toBeAdded)
            {
                var p = _permissionsRepository.GetPermission(permission);
                if (p == null)
                {
                    p = new Permission { Name = permission };
                    _permissionsRepository.Insert(p).Wait();
                }

                _permissionsRepository.AssignPermissionToRole(permission, rolePermissionPair.Item1.ToString());
            }

            if (toBeRemoved.Any())
            {
                _permissionsRepository.HardDeleteRange(toBeRemoved);
            }
        }

        if (_userRepository.GetAll().Any()) return;
        _accountLogic.Register(new UserAuthenticationRequest { Username = "Admin", Password = "123123123" },
            "Admin");
    }
}