using System;
using System.Data;
using Milos.BusinessObjects;

namespace Milos.Business.Names
{
    /// <summary>
    /// Basic Milos Name business object. 
    /// This object is used as the basis for all name related business objects, such as
    /// customers, patients,...
    /// </summary>
    public class NameBusinessObject : BusinessObject
    {
        /// <summary>
        /// Default list of fields returned by the GetList() methods with parameters
        /// </summary>
        public string DefaultNameFields { get; set; } = "Names.*";

        /// <summary>
        /// Placement primary key field name
        /// </summary>
        public string PlacementKeyField { get; set; } = "pk_Placement";

        /// <summary>
        /// Address primary key field name
        /// </summary>
        public string AddressKeyField { get; set; } = "pk_Address";

        /// <summary>
        /// Communication Info Assignment primary key field name
        /// </summary>
        public string CommAssignmentKeyField { get; set; } = "pk_CommAssignment";

        /// <summary>
        /// Comm info primary key field name
        /// </summary>
        public string CommInfoKeyField { get; set; } = "pk_CommInfo";

        /// <summary>
        /// Comm info value value field name
        /// </summary>
        public string CommInfoValueField { get; set; } = "cValue";

        /// <summary>
        /// Name category assignment primary key field name
        /// </summary>
        public string NameCategoryAssignmentKeyField { get; set; } = "pk_NameCategoryAssignment";

        /// <summary>
        /// Placement foreign key field name
        /// (between placement and name)
        /// </summary>
        public string PlacementForeignKeyField { get; set; } = "fk_name";

        /// <summary>
        /// Placement foreign key field name
        /// (between placement and address)
        /// </summary>
        public string PlacementForeignKeyField2 { get; set; } = "fk_address";

        /// <summary>
        /// Communication Info Assignment foreign key field name
        /// (between name and comminfoassignment)
        /// </summary>
        public string CommAssignmentForeignKeyField { get; set; } = "fk_name";

        /// <summary>
        /// Communication Info Assignment foreign key field name
        /// (between comminfo and comminfoassignment)
        /// </summary>
        public string CommAssignmentForeignKeyField2 { get; set; } = "fk_comminfo";

        /// <summary>
        /// Name category assignment foreign key field name
        /// </summary>
        public string NameCategoryAssignmentForeignKeyField { get; set; } = "fk_name";

        /// <summary>
        /// Placement table name
        /// </summary>
        public string PlacementTable { get; set; } = "Placement";

        /// <summary>
        /// Address table name
        /// </summary>
        public string AddressTable { get; set; } = "Address";

        /// <summary>
        /// Communication Info Assignment Table Name
        /// </summary>
        public string CommAssignmentTable { get; set; } = "CommAssignment";

        /// <summary>
        /// Comm info table name
        /// </summary>
        public string CommInfoTable { get; set; } = "CommInfo";

        /// <summary>
        /// Name category assignment table name
        /// </summary>
        public string NameCategoryAssignmentTable { get; set; } = "NameCategoryAssignment";

        /// <summary>
        /// Name category table name
        /// </summary>
        public string NameCategoryTable { get; set; } = "NameCategory";

        /// <summary>
        /// First name field for internal queries
        /// </summary>
        public string FirstNameField { get; set; } = "cFirstName";

        /// <summary>
        /// Last name field for internal queries
        /// </summary>
        public string LastNameField { get; set; } = "cLastName";

        /// <summary>
        /// Company name field for internal queries
        /// </summary>
        public string CompanyNameField { get; set; } = "cCompanyName";

        /// <summary>
        /// Name of the country table
        /// </summary>
        public string CountryTable { get; set; } = "Country";

        /// <summary>
        /// Name of the country table's primary key field
        /// </summary>
        public string CountryKeyField { get; set; } = "pk_country";

        /// <summary>
        /// Field name of the foreign key field that links to countries (such as fk_country)
        /// </summary>
        public string AddressCountryForeignKey { get; set; } = "fk_country";

        /// <summary>
        /// Configures settings in the current BO
        /// </summary>
        protected override void Configure()
        {
            MasterEntity = "Names";
            PrimaryKeyField = "pk_name";
        }

