namespace Milos.Business.Names;

/// <summary>
/// Summary description for AddressBusinessObject.
/// </summary>
public class AddressBusinessObject : BusinessObject
{
    /// <summary>
    /// Configures settings in the current BO
    /// </summary>
    protected override void Configure()
    {
        MasterEntity = "Address";
        PrimaryKeyField = "pk_address";
    }

    public string GetNameByAddressId(Guid addressId)
    {
        using var command = NewDbCommand("select pk_name, cSearchName, cFirstName, cLastName, cCompanyName from Names inner join placement on names.pk_name = placement.fk_name where placement.fk_address = @Id");
        AddDbCommandParameter(command, "@Id", addressId);
        using (var ds = ExecuteQuery(command))
        {
            if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                var fullName = ds.Tables[0].Rows[0]["cSearchName"].ToStringSafe().Trim();
                if (string.IsNullOrEmpty(fullName))
                {
                    var firstName = ds.Tables[0].Rows[0]["cFirstName"].ToStringSafe().Trim();
                    var lastName = ds.Tables[0].Rows[0]["cLastName"].ToStringSafe().Trim();
                    if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                        fullName = firstName + " " + lastName;
                    else if (!string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                        fullName = firstName;
                    else if (string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                        fullName = lastName;
                }

                var companyName = ds.Tables[0].Rows[0]["cCompanyName"].ToStringSafe().Trim();
                if (!string.IsNullOrEmpty(companyName))
                {
                    if (!string.IsNullOrEmpty(fullName)) fullName += "\n";
                    fullName += companyName;
                }

                return fullName;
            }
        }

        return string.Empty;
    }
}