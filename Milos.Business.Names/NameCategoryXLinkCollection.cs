namespace Milos.Business.Names;

/// <summary>
/// Name category xlink collection interface
/// </summary>
public interface INameCategoryXLinkCollection : IEntityXlinkSubItemCollection
{
	/// <summary>
	/// Indexer
	/// </summary>
	new INameCategoryEntity this[int index] { get; }
}

/// <summary>
/// Provides a collection that links names to assigned categories.
/// </summary>
public class NameCategoryXLinkCollection : EntityXlinkSubItemCollection, INameCategoryXLinkCollection
{
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="parentEntity">Parent Entity</param>
	public NameCategoryXLinkCollection(IBusinessEntity parentEntity) : base(parentEntity) { }

	/// <summary>
	/// We configure the object for fundamental operations.
	/// </summary>
	protected override void Configure()
	{
		PrimaryKeyField = "pk_namecategoryassignment";
		ForeignKeyField = "fk_name";
		ParentTableName = "Names";
		ParentTablePrimaryKeyField = "pk_name";
		TargetForeignKeyField = "fk_namecategory";
		TargetPrimaryKeyField = "pk_namecategory";
		TargetTextField = "cName";
	}

	/// <summary>
	/// Instantiates the object used for each item in the collection
	/// </summary>
	/// <returns></returns>
	public override IEntitySubItemCollectionItem GetItemObject() => new NameCategoryEntity(this);

	/// <summary>
	/// New indexer that provides the appropriate member object type.
	/// </summary>
	public new INameCategoryEntity this[int index] => (INameCategoryEntity)base[index];
}

/// <summary>
/// Basic interface for name category entities
/// </summary>
public interface INameCategoryEntity : IEntitySubItemCollectionXLinkItem { }

/// <summary>
/// Basic implementation of a name category entity
/// </summary>
public class NameCategoryEntity : EntitySubItemCollectionXLinkItem, INameCategoryEntity
{
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="parentCollection">Parent collection</param>
	public NameCategoryEntity(IEntitySubItemCollection parentCollection) : base(parentCollection) { }
}