        /// <summary>
        /// Saves all secondary tables
        /// </summary>
        /// <param name="parentPk">Parent PK</param>
        /// <param name="existingDataSet">Existing DataSet</param>
        /// <returns>True if successful</returns>
        protected override bool SaveSecondaryTables(Guid parentPk, DataSet existingDataSet)
        {
            // Default save behavior
            var success = base.SaveSecondaryTables(parentPk, existingDataSet);
            if (success != true) return false;

            // Saving addresses
            success = SaveAddresses(existingDataSet);
            if (!success) return false;

            // Saving CommInfo
            success = SaveCommInfo(existingDataSet);
            if (!success) return false;

            // Saving the category assignment
            success = SaveCategoryAssignment(existingDataSet);
            if (!success) return false;

            return true;
        }

        /// <summary>
        /// Saves address information.
        /// </summary>
        /// <param name="existingDataSet">DataSet containing the entity data</param>
        /// <returns>True if successful</returns>
        /// <remarks>Saves both the placement and the address table.</remarks>
        protected virtual bool SaveAddresses(DataSet existingDataSet)
        {
            var success = SaveTable(existingDataSet.Tables[AddressTable], AddressKeyField);
            if (!success) return false;
            success = SaveTable(existingDataSet.Tables[PlacementTable], PlacementKeyField);
            if (!success) return false;
            return true;
        }

        /// <summary>
        /// Saves communication information.
        /// </summary>
        /// <param name="existingDataSet">DataSet containing the entity data</param>
        /// <returns>True if successful</returns>
        /// <remarks>Saves both the comminfo and comm info assignment tables.</remarks>
        protected virtual bool SaveCommInfo(DataSet existingDataSet)
        {
            var success = SaveTable(existingDataSet.Tables[CommInfoTable], CommInfoKeyField);
            if (!success) return false;
            success = SaveTable(existingDataSet.Tables[CommAssignmentTable], CommAssignmentKeyField);
            if (!success) return false;
            return true;
        }

        /// <summary>
        /// Saves the name/category assignment
        /// </summary>
        /// <param name="existingDataSet">DataSet containing the entity data</param>
        /// <returns>True if successful</returns>
        public virtual bool SaveCategoryAssignment(DataSet existingDataSet)
        {
            if (existingDataSet == null) throw new ArgumentNullException("existingDataSet");
            return SaveTable(existingDataSet.Tables[NameCategoryAssignmentTable], NameCategoryAssignmentKeyField);
        }

        /// <summary>
        /// Returns a custom entiry object
        /// </summary>
        /// <param name="defaultData">Default data</param>
        /// <returns>Entity</returns>
        protected virtual IBusinessEntity GetEntityObject(DataSet defaultData) => new NameBusinessEntity(defaultData);

        /// <summary>
        /// Adds new data
        /// </summary>
        /// <returns>DataSet</returns>
        public override DataSet AddNew()
        {
            var dsName = base.AddNew();
            AddNewAddress(dsName);
            AddNewCommInfo(dsName);
            AddNewCategory(dsName);
            return dsName;
        }

        /// <summary>
        /// Adds new tables and records for addresses and location assignments
        /// </summary>
        /// <param name="nameDataSet">New name DataSet</param>
        protected virtual void AddNewAddress(DataSet nameDataSet)
        {
            NewSecondaryEntity(PlacementTable, nameDataSet);
            NewSecondaryEntity(AddressTable, nameDataSet);
        }

        /// <summary>
        /// Adds new tables and records for communication information
        /// and communication information assignments.
        /// </summary>
        /// <param name="nameDataSet">New name DataSet</param>
        protected virtual void AddNewCommInfo(DataSet nameDataSet)
        {
            NewSecondaryEntity(CommAssignmentTable, nameDataSet);
            NewSecondaryEntity(CommInfoTable, nameDataSet);
        }

        /// <summary>
        /// Adds new tables and records for category assignments.
        /// Also loads all the available categories.
        /// </summary>
        /// <param name="nameDataSet">New name DataSet</param>
        protected virtual void AddNewCategory(DataSet nameDataSet)
        {
            NewSecondaryEntity(NameCategoryAssignmentTable, nameDataSet);
            GetAllNameCategories(nameDataSet);
        }

