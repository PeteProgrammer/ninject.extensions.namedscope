//-------------------------------------------------------------------------------
// <copyright file="NamedScopeExtensionMethods.cs" company="bbv Software Services AG">
//   Copyright (c) 2010 bbv Software Services AG
//   Author: Remo Gloor remo.gloor@bbv.ch
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//-------------------------------------------------------------------------------

namespace Ninject.Extensions.NamedScope
{
    using System.Linq;
    using Ninject.Activation;
    using Ninject.Syntax;

    /// <summary>
    /// Extension methods for the named scope module.
    /// </summary>
    public static class NamedScopeExtensionMethods
    {
        /// <summary>
        /// Defines that a binding is in a named scope.
        /// </summary>
        /// <typeparam name="T">The type of the binding.</typeparam>
        /// <param name="syntax">The In syntax.</param>
        /// <param name="scopeParameterName">Name of the scope parameter.</param>
        /// <returns>The Named syntax.</returns>
        public static IBindingNamedWithOrOnSyntax<T> InNamedScope<T>(this IBindingInSyntax<T> syntax, string scopeParameterName)
        {
            return syntax.InScope(context => GetScope(context, scopeParameterName));
        }

        /// <summary>
        /// Defines that a binding is in the scope of its target.
        /// </summary>
        /// <typeparam name="T">The type of the binding.</typeparam>
        /// <param name="syntax">The In syntax.</param>
        /// <returns>The Named syntax.</returns>
        public static IBindingNamedWithOrOnSyntax<T> InParentScope<T>(this IBindingInSyntax<T> syntax)
        {
            return syntax.InScope(
                context =>
                    {
                        const string ScopeParameterName = "NamedScopeInParentScope";
                        var parentContext = context.Request.ParentContext;
                        var namedScopeParameter = GetNamedScopeParameter(parentContext, ScopeParameterName);
                        if (namedScopeParameter == null)
                        {
                            namedScopeParameter = new NamedScopeParameter(ScopeParameterName);
                            parentContext.Parameters.Add(namedScopeParameter);
                        }

                        return namedScopeParameter.Scope;
                    });
        }

        /// <summary>
        /// Defines the a binding defines a named scope.
        /// </summary>
        /// <typeparam name="T">The type of the binding.</typeparam>
        /// <param name="syntax">The syntax.</param>
        /// <param name="scopeName">The name of the scope.</param>
        public static void DefinesNamedScope<T>(this IBindingOnSyntax<T> syntax, string scopeName)
        {
            var callback = syntax.Binding.ProviderCallback;
            syntax.Binding.ProviderCallback =
                context =>
                    {
                        context.Parameters.Add(new NamedScopeParameter(scopeName));
                        return callback(context);
                    };
        }

        /// <summary>
        /// Gets a named scope from the request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="scopeParameterName">Name of the scope parameter.</param>
        /// <returns>The scope.</returns>
        private static object GetScope(IContext context, string scopeParameterName)
        {
            NamedScopeParameter namedScopeParameter = GetNamedScopeParameter(context, scopeParameterName);
            if (namedScopeParameter != null)
            {
                if (namedScopeParameter.Scope.IsDisposed)
                {
                    throw new ScopeDisposedException();
                }

                return namedScopeParameter.Scope;
            }

            if (context.Request.ParentContext != null)
            {
                return GetScope(context.Request.ParentContext, scopeParameterName);
            }

            throw new UnknownScopeException(scopeParameterName);
        }

        /// <summary>
        /// Gets a named scope parameter from a context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="scopeParameterName">Name of the scope parameter.</param>
        /// <returns>The requested parameter of null if it is not found.</returns>
        private static NamedScopeParameter GetNamedScopeParameter(IContext context, string scopeParameterName)
        {
            return context.Parameters.OfType<NamedScopeParameter>().SingleOrDefault(parameter => parameter.Name == scopeParameterName);
        }
    }
}