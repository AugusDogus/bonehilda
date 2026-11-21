using System.Security.Permissions;

// Publicized references expose members such as BaseAI.MoveTo. Match JotunnModExample's
// Mono access policy without changing the user's game assemblies:
// https://github.com/Valheim-Modding/JotunnModExample/blob/master/JotunnModExample/Properties/IgnoreAccessModifiers.cs
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
