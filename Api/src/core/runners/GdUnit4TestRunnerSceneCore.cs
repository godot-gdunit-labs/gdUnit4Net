// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Runners;

using System.ComponentModel;

/// <summary>
///     Compatibility type for runner scenes installed by older versions, it is not used by the test runner.
/// </summary>
/// <remarks>
///     Up to 5.1.0-rc6 the test adapter installed <c>gdunit4_testadapter_v5/GdUnit4TestRunnerScene.cs</c> into the test project,
///     its runner scene derives from this type, which was a <c>SceneTree</c> compiled into this assembly.
///     From Godot 4.8 on, the source generators of the test project call generated members of such a base type
///     that an assembly built for an older Godot version does not have, so the test project fails to compile.
///     <para>
///         The current runner scene derives from <c>SceneTree</c> and calls <see cref="GdUnit4TestRunnerHost.Initialize" />.
///         This type stays as a plain class only to not break existing projects: an outdated runner scene still compiles,
///         and the next test run detects that it differs from the current runner scene and replaces it.
///     </para>
///     <para>
///         Remove this type in the next major version.
///     </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class GdUnit4TestRunnerSceneCore;