        /// <summary>
        /// Loads secondary tables
        /// </summary>
        /// <param name="parentPk">Parent PK</param>
        /// <param name="existingDataSet">Existing DataSet</param>
        protected override void LoadSecondaryTables(Guid parentPk, DataSet existingDataSet)
        {
            if (existingDataSet == null) throw new ArgumentNullException("existingDataSet");

            base.LoadSecondaryTables(parentPk, existingDataSet);

            // Loading address data
            LoadAddressTables(parentPk, existingDataSet);

            // Comm info tables
            LoadCommInfoTables(parentPk, existingDataSet);

            // Name categories
            LoadCategoryTables(parentPk, existingDataSet);
        }

        /// <summary>
        /// Loads all the required address and location data
        /// </summary>
        /// <param name="parentPk">Name PK</param>
        /// <param name="existingDataSet">Existing DataSet</param>
        protected virtual void LoadAddressTables(Guid parentPk, DataSet existingDataSet)
        {
            // This is the cross-link table to link to addresses
            using (var cmd = GetMultipleRecordsByKeyCommand(PlacementTable, "*", PlacementForeignKeyField, parentPk))
                ExecuteQuery(cmd, PlacementTable, existingDataSet);

            // We now add the actual address records
            using (var cmd2 = NewDbCommand($"SELECT {AddressTable}.* FROM {AddressTable}, {PlacementTable} where {AddressTable}.{AddressKeyField} = {PlacementTable}.{PlacementForeignKeyField2} and {PlacementTable}.{PlacementForeignKeyField} = @FK"))
            {
                AddDbCommandParameter(cmd2, "@FK", parentPk);
                ExecuteQuery(cmd2, AddressTable, existingDataSet);
            }
        }

        /// <summary>
        /// Loads all the required comm info and comm info assignment data
        /// </summary>
        /// <param name="parentPk">Name PK</param>
        /// <param name="existingDataSet">Existing DataSet</param>
        protected virtual void LoadCommInfoTables(Guid parentPk, DataSet existingDataSet)
        {
            // This is the cross-link to the phone numbers (comm info)
            using (var cmd = GetMultipleRecordsByKeyCommand(CommAssignmentTable, "*", CommAssignmentForeignKeyField, parentPk))
                ExecuteQuery(cmd, CommAssignmentTable, existingDataSet);

            // We now add the actual comminfo records
            using (var cmd2 = NewDbCommand($"SELECT {CommInfoTable}.* FROM {CommInfoTable}, {CommAssignmentTable} where {CommInfoTable}.{CommInfoKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField2} and {CommAssignmentTable}.{CommAssignmentForeignKeyField} = @FK"))
            {
                AddDbCommandParameter(cmd2, "@FK", parentPk);
                ExecuteQuery(cmd2, CommInfoTable, existingDataSet);
            }
        }

        /// <summary>
        /// Loads all the name category information
        /// (xlink table as well as all the target categories)
        /// </summary>
        /// <param name="parentPk">Parent PK</param>
        /// <param name="existingDataSet">Existing DataSet</param>
        protected virtual void LoadCategoryTables(Guid parentPk, DataSet existingDataSet)
        {
            // Now, we add all the category records
            using (var cmd = GetMultipleRecordsByKeyCommand(NameCategoryAssignmentTable, "*", NameCategoryAssignmentForeignKeyField, parentPk))
                ExecuteQuery(cmd, NameCategoryAssignmentTable, existingDataSet);

            // We also load all the categories we can link to
            GetAllNameCategories(existingDataSet);
        }

