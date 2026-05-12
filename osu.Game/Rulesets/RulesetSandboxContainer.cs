// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Game.Rulesets
{
    internal partial class RulesetSandboxContainer : Container
    {
        private readonly HashSet<Type> allowedDependencies;

        public RulesetSandboxContainer(params Type[] allowedDependencies)
        {
            this.allowedDependencies = allowedDependencies.ToHashSet();
        }

        protected override void Update()
        {
            base.Update();

            // this attempts to - and utterly fails in trying to - synchronise the child's sizing and ours
            // so that this container *can* be transparently used as a wrapper instead of a roadblock all the time
            Child.Size = Size;
            Child.RelativeSizeAxes = RelativeSizeAxes;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            // a good question might be: "why is this so convoluted????"
            // the answer is that while game-side you can implement `IReadOnlyDependencyContainer.Get()` to suppress dependency resolutions,
            // the one that *actually* matters is `Inject<T>` because that's the one that'll get invoked via BDL.
            // the problem with *that* one is that you:
            // - can't call the parent's `.Inject()` method, because it won't suppress the resolutions you want it to
            // - can't reimplement it yourself, because `DependencyActivator` is internal framework-side
            return new DependencyContainer(new NoDependenciesContainer(parent, allowedDependencies));
        }

        private class NoDependenciesContainer : IReadOnlyDependencyContainer
        {
            private readonly IReadOnlyDependencyContainer parent;
            private readonly HashSet<Type> allowedDependencies;

            public NoDependenciesContainer(IReadOnlyDependencyContainer parent, HashSet<Type> allowedDependencies)
            {
                this.parent = parent;
                this.allowedDependencies = allowedDependencies;
            }

            public object? Get(Type type)
                => allowedDependencies.Contains(type) ? parent.Get(type) : null;

            public object? Get(Type type, CacheInfo info)
                => allowedDependencies.Contains(type) ? parent.Get(type, info) : null;

            public void Inject<T>(T instance) where T : class, IDependencyInjectionCandidate
            {
                // this being a no-op doesn't matter for reasons mentioned a few lines above.
            }
        }
    }

    public static class RulesetSandboxExtensions
    {
        public static Drawable AsSandboxed(this Drawable drawable, params Type[] allowedDependencies)
        {
            return new RulesetSandboxContainer(allowedDependencies) { Child = drawable };
        }
    }
}
