using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper.SuggestionHijack
{
    /// <summary>
    /// The hijack for Visual Studio 2026 and anything newer, where the inline completion internals
    /// are no longer where Visual Studio 2022 kept them: the whole <c>SuggestionService</c> moved
    /// out of <c>Microsoft.VisualStudio.IntelliCode</c> into
    /// <c>Microsoft.VisualStudio.Editor.Implementation</c>, the suggestion manager and the session
    /// became auto properties, the editor view now holds a <c>Task</c> of the completions instance
    /// rather than the instance, and <c>CacheProposal</c> grew a second parameter.
    ///
    /// So nothing here is looked up by a fixed name or a fixed shape. Every assembly, member and
    /// argument list is probed, and a member that cannot be found is reported to the Activity Log
    /// and turns the hijack off instead of throwing - which is what issue #75 was: a type that had
    /// moved took the whole class down through its type initializer.
    /// </summary>
    internal sealed class ProbingSuggestionHijack : ISuggestionHijack
    {
        /// <summary>
        /// The assemblies that have held the inline completion machinery, newest first: Visual
        /// Studio 2026 moved it into the editor implementation, before that it was IntelliCode's.
        /// </summary>
        private static readonly string[] CandidateAssemblyNames =
        [
            "Microsoft.VisualStudio.Editor.Implementation",
            "Microsoft.VisualStudio.IntelliCode",
        ];

        /// <summary>The editor's internal <c>InlineCompletionsInstance</c> type, whichever assembly it lives in.</summary>
        private readonly Type _completionsType;
        /// <summary>The non-public constructor of the nested <c>InlineCompletionSuggestion</c>, which takes the completions instance.</summary>
        private readonly ConstructorInfo _suggestionConstructor;
        /// <summary>The internal <c>CacheProposal</c> method; its parameter list differs between IDE versions.</summary>
        private readonly MethodInfo _cacheProposalMethod;
        /// <summary>The <c>TryDisplaySuggestionAsync</c> method of the suggestion manager, the call that actually puts ghost text on screen.</summary>
        private readonly MethodInfo _tryDisplaySuggestionAsyncMethod;
        /// <summary>Read/write access to the completions instance's current suggestion session, field or property.</summary>
        private readonly ReflectedMember _session;
        /// <summary>Read access to the completions instance's suggestion manager, field or property.</summary>
        private readonly ReflectedMember _suggestionManager;

        /// <summary>
        /// Probes the running IDE for everything the hijack needs and returns <c>null</c> when
        /// anything is missing, naming the missing piece in the Activity Log. A null result means
        /// whole line suggestions are unavailable, not that the extension is broken.
        /// </summary>
        internal static ProbingSuggestionHijack TryCreate()
        {
            try
            {
                var completionsType = FindCompletionsType();
                if (completionsType is null)
                {
                    ReportMissing("the InlineCompletionsInstance type");
                    return null;
                }

                var suggestionType = FindNestedType(completionsType, "InlineCompletionSuggestion");
                var suggestionConstructor = suggestionType?
                    .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(c => c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType.IsAssignableFrom(completionsType));
                if (suggestionConstructor is null)
                {
                    ReportMissing("the InlineCompletionSuggestion constructor");
                    return null;
                }

                var cacheProposalMethod = completionsType.GetMethod("CacheProposal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (cacheProposalMethod is null)
                {
                    ReportMissing("InlineCompletionsInstance.CacheProposal");
                    return null;
                }

                var session = ReflectedMember.Find(completionsType, "Session");
                if (session is null)
                {
                    ReportMissing("InlineCompletionsInstance.Session");
                    return null;
                }

                var suggestionManager = ReflectedMember.Find(completionsType, "SuggestionManager");
                if (suggestionManager is null)
                {
                    ReportMissing("InlineCompletionsInstance.SuggestionManager");
                    return null;
                }

                var tryDisplaySuggestionAsyncMethod = suggestionManager.MemberType.GetMethod("TryDisplaySuggestionAsync");
                if (tryDisplaySuggestionAsyncMethod is null)
                {
                    ReportMissing("SuggestionManagerBase.TryDisplaySuggestionAsync");
                    return null;
                }

                return new ProbingSuggestionHijack(
                    completionsType,
                    suggestionConstructor,
                    cacheProposalMethod,
                    tryDisplaySuggestionAsyncMethod,
                    session,
                    suggestionManager
                    );
            }
            catch (Exception excp)
            {
                excp.ActivityLogException(
                    "FreeAIr cannot reach the editor's inline completion internals; whole line suggestions will not be shown."
                    );
                return null;
            }
        }

        private ProbingSuggestionHijack(
            Type completionsType,
            ConstructorInfo suggestionConstructor,
            MethodInfo cacheProposalMethod,
            MethodInfo tryDisplaySuggestionAsyncMethod,
            ReflectedMember session,
            ReflectedMember suggestionManager
            )
        {
            _completionsType = completionsType;
            _suggestionConstructor = suggestionConstructor;
            _cacheProposalMethod = cacheProposalMethod;
            _tryDisplaySuggestionAsyncMethod = tryDisplaySuggestionAsyncMethod;
            _session = session;
            _suggestionManager = suggestionManager;
        }

        /// <summary>
        /// Displays a FreeAIr-generated completion as ghost text, by making the editor's own
        /// inline completion instance believe the suggestion is its own: an existing session is
        /// dismissed, a suggestion is constructed over that instance, the suggestion manager is
        /// asked to display it, and the resulting session is written back where the editor looks
        /// for it before the proposal is finally drawn.
        /// </summary>
        public async Task ShowAutocompleteAsync(
            ITextView textView,
            ProposalCollectionBase proposalCollection
            )
        {
            var proposal = proposalCollection.Proposals.FirstOrDefault();
            if (proposal is null)
            {
                return;
            }

            try
            {
                var completionsInstance = await GetCompletionsInstanceAsync(textView);
                if (completionsInstance is null)
                {
                    ActivityLogHelper.ActivityLogWarning(
                        "FreeAIr found no inline completion instance on this editor view; whole line suggestions need the editor's own inline completions switched on."
                        );
                    return;
                }

                //an already displayed suggestion owns the ghost text, so it has to go first
                if (_session.GetValue(completionsInstance) is SuggestionSessionBase displayedSession)
                {
                    await displayedSession.DismissAsync(ReasonForDismiss.DismissedDueToInvalidProposal, CancellationToken.None);
                }

                var suggestionManagerInstance = _suggestionManager.GetValue(completionsInstance);
                if (suggestionManagerInstance is null)
                {
                    ActivityLogHelper.ActivityLogWarning(
                        "FreeAIr found no suggestion manager on this editor view; whole line suggestions need the editor's own inline completions switched on."
                        );
                    return;
                }

                var suggestion = _suggestionConstructor.Invoke([completionsInstance]);

                var newSession = await (Task<SuggestionSessionBase>)_tryDisplaySuggestionAsyncMethod.Invoke(
                    suggestionManagerInstance,
                    BuildArguments(_tryDisplaySuggestionAsyncMethod.GetParameters(), suggestion)
                    );
                if (newSession is null)
                {
                    return;
                }

                _cacheProposalMethod.Invoke(completionsInstance, BuildArguments(_cacheProposalMethod.GetParameters(), proposal));
                _session.SetValue(completionsInstance, newSession);
                await newSession.DisplayProposalAsync(proposal, CancellationToken.None);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Digs the editor's inline completion instance out of the view's property bag. Visual
        /// Studio 2022 stores the instance under its own type; Visual Studio 2026 creates it
        /// asynchronously and stores a <c>Task</c> of it under the task's type, so a task found
        /// here is awaited rather than skipped - the view has only just been opened when that
        /// matters.
        /// </summary>
        private async Task<object> GetCompletionsInstanceAsync(
            ITextView textView
            )
        {
            object candidate = null;
            foreach (var property in textView.Properties.PropertyList)
            {
                if (property.Key is not Type key)
                {
                    continue;
                }

                if (IsCompletionsType(key)
                    || (key.IsGenericType && key.GetGenericArguments().Any(IsCompletionsType)))
                {
                    candidate = property.Value;
                    break;
                }
            }

            if (candidate is Task pendingInstance)
            {
                await pendingInstance;
                candidate = pendingInstance.GetType().GetProperty("Result")?.GetValue(pendingInstance);
            }

            return candidate;
        }

        /// <summary>
        /// Whether a property bag key names the completions instance. The name is compared as well
        /// as the type itself, so a second copy of the type loaded from another assembly still
        /// matches.
        /// </summary>
        private bool IsCompletionsType(
            Type type
            )
        {
            return type == _completionsType || type.Name == _completionsType.Name;
        }

        /// <summary>
        /// Builds an argument array for a reflected call whose first argument is known and whose
        /// remaining parameters are to be left at their defaults. This is what survives
        /// <c>CacheProposal</c> gaining a <c>forceCache</c> flag and the cancellation token of
        /// <c>TryDisplaySuggestionAsync</c>: the count comes from the method, not from this code.
        /// </summary>
        private static object[] BuildArguments(
            ParameterInfo[] parameters,
            object firstArgument
            )
        {
            if (parameters.Length == 0)
            {
                return [];
            }

            var arguments = new object[parameters.Length];
            arguments[0] = firstArgument;
            for (var i = 1; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.HasDefaultValue)
                {
                    arguments[i] = parameter.DefaultValue;
                }
                else
                {
                    arguments[i] = parameter.ParameterType.IsValueType
                        ? Activator.CreateInstance(parameter.ParameterType)
                        : null;
                }
            }

            return arguments;
        }

        /// <summary>
        /// Finds <c>InlineCompletionsInstance</c> in whichever of the known assemblies is hosting
        /// it, preferring one already loaded into the IDE over a fresh <see cref="Assembly.Load"/>.
        /// </summary>
        private static Type FindCompletionsType()
        {
            foreach (var assembly in EnumerateCandidateAssemblies())
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type.Name == "InlineCompletionsInstance")
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// The nested helper type by name. It has always been nested inside
        /// <c>InlineCompletionsInstance</c>, but the fallback scans the whole assembly in case a
        /// later release lifts it out.
        /// </summary>
        private static Type FindNestedType(
            Type completionsType,
            string name
            )
        {
            var nested = completionsType.GetNestedType(name, BindingFlags.Public | BindingFlags.NonPublic);
            if (nested is not null)
            {
                return nested;
            }

            return SafeGetTypes(completionsType.Assembly).FirstOrDefault(t => t.Name == name);
        }

        private static IEnumerable<Assembly> EnumerateCandidateAssemblies()
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var name in CandidateAssemblyNames)
            {
                var alreadyLoaded = loaded.FirstOrDefault(
                    a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase)
                    );
                if (alreadyLoaded is not null)
                {
                    yield return alreadyLoaded;
                    continue;
                }

                Assembly justLoaded;
                try
                {
                    justLoaded = Assembly.Load(name);
                }
                catch
                {
                    //an IDE which never shipped this assembly is not an error, it is the next candidate
                    continue;
                }

                if (justLoaded is not null)
                {
                    yield return justLoaded;
                }
            }
        }

        /// <summary>
        /// The types of an assembly, keeping the ones which did load when some of them did not -
        /// the editor implementation assembly references plenty that a reflection walk cannot
        /// resolve, and a single one of those must not hide the type being looked for.
        /// </summary>
        private static IEnumerable<Type> SafeGetTypes(
            Assembly assembly
            )
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException excp)
            {
                return excp.Types.Where(t => t is not null);
            }
            catch
            {
                return [];
            }
        }

        private static void ReportMissing(
            string what
            )
        {
            ActivityLogHelper.ActivityLogWarning(
                $"FreeAIr cannot find {what} in this Visual Studio; whole line suggestions will not be shown."
                );
        }

        /// <summary>
        /// A field or a property addressed the same way. Visual Studio 2022 exposed the suggestion
        /// manager and the session as internal fields and Visual Studio 2026 turned them into auto
        /// properties, which renames the field to a compiler generated backing field - so the
        /// lookup has to try both, and both spellings of a hand written field.
        /// </summary>
        private sealed class ReflectedMember
        {
            private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            private readonly FieldInfo _field;
            private readonly PropertyInfo _property;

            /// <summary>The declared type of the member, used to look methods up on it.</summary>
            public Type MemberType => _field is not null ? _field.FieldType : _property.PropertyType;

            private ReflectedMember(FieldInfo field, PropertyInfo property)
            {
                _field = field;
                _property = property;
            }

            /// <summary>
            /// Looks for the member under every spelling seen so far: the plain field, the auto
            /// property's backing field, an underscore-prefixed private field, and finally the
            /// property itself. The backing field is preferred over the property because these
            /// properties have private setters and writing the field skips the accessor entirely.
            /// </summary>
            public static ReflectedMember Find(
                Type type,
                string name
                )
            {
                var candidateFieldNames = new[]
                {
                    name,
                    $"<{name}>k__BackingField",
                    "_" + char.ToLowerInvariant(name[0]) + name.Substring(1),
                };

                foreach (var candidate in candidateFieldNames)
                {
                    var field = type.GetField(candidate, MemberFlags);
                    if (field is not null)
                    {
                        return new ReflectedMember(field, null);
                    }
                }

                var property = type.GetProperty(name, MemberFlags);
                if (property is not null)
                {
                    return new ReflectedMember(null, property);
                }

                return null;
            }

            public object GetValue(object instance)
            {
                return _field is not null
                    ? _field.GetValue(instance)
                    : _property.GetValue(instance);
            }

            public void SetValue(object instance, object value)
            {
                if (_field is not null)
                {
                    _field.SetValue(instance, value);
                }
                else
                {
                    _property.SetValue(instance, value);
                }
            }
        }
    }
}
