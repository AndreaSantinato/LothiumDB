using LothiumDB.Tools;

namespace LothiumDB;

internal interface IDatabase : IDisposable, IDatabaseConnection, IDatabaseTransaction, IDatabaseCommands, IDatabaseExtendedCommands
{ }