        /// <summary>
        /// This method returns a DataSet that contains all names that with a certain first and last name.
        /// Note that the search is done exact, unless wildcards are embedded (such as E% for the last name).
        /// </summary>
        /// <param name="lastName">Last Name</param>
        /// <param name="firstName">First Name</param>
        /// <returns>Result DataSet</returns>
        /// <remarks>
        /// The result set of this operation contains 3 tables: Names, CommInfo, and Addresses
        /// 
        /// The tables have the following structures:
        /// 
        ///    Names
        ///       Contains all the fields defined in the DefaultNameField property of this business object
        ///       Default: Names.*
        ///    
        ///    CommInfo
        ///       FK_Name (Guid)     = Foreign key that links to names
        ///       cType (String)     = CommInfo link type, such as "home phone"
        ///       cValue (String)    = Value, such as the actual phone number or email address
        ///       iCommType (Int)    = Maps to the CommInfoType enum and defines the type of information (phone, email,...)
        ///       PK_CommInfo (Guid) = Primary key of the particular comm info
        /// 
        ///    Addresses
        ///       FK_Name (Guid)           = Foreign key that links to names
        ///       cType (String)           = Address link type, such as "home" or "office"
        ///       PK_Address (Guid)        = Address primary key
        ///       FK_Country (Guid)        = Foreign key of the associated country
        ///       cStreet (String)         = Street address
        ///       cStreet2 (String)        = Street address 2
        ///       cStreet3 (String)        = Street address 3
        ///       cCity (String)           = City
        ///       cState (String)          = State/Province
        ///       cZip (String)            = Zip Code/ Postal Code
        ///       cAddressName (String)    = Address name (name that may be unique to this address)
        ///       cAddressCompany (String) = Address company (company name that may be unique to this address)
        ///       cCountryName (String)    = Country name
        ///       iAddrFormat (Int)        = Address format type. Maps to the AddressFormat enum.
        ///       FullAddress (String)     = Complete, properly formatted address string
        /// 
        /// Note: Field names may differ when properties on the business objects relating to field names are set to non-default values.
        /// </remarks>
        public virtual DataSet GetList(string lastName, string firstName) => GetList(lastName, firstName, string.Empty);

