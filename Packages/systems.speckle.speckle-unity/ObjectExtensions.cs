using System.Linq;
using Speckle.Core.Models;

#nullable enable
namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// Extension methods for <see cref="Base"/> object models
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Sets a property dynamically, checking if an instance prop with the same name exists
        /// </summary>
        /// <param name="speckleObject"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void SetDetachedPropertyChecked(this Base speckleObject, string propertyName, object? value)
        {
            if(speckleObject.GetInstanceMembersNames().Any(name => name == propertyName))
                speckleObject[propertyName] = value;
            else
                speckleObject[$"@{propertyName}"] = value;
        }
    }
}
