﻿using System;

namespace Frosty.Core.Attributes
{
    /// <summary>
    /// This attribute registers a custom profile to the plugin system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class RegisterProfileAttribute : Attribute
    {
        /// <summary>
        /// Gets the type to use to construct the profile. Must implement the IProfile interface
        /// </summary>
        /// <returns>The type to use to construct the profile</returns>
        public Type ProfileType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterProfileAttribute"/> class with the profile type.
        /// </summary>
        public RegisterProfileAttribute(Type type)
        {
            ProfileType = type;
        }
    }
}
