namespace Bangaliyana
{
    /// <summary>
    /// Marker class for shared localization resources.
    /// MUST be in root namespace (Bangaliyana) because:
    /// ASP.NET Core with ResourcesPath="Resources" calculates resource path as:
    /// {RootNamespace}.{ResourcesPath}.{TypeName} = Bangaliyana.Resources.SharedResources
    /// If class is in Bangaliyana.Resources namespace, it becomes:
    /// Bangaliyana.Resources.Resources.SharedResources (WRONG - double Resources!)
    /// </summary>
    public class SharedResources
    {
    }
}
