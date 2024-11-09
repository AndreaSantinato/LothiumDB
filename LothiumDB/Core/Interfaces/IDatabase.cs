using LothiumDB.Tools;

namespace LothiumDB.Core.Interfaces;

internal interface IDatabase : IDisposable, IDatabaseConnection, IDatabaseTransaction, IDatabaseCommands, IDatabaseExtendedCommands
{ }