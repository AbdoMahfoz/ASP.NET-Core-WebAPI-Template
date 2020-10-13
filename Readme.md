# Purpose

This is an asp.net core web api jump starter template with a bunch of useful features such as
* JWT Authentication 
* Generic Exception Handling
* Generic Roles And Permissions System
* Swagger Documentation
* Action Filters Validation
* Email Sending Service
* File Logger
* Unit Tests Project
* Self Explanatory error messeges 

## Usage
Feel free to initialize a new repo with the template by clicking the "Use This Template" Button
Or 
To download the Solution and start coding on the fly 

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[MIT](https://choosealicense.com/licenses/mit/)

# Role System
The template includes a custom-made role system simplifies the process of setting roles and permissions.
It allows you to assign roles/permissions
 - **Dynamically**, using the provided endpoints.
 - **Statically**, using the provided attributes.
	- `HasRole`/`HasPermission` attributes can be decorated on any controller/action to forbid acces to it outside of the given roles.
	
Roles can have a variable amount of permissions. Users can be assigned roles or permissions or both. If a user is assigned a role that has a permission, permission A for example, and a certain endpoint requires this permission, the system will automatically deduce that this user has access because the role of the user includes the required permission. endpoints can also require roles if so desired.

## Examples
### Using attributes to statically define roles/permissions
```cs
[HasRole("Admin")]
[HasPermission("dangerousPermission")]
public IActionResult DoSomethingExtremelyDangerous(string dangerousCommand)
{
    ....
}
```
### Using endpoints to dynamically set roles/permissions
 - refer to swagger for the full documentation of endpoints and the example values

## Notes
 - Users can only be assigned roles/permissions through endpoints defined in the swagger documentation
 - Make sure that the `RolesAndPermissions` controller is guarded with strict roles/permissions. You don't want your users missing around with that.

