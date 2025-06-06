global using NitroxModel.Logger;
using System;
using System.Reflection;
using Autofac;
using NitroxModel.Core;
using NitroxModel.Networking;
using NitroxServer.Communication.LiteNetLib;
using NitroxServer.Communication.Packets;
using NitroxServer.Communication.Packets.Processors;
using NitroxServer.Communication.Packets.Processors.Abstract;
using NitroxServer.ConsoleCommands.Abstract;
using NitroxServer.ConsoleCommands.Processor;
using NitroxServer.Serialization.Upgrade;
using NitroxServer.Serialization.World;

namespace NitroxServer
{
    public class ServerAutoFacRegistrar : IAutoFacRegistrar
    {
        public virtual void RegisterDependencies(ContainerBuilder containerBuilder)
        {
            RegisterCoreDependencies(containerBuilder);
            RegisterWorld(containerBuilder);

            RegisterGameSpecificServices(containerBuilder, Assembly.GetCallingAssembly());
            RegisterGameSpecificServices(containerBuilder, Assembly.GetExecutingAssembly());
        }

        private static void RegisterCoreDependencies(ContainerBuilder containerBuilder)
        {
            // TODO: Remove this once .NET Generic Host is implemented
            containerBuilder.Register(c => Server.CreateOrLoadConfig()).SingleInstance();
            containerBuilder.RegisterType<Server>().SingleInstance();
            containerBuilder.RegisterType<DefaultServerPacketProcessor>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<PacketHandler>().InstancePerLifetimeScope();
            containerBuilder.RegisterType<ConsoleCommandProcessor>().SingleInstance();

            containerBuilder.RegisterType<LiteNetLibServer>()
                            .As<Communication.NitroxServer>()
                            .SingleInstance();

            containerBuilder.RegisterType<NtpSyncer>().SingleInstance();
        }

        private void RegisterWorld(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<WorldPersistence>().SingleInstance();

            // TODO: Remove this once .NET Generic Host is implemented
            containerBuilder.Register(c => c.Resolve<WorldPersistence>().Load(Server.GetSaveName(Environment.GetCommandLineArgs(), "My World"))).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().BuildingManager).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().TimeKeeper).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().PlayerManager).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().StoryManager).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().ScheduleKeeper).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().SimulationOwnershipData).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().WorldEntityManager).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().EntityRegistry).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().EntitySimulation).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().EscapePodManager).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().BatchEntitySpawner).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().GameData).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().GameData.PDAState).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().GameData.StoryGoals).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().GameData.StoryTiming).SingleInstance();
            containerBuilder.Register(c => c.Resolve<World>().SessionSettings).SingleInstance();
        }

        private void RegisterGameSpecificServices(ContainerBuilder containerBuilder, Assembly assembly)
        {
            containerBuilder
                .RegisterAssemblyTypes(assembly)
                .AssignableTo<Command>()
                .As<Command>()
                .InstancePerLifetimeScope();

            containerBuilder
                .RegisterAssemblyTypes(assembly)
                .AsClosedTypesOf(typeof(AuthenticatedPacketProcessor<>))
                .InstancePerLifetimeScope();

            containerBuilder
                .RegisterAssemblyTypes(assembly)
                .AsClosedTypesOf(typeof(UnauthenticatedPacketProcessor<>))
                .InstancePerLifetimeScope();

            containerBuilder
                .RegisterAssemblyTypes(assembly)
                .AssignableTo<SaveDataUpgrade>()
                .As<SaveDataUpgrade>()
                .InstancePerLifetimeScope();
        }
    }
}