        /// <summary>
        /// This method returns a DataSet that contains all names that with a certain first and last name.
        /// Note that the search is done exact, unless wildcards are embedded (such as E% for the last name).
        /// </summary>
        /// <param name="lastName">Last Name</param>
        /// <param name="firstName">First Name</param>
        /// <param name="companyName">Company Name</param>
        /// <returns>Result DataSet</returns>
        /// <remarks>
        /// The result set of this operation contains 3 tables: Names, CommInfo, and Addresses
        /// 
        /// The tables have the following structures:
        /// 
        ///    Names
        ///       Contains all the fields defined in the DefaultNameField property of this business object
        ///       Default: Names.*
        ///    
        ///    CommInfo
        ///       FK_Name (Guid)     = Foreign key that links to names
        ///       cType (String)     = CommInfo link type, such as "home phone"
        ///       cValue (String)    = Value, such as the actual phone number or email address
        ///       iCommType (Int)    = Maps to the CommInfoType enum and defines the type of information (phone, email,...)
        ///       PK_CommInfo (Guid) = Primary key of the particular comm info
        /// 
        ///    Addresses
        ///       FK_Name (Guid)           = Foreign key that links to names
        ///       cType (String)           = Address link type, such as "home" or "office"
        ///       PK_Address (Guid)        = Address primary key
        ///       FK_Country (Guid)        = Foreign key of the associated country
        ///       cStreet (String)         = Street address
        ///       cStreet2 (String)        = Street address 2
        ///       cStreet3 (String)        = Street address 3
        ///       cCity (String)           = City
        ///       cState (String)          = State/Province
        ///       cZip (String)            = Zip Code/ Postal Code
        ///       cAddressName (String)    = Address name (name that may be unique to this address)
        ///       cAddressCompany (String) = Address company (company name that may be unique to this address)
        ///       cCountryName (String)    = Country name
        ///       iAddrFormat (Int)        = Address format type. Maps to the AddressFormat enum.
        ///       FullAddress (String)     = Complete, properly formatted address string
        /// 
        /// Note: Field names may differ when properties on the business objects relating to field names are set to non-default values.
        /// </remarks>
        public virtual DataSet GetList(string lastName, string firstName, string companyName)
        {
            // First, we retrieve all name information
            using (var command1 = NewDbCommand($"SELECT {DefaultNameFields} FROM {MasterEntity}"))
            {
                var whereClause = string.Empty;
                var statementAdded = false;
                if (!string.IsNullOrEmpty(lastName))
                {
                    whereClause += $"{MasterEntity}.{LastNameField} like @LName ";
                    AddDbCommandParameter(command1, "@LName", lastName.Trim() + "%");
                    statementAdded = true;
                }

                if (!string.IsNullOrEmpty(firstName))
                {
                    if (!statementAdded) whereClause += " WHERE ";
                    if (statementAdded) whereClause += "AND ";
                    whereClause += $"{MasterEntity}.{FirstNameField} like @FName ";
                    AddDbCommandParameter(command1, "@FName", firstName.Trim() + "%");
                    statementAdded = true;
                }

                if (!string.IsNullOrEmpty(companyName))
                {
                    if (!statementAdded) whereClause += " WHERE ";
                    if (statementAdded) whereClause += "AND ";
                    whereClause += $"{MasterEntity}.{CompanyNameField} like @Company ";
                    AddDbCommandParameter(command1, "@Company", companyName.Trim() + "%");
                }

                command1.CommandText += whereClause;
                var dsNames = ExecuteQuery(command1, "Names");

                // We now add comm-info information
                using (var command2 = NewDbCommand())
                {
                    command2.CommandText = $"SELECT {CommAssignmentTable}.{CommAssignmentForeignKeyField}, {CommAssignmentTable}.ctype, {CommInfoTable}.cValue, {CommInfoTable}.iCommType, {CommInfoTable}.{CommInfoKeyField} FROM {MasterEntity} INNER JOIN {CommAssignmentTable} ON {MasterEntity}.{PrimaryKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField} INNER JOIN {CommInfoTable} ON {CommAssignmentTable}.{CommAssignmentForeignKeyField2} = {CommInfoTable}.{CommInfoKeyField} ";
                    command2.CommandText += whereClause;
                    if (!string.IsNullOrEmpty(lastName)) AddDbCommandParameter(command2, "@LName", lastName.Trim() + "%");
                    if (!string.IsNullOrEmpty(firstName)) AddDbCommandParameter(command2, "@FName", firstName.Trim() + "%");
                    if (!string.IsNullOrEmpty(companyName)) AddDbCommandParameter(command2, "@Company", companyName.Trim() + "%");
                    // We add the result of this query to our names DataSet
                    ExecuteQuery(command2, "CommInfo", dsNames);
                    // We relate the two result sets so it is easy to find communication information that goes with the name
                    dsNames.Relations.Add("RelatedCommInfo", dsNames.Tables["Names"].Columns[PrimaryKeyField], dsNames.Tables["CommInfo"].Columns[CommAssignmentForeignKeyField]);
                }

                // We also add the address information
                using (var addressCommand = NewDbCommand())
                {
                    addressCommand.CommandText = $"SELECT {PlacementTable}.{PlacementForeignKeyField}, {PlacementTable}.cType, {AddressTable}.*, {CountryTable}.cname AS cCountryName, {CountryTable}.ccode AS cCountryCode, {CountryTable}.iaddrformat FROM {MasterEntity} INNER JOIN {PlacementTable} ON {MasterEntity}.{PrimaryKeyField} = {PlacementTable}.{PlacementForeignKeyField} INNER JOIN {AddressTable} ON {PlacementTable}.{PlacementForeignKeyField2} = {AddressTable}.{AddressKeyField} INNER JOIN {CountryTable} ON {AddressTable}.{AddressCountryForeignKey} = {CountryTable}.{CountryKeyField}";
                    addressCommand.CommandText += whereClause;
                    if (!string.IsNullOrEmpty(lastName)) AddDbCommandParameter(addressCommand, "@LName", lastName.Trim() + "%");
                    if (!string.IsNullOrEmpty(firstName)) AddDbCommandParameter(addressCommand, "@FName", firstName.Trim() + "%");
                    if (!string.IsNullOrEmpty(companyName)) AddDbCommandParameter(addressCommand, "@Company", companyName.Trim() + "%");
                    // We add the result of this query to our names DataSet
                    ExecuteQuery(addressCommand, "Addresses", dsNames);
                }

                // We relate the two result sets so it is easy to find communication information that goes with the name
                dsNames.Relations.Add("RelatedAddresses", dsNames.Tables["Names"].Columns[PrimaryKeyField], dsNames.Tables["Addresses"].Columns[PlacementForeignKeyField]);
                // We also provide a field in the address table with the pre-formatted proper address
                FormatAddressesInTable(dsNames.Tables["Addresses"]);

                return dsNames;
            }
        }

