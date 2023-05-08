namespace AnakinRaW.AppUpdaterFramework.Commands.Factories;

internal interface IUpdateCommandsFactory
{
    ICommandDefinition CreateRestart();

    ICommandDefinition CreateElevate();
}