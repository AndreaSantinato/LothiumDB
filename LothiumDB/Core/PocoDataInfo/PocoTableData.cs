namespace LothiumDB.Core.PocoDataInfo;

/// <summary>
/// Define the info of an object associated database's table
/// </summary>
internal class PocoTableData
{
    /// <summary>
    /// Contains the name of the poco class
    /// </summary>
    public string? PocoObjectClassName { get; init; }

    /// <summary>
    /// Contains the name of the table
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Contains the schema of the table
    /// </summary>
    public string? TableSchema { get; init; }

    /// <summary>
    /// Contains the combined value of table's schema and table's name
    /// </summary>
    public string TableFullName
    {
        get 
        {
            var tbSchema = this.TableSchema;
            
            var tbName = (string.IsNullOrEmpty(this.TableName))
                ? this.PocoObjectClassName
                : this.TableName;

            var tbFullName = (!string.IsNullOrEmpty(this.TableSchema))
                ? $"{this.TableSchema}.{this.TableName}"
                : $"{this.TableName}";

            return tbFullName;
        }
    } 
}