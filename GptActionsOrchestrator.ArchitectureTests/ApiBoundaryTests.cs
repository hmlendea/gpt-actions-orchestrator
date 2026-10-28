using System;
using System.Linq;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

using NUnit.Framework;

using GptActionsOrchestrator.Api.Controllers;
using GptActionsOrchestrator.Service;

namespace GptActionsOrchestrator.ArchitectureTests
{
    [TestFixture]
    public sealed class ApiBoundaryTests
    {
        [Test]
        public static void ControllersDependOnOrchestrationInterfacesNotImplementations()
        {
            Type[] controllers = typeof(Program).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type)).ToArray();
            Assert.That(controllers, Is.Not.Empty, "No production controllers were discovered.");

            using (Assert.EnterMultipleScope())
            {
                foreach (Type controller in controllers)
                {
                    Assert.That(controller.Namespace, Is.EqualTo("GptActionsOrchestrator.Api.Controllers"));
                    Type[] dependencies = controller.GetConstructors()
                        .SelectMany(constructor => constructor.GetParameters())
                        .Select(parameter => parameter.ParameterType).ToArray();
                    Assert.That(dependencies, Does.Contain(typeof(IActionsOrchestrator)), controller.FullName);
                    Assert.That(dependencies.Where(type =>
                        type.Namespace is not null &&
                        type.Namespace.StartsWith("GptActionsOrchestrator.Service", StringComparison.Ordinal) &&
                        !type.IsInterface), Is.Empty, controller.FullName);
                    MethodInfo[] actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly).Where(method =>
                            method.GetCustomAttributes<HttpMethodAttribute>().Any()).ToArray();
                    Assert.That(actions, Is.Not.Empty, $"{controller.FullName}: No HTTP actions found.");
                    Assert.That(actions.Select(method => method.ReturnType),
                        Is.All.EqualTo(typeof(ActionResult)));
                }
            }
        }

        [Test]
        public static void ProductionDoesNotExposeInternalsToOtherAssemblies() =>
            Assert.That(typeof(Program).Assembly.GetCustomAttributesData()
                .Where(attribute => string.Equals(attribute.AttributeType.Name,
                    "InternalsVisibleToAttribute", StringComparison.Ordinal)), Is.Empty);

        [Test]
        public static void ControllerInventoryIsExplicit() =>
            Assert.That(typeof(ActionsController).IsSealed, Is.True);
    }
}