        /// <summary>
        /// This method returns a DataSet that contains all names that with a certain first and last name,
        /// as well as certain communication information, such as email or telephone.
        /// Note that the search is done exact, unless wildcards are embedded (such as E% for the last name).
        /// </summary>
        /// <param name="lastName">Last Name</param>
        /// <param name="firstName">First Name</param>
        /// <param name="companyName">Company Name</param>
        /// <param name="commInfo">Communication information (email, phone, fax, web,...)</param>
        /// <returns>Result DataSet</returns>
        /// <remarks>
        /// The result set of this operation contains 3 tables: Names, CommInfo, and Addresses
        /// 
        /// The tables have the following structures:
        /// 
        ///    Names
        ///       Contains all the fields defined in the DefaultNameField property of this business object
        ///       Default: Names.*
        ///    
        ///    CommInfo
        ///       FK_Name (Guid)     = Foreign key that links to names
        ///       cType (String)     = CommInfo link type, such as "home phone"
        ///       cValue (String)    = Value, such as the actual phone number or email address
        ///       iCommType (Int)    = Maps to the CommInfoType enum and defines the type of information (phone, email,...)
        ///       PK_CommInfo (Guid) = Primary key of the particular comm info
        /// 
        ///    Addresses
        ///       FK_Name (Guid)           = Foreign key that links to names
        ///       cType (String)           = Address link type, such as "home" or "office"
        ///       PK_Address (Guid)        = Address primary key
        ///       FK_Country (Guid)        = Foreign key of the associated country
        ///       cStreet (String)         = Street address
        ///       cStreet2 (String)        = Street address 2
        ///       cStreet3 (String)        = Street address 3
        ///       cCity (String)           = City
        ///       cState (String)          = State/Province
        ///       cZip (String)            = Zip Code/ Postal Code
        ///       cAddressName (String)    = Address name (name that may be unique to this address)
        ///       cAddressCompany (String) = Address company (company name that may be unique to this address)
        ///       cCountryName (String)    = Country name
        ///       iAddrFormat (Int)        = Address format type. Maps to the AddressFormat enum.
        ///       FullAddress (String)     = Complete, properly formatted address string
        /// 
        /// Note: Field names may differ when properties on the business objects relating to field names are set to non-default values.
        /// </remarks>
        public virtual DataSet GetList(string lastName, string firstName, string companyName, string commInfo)
        {
            if (string.IsNullOrEmpty(commInfo))
                // Since no comm-info is specified, we can just use another overload
                return GetList(lastName, firstName, companyName);

            // First, we retrieve all name information
            using (var command = NewDbCommand())
            {
                command.CommandText = $"SELECT {DefaultNameFields} FROM {MasterEntity} WHERE {PrimaryKeyField} IN (SELECT DISTINCT {PrimaryKeyField} FROM {MasterEntity} INNER JOIN {CommAssignmentTable} ON {MasterEntity}.{PrimaryKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField} INNER JOIN {CommInfoTable} ON {CommAssignmentTable}.{CommAssignmentForeignKeyField2} = {CommInfoTable}.{CommInfoKeyField}";
                var whereClause = string.Empty;
                var statementAdded = false;
                if (!string.IsNullOrEmpty(lastName))
                {
                    whereClause += $"{MasterEntity}.{LastNameField} like @LName ";
                    AddDbCommandParameter(command, "@LName", lastName.Trim() + "%");
                    statementAdded = true;
                }

                if (!string.IsNullOrEmpty(firstName))
                {
                    if (!statementAdded) whereClause += " WHERE ";
                    if (statementAdded) whereClause += "AND ";
                    whereClause += $"{MasterEntity}.{FirstNameField} like @FName ";
                    AddDbCommandParameter(command, "@FName", firstName.Trim() + "%");
                    statementAdded = true;
                }

                if (!string.IsNullOrEmpty(companyName))
                {
                    if (!statementAdded) whereClause += " WHERE ";
                    if (statementAdded) whereClause += "AND ";
                    whereClause += $"{MasterEntity}.{CompanyNameField} like @Company ";
                    AddDbCommandParameter(command, "@Company", companyName.Trim() + "%");
                    statementAdded = true;
                }

                if (!statementAdded) whereClause += " WHERE ";
                if (statementAdded) whereClause += "AND ";
                whereClause += CommInfoTable + "." + CommInfoValueField + " like @CommInfo ";
                AddDbCommandParameter(command, "@CommInfo", commInfo.Trim() + "%");

                command.CommandText += whereClause + ")";
                var dsNames = ExecuteQuery(command, "Names");

                // We now add comm-info information
                using (var command2 = NewDbCommand())
                {
                    command2.CommandText = $"SELECT {CommAssignmentTable}.{CommAssignmentForeignKeyField}, {CommAssignmentTable}.ctype, {CommInfoTable}.cValue, {CommInfoTable}.iCommType, {CommInfoTable}.{CommInfoKeyField} FROM {MasterEntity} INNER JOIN {CommAssignmentTable} ON {MasterEntity}.{PrimaryKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField} INNER JOIN {CommInfoTable} ON {CommAssignmentTable}.{CommAssignmentForeignKeyField2} = {CommInfoTable}.{CommInfoKeyField} WHERE {PrimaryKeyField} IN (SELECT DISTINCT {PrimaryKeyField} FROM {MasterEntity} INNER JOIN {CommAssignmentTable} ON {MasterEntity}.{PrimaryKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField} INNER JOIN {CommInfoTable} ON {CommAssignmentTable}.{CommAssignmentForeignKeyField2} = {CommInfoTable}.{CommInfoKeyField}";
                    command2.CommandText += whereClause + ")";
                    if (!string.IsNullOrEmpty(lastName)) AddDbCommandParameter(command2, "@LName", lastName.Trim() + "%");
                    if (!string.IsNullOrEmpty(firstName)) AddDbCommandParameter(command2, "@FName", firstName.Trim() + "%");
                    if (!string.IsNullOrEmpty(companyName)) AddDbCommandParameter(command2, "@Company", companyName.Trim() + "%");
                    AddDbCommandParameter(command2, "@CommInfo", commInfo.Trim() + "%");
                    // We add the result of this query to our names DataSet
                    ExecuteQuery(command2, "CommInfo", dsNames);
                    // We relate the two result sets so it is easy to find communication information that goes with the name
                    dsNames.Relations.Add("RelatedCommInfo", dsNames.Tables["Names"].Columns[PrimaryKeyField], dsNames.Tables["CommInfo"].Columns[CommAssignmentForeignKeyField]);
                }


                // We also add the address information
                using (var addressCommand = NewDbCommand())
                {
                    addressCommand.CommandText = $"SELECT {PlacementTable}.{PlacementForeignKeyField}, {PlacementTable}.cType, {AddressTable}.*, {CountryTable}.cname AS cCountryName, {CountryTable}.ccode AS cCountryCode, {CountryTable}.iaddrformat  FROM {MasterEntity} INNER JOIN {PlacementTable} ON {MasterEntity}.{PrimaryKeyField} = {PlacementTable}.{PlacementForeignKeyField} INNER JOIN {AddressTable} ON {PlacementTable}.{PlacementForeignKeyField2} = {AddressTable}.{AddressKeyField} INNER JOIN {CountryTable} ON {AddressTable}.{AddressCountryForeignKey} = {CountryTable}.{CountryKeyField} WHERE {PrimaryKeyField} IN (SELECT DISTINCT {PrimaryKeyField} FROM {MasterEntity} INNER JOIN {CommAssignmentTable} ON {MasterEntity}.{PrimaryKeyField} = {CommAssignmentTable}.{CommAssignmentForeignKeyField} INNER JOIN {CommInfoTable} ON {CommAssignmentTable}.{CommAssignmentForeignKeyField2} = {CommInfoTable}.{CommInfoKeyField}";
                    addressCommand.CommandText += whereClause + ")";
                    if (!string.IsNullOrEmpty(lastName)) AddDbCommandParameter(addressCommand, "@LName", lastName.Trim() + "%");
                    if (!string.IsNullOrEmpty(firstName)) AddDbCommandParameter(addressCommand, "@FName", firstName.Trim() + "%");
                    if (!string.IsNullOrEmpty(companyName)) AddDbCommandParameter(addressCommand, "@Company", companyName.Trim() + "%");
                    AddDbCommandParameter(addressCommand, "@CommInfo", commInfo.Trim() + "%");
                    // We add the result of this query to our names DataSet
                    ExecuteQuery(addressCommand, "Addresses", dsNames);
                    // We relate the two result sets so it is easy to find communication information that goes with the name
                }

                dsNames.Relations.Add("RelatedAddresses", dsNames.Tables["Names"].Columns[PrimaryKeyField], dsNames.Tables["Addresses"].Columns[PlacementForeignKeyField]);
                // We also provide a field in the address table with the pre-formated proper address
                FormatAddressesInTable(dsNames.Tables["Addresses"]);

                return dsNames;
            }
        }

