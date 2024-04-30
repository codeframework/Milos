namespace Milos.BusinessObjects;

/// <summary>
///     Represents an individual broken business rule
/// </summary>
/// <remarks>
///     Constructor
/// </remarks>
/// <param name="brokenRule">Reference to the broken rule record</param>
/// <param name="parentCollection">Collection that contains this broken rule</param>
public class BrokenRule(DataRow brokenRule, BrokenRulesCollection parentCollection)
{
    /// <summary>
    ///     For internal use only
    /// </summary>
    private readonly BrokenRulesCollection parentCollection = parentCollection;

    /// <summary>
    ///     Internal reference to the broken rule record
    /// </summary>
    private readonly DataRow brokenRuleRow = brokenRule;

    /// <summary>
    ///     Table that contains the broken rule
    /// </summary>
    public string TableName => brokenRuleRow["TableName"].ToString();

    /// <summary>
    ///     Field name within the table that contains the broken rule
    /// </summary>
    public string FieldName => brokenRuleRow["FieldName"].ToString();

    /// <summary>
    ///     Index of the row within the table that contains the broken rule
    /// </summary>
    public int RowIndex => (int) brokenRuleRow["RowIndex"];

    /// <summary>
    ///     Rule violation type
    /// </summary>
    public RuleViolationType ViolationType => (RuleViolationType) brokenRuleRow["ViolationType"];

    /// <summary>
    ///     Message associated with the violation
    /// </summary>
    public string Message => brokenRuleRow["Message"].ToString();

    /// <summary>
    ///     Violation class
    /// </summary>
    public string Class => brokenRuleRow["RuleClass"].ToString();

    /// <summary>
    ///     Message associated with the violation
    /// </summary>
    public DataRow ProblematicRow => parentCollection.BusinessEntity.GetInternalData().Tables[TableName].Rows[RowIndex];

    /// <summary>
    ///     Displays the broken rule message
    /// </summary>
    /// <returns>Message</returns>
    public override string ToString() => Message;
}