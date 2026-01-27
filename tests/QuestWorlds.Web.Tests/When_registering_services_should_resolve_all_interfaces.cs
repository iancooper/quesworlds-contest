using Microsoft.Extensions.DependencyInjection;
using QuestWorlds.DiceRoller;
using QuestWorlds.Outcome;
using QuestWorlds.Resolution;
using QuestWorlds.Session;

namespace QuestWorlds.Web.Tests;

public class When_registering_services_should_resolve_all_interfaces
{
    [Fact]
    public void Should_resolve_ISessionCoordinator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSessionModule();

        // Act
        var provider = services.BuildServiceProvider();
        var coordinator = provider.GetService<ISessionCoordinator>();

        // Assert
        Assert.NotNull(coordinator);
    }

    [Fact]
    public void Should_resolve_IDiceRoller()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDiceRollerModule();

        // Act
        var provider = services.BuildServiceProvider();
        var roller = provider.GetService<IDiceRoller>();

        // Assert
        Assert.NotNull(roller);
    }

    [Fact]
    public void Should_resolve_IContestResolver()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddResolutionModule();

        // Act
        var provider = services.BuildServiceProvider();
        var resolver = provider.GetService<IContestResolver>();

        // Assert
        Assert.NotNull(resolver);
    }

    [Fact]
    public void Should_resolve_IOutcomeInterpreter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOutcomeModule();

        // Act
        var provider = services.BuildServiceProvider();
        var interpreter = provider.GetService<IOutcomeInterpreter>();

        // Assert
        Assert.NotNull(interpreter);
    }
}