        /// <summary>
        /// Makes sure the address table has a field for the full address 
        /// containing a properly formatted address.
        /// </summary>
        /// <param name="addressTable">DataTable containing addresses</param>
        protected virtual void FormatAddressesInTable(DataTable addressTable)
        {
            // First, we make sure the field containing the full address exists
            if (!addressTable.Columns.Contains("FullAddress")) addressTable.Columns.Add("FullAddress");

            // Then, we format the address
            foreach (DataRow addressRow in addressTable.Rows)
            {
                var address = string.Empty;
                address += addressRow["cStreet"].ToString().Trim() + "\n";
                if (!string.IsNullOrEmpty(addressRow["cStreet2"].ToString())) address += addressRow["cStreet2"].ToString().Trim() + "\n";
                if (!string.IsNullOrEmpty(addressRow["cStreet3"].ToString())) address += addressRow["cStreet3"].ToString().Trim() + "\n";
                AddressFormat format;
                try
                {
                    format = (AddressFormat) addressRow["iAddrFormat"];
                }
                catch
                {
                    format = AddressFormat.CityStateZip;
                }

                switch (format)
                {
                    case AddressFormat.CityStateZip:
                        address += addressRow["cCity"].ToString().Trim() + ", " + addressRow["cState"].ToString().Trim() + " " + addressRow["cZip"].ToString().Trim() + "\n";
                        break;
                    case AddressFormat.CityPostalCode:
                        address += addressRow["cCity"].ToString().Trim() + ", " + addressRow["cZip"].ToString().Trim() + "\n";
                        break;
                    case AddressFormat.PostalCodeCity:
                        address += addressRow["cZip"].ToString().Trim() + " " + addressRow["cCity"].ToString().Trim() + "\n";
                        break;
                    case AddressFormat.PostalCodeCityState:
                        address += addressRow["cZip"].ToString().Trim() + " " + addressRow["cCity"].ToString().Trim() + ", " + addressRow["cState"].ToString().Trim() + "\n";
                        break;
                }

                address += addressRow["cCountryName"].ToString().Trim();
                addressRow["FullAddress"] = address;
            }
        }

        /// <summary>
        /// This method loads all name categories into an existing DataSet
        /// </summary>
        /// <param name="existingDataSet">Existing data set</param>
        public virtual void GetAllNameCategories(DataSet existingDataSet)
        {
            using (var comCat = NewDbCommand($"SELECT * FROM {NameCategoryTable}"))
                ExecuteQuery(comCat, "NameCategory", existingDataSet);
        }

        /// <summary>
        /// This method loads all name categories into an existing DataSet
        /// </summary>
        public virtual DataSet GetAllNameCategories()
        {
            using (var comCat = NewDbCommand($"SELECT * FROM {NameCategoryTable} Order by cName"))
                return ExecuteQuery(comCat, "NameCategory");
        }
    }
}