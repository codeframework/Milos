using Milos.Data;

// TODO: I (Markus) honestly do not know what this is or why we need it. Perhaps this was something special for a specific customer?

namespace Milos.BusinessObjects;

public interface IDeletionDependency
{
    string TableName { get; set; }
    string PrimaryKeyField { get; set; }
    string ForeignKeyField { get; set; }
    string ColumnList { get; set; }
    string OrderBy { get; set; }
    string ViolationMessage { get; set; }
    Guid CurrentTopPrimaryKeyValue { get; }
    IDeletionDependency Parent { get; }
    IList<IDeletionDependency> Dependencies { get; }
    bool Restrict { get; set; }
    IDataService DataService { get; set; }
    DataTable CurrentDependentRowsTable { get; set; }
    Dependency AddDependency();
    string BuildQuery(QueryType queryType);
    IDbCommand GetCommand(QueryType queryType);
}

public class Dependency(IDeletionDependency parent, Guid topPrimaryKey) : IDeletionDependency
{
    public DataTable CurrentDependentRowsTable { get; set; }

    public IDataService DataService
    {
        get => Parent.DataService;
        set => Parent.DataService = value;
    }

    public string PrimaryKeyField { get; set; }
    public string ForeignKeyField { get; set; }
    public string TableName { get; set; }
    public string ColumnList { get; set; }
    public string OrderBy { get; set; }
    public string ViolationMessage { get; set; }

    public Dependency AddDependency()
    {
        var dependency = new Dependency(this, CurrentTopPrimaryKeyValue);
        Dependencies.Add(dependency);
        return dependency;
    }

    public IList<IDeletionDependency> Dependencies { get; } = new List<IDeletionDependency>();

    public IDeletionDependency Parent { get; } = parent;

    public Guid CurrentTopPrimaryKeyValue { get; set; } = topPrimaryKey;

    public string BuildQuery(QueryType queryType)
    {
        string resultingQuery;
        if (Parent.Parent == null)
            resultingQuery = $"SELECT {GetColumnFormattedAsPerQueryType(queryType, PrimaryKeyField)} FROM {TableName} WHERE {ForeignKeyField} = @PK";
        else
            resultingQuery = $"SELECT {GetColumnFormattedAsPerQueryType(queryType, PrimaryKeyField)} FROM {TableName} WHERE {ForeignKeyField} IN ({Parent.BuildQuery(Parent.Parent == null ? QueryType.Count : QueryType.Rows)})";

        if (queryType == QueryType.Report) resultingQuery += " ORDER BY " + OrderBy;

        return resultingQuery;
    }

    public bool Restrict { get; set; }

    public IDbCommand GetCommand(QueryType queryType)
    {
        var command = MakeCommand();
        command.CommandText = BuildQuery(queryType);

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@PK";
        parameter.Value = CurrentTopPrimaryKeyValue;

        command.Parameters.Add(parameter);

        return command;
    }

    protected virtual IDbCommand MakeCommand() => DataService.NewCommandObject();

    private string GetColumnFormattedAsPerQueryType(QueryType queryType, string column)
    {
        var result = string.Empty;
        switch (queryType)
        {
            case QueryType.Count:
                result = $"Count({column})";
                break;
            case QueryType.Rows:
                result = column;
                break;
            case QueryType.Report:
                result = ColumnList;
                break;
        }

        return result;
    }
}

public class DeletionDependencyManager : IDeletionDependency
{
    private readonly List<string> rules = [];

    public DeletionDependencyManager() { }

    /// <summary>Initializes a new instance of the DeletionDependencyManager class.</summary>
    /// <param name="dataService">The data service.</param>
    public DeletionDependencyManager(IDataService dataService) => DataService = dataService;

    public DataTable CurrentDependentRowsTable { get; set; }
    public IDataService DataService { get; set; }
    public string PrimaryKeyField { get; set; }
    public string ForeignKeyField { get; set; }
    public string TableName { get; set; }
    public string ColumnList { get; set; }
    public string OrderBy { get; set; }
    public string ViolationMessage { get; set; }

    public Dependency AddDependency()
    {
        var dependency = new Dependency(this, CurrentTopPrimaryKeyValue);
        Dependencies.Add(dependency);
        return dependency;
    }

    public IList<IDeletionDependency> Dependencies { get; } = new List<IDeletionDependency>();

    public IDeletionDependency Parent => null;

    public Guid CurrentTopPrimaryKeyValue { get; set; }

    public string BuildQuery(QueryType queryType) => string.Empty;

    public bool Restrict { get; set; }

    public IDbCommand GetCommand(QueryType queryType) => throw new NotImplementedException();

    public IList<string> GetRules()
    {
        rules.Clear();
        ListDependencies(Dependencies);
        return rules;
    }

    private void ListDependencies(IEnumerable<IDeletionDependency> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            if (dependency.Restrict) rules.Add(dependency.ViolationMessage);
            ListDependencies(dependency.Dependencies);
        }
    }
}