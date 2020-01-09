using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Milos.BusinessObjects
{
    /// <summary>
    ///     This is to be used to serialize a business entity into an XML document with schema.
    /// </summary>
    public class BusinessEntityReportSerializer
    {
        /// <summary>
        ///     For internal use only
        /// </summary>
        private readonly Type businessEntityType = typeof(IBusinessEntity);

        /// <summary>
        ///     For internal use only
        /// </summary>
        private readonly Type crossLinkType = typeof(IEntitySubItemCollectionXLinkItem);

        /// <summary>
        ///     For internal use only
        /// </summary>
        private readonly Type subItemCollectionType = typeof(IEntitySubItemCollection);

        /// <summary>
        ///     This method is used to detect if the custom attribute
        ///     NotSerializable() has been applied to the property.
        /// </summary>
        /// <param name="propertyInfoType">
        ///     PropertyInfo for the property to be checked
        /// </param>
        /// <returns>
        ///     Boolean stating if the property is serializable
        /// </returns>
        public bool IsSerializable(MemberInfo propertyInfoType)
        {
            if (propertyInfoType == null)
                throw new NullReferenceException("Parameter 'propertyInfoType' cannot be null.");

            var objAttrs = propertyInfoType.GetCustomAttributes(typeof(NotReportSerializableAttribute), true);
            return objAttrs.Length == 0;
        }

        /// <summary>
        ///     This is the method to be called to kick off the process.
        /// </summary>
        /// <param name="businessEntity">
        ///     The business entity to be processed
        /// </param>
        /// <returns>
        ///     A string containing the XML document for the Business Entity data.
        /// </returns>
        public string Serialize(object businessEntity)
        {
            string entityName;
            var sb = new StringBuilder();
            var writer = new StringWriter(sb, CultureInfo.InvariantCulture);
            var xmlWriter = new XmlTextWriter(writer);
            xmlWriter.Formatting = Formatting.Indented;

            xmlWriter.WriteStartElement("NewDataSet");
            entityName = GetEntityName(businessEntity);

            // Write the schema
            WriteSchema(businessEntity, xmlWriter, entityName);

            // Write the data
            xmlWriter.WriteStartElement(entityName);
            ScanObject(businessEntity, xmlWriter);

            // write the closing tags
            xmlWriter.WriteEndElement(); // Entity Name
            xmlWriter.WriteEndElement(); // "NewDataSet
            xmlWriter.Close();

            return sb.ToString();
        }

        /// <summary>
        ///     This is an override for the method that gets called to kick
        ///     off the process.  This override takes an additional parameter
        ///     for a file name.  It will write the XML to the specified file
        ///     before it returns
        /// </summary>
        /// <param name="businessEntity">The business entity to be processed</param>
        /// <param name="fileName">The name of the file where the XML will be written.</param>
        /// <returns>
        ///     A string containing the XML document for the Business Entity data.
        /// </returns>
        public string Serialize(object businessEntity, string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                var xml = Serialize(businessEntity);
                writer.Write(xml);
                writer.Close();
                return xml;
            }
        }

        /// <summary>
        ///     This method uses reflection to get the name of the entity
        /// </summary>
        /// <param name="businessEntity">
        ///     The entity
        /// </param>
        /// <returns>
        ///     A string containing the name of the entity
        /// </returns>
        private string GetEntityName(object businessEntity)
        {
            var name = string.Empty;
            if (businessEntity is EntitySubItemCollectionItem entity)
            {
                var entityType = entity.ParentCollection.GetType();
                var members = entityType.GetDefaultMembers();
                if (members.Length > 0)
                {
                    if (members[0] is PropertyInfo)
                        name = ((PropertyInfo) members[0]).PropertyType.Name;
                }
                else
                {
                    var objType = businessEntity.GetType();
                    name = objType.Name;
                }
            }
            else
            {
                var objType = businessEntity.GetType();
                name = objType.Name;
            }

            if (name.EndsWith("Entity"))
                name = name.Substring(0, name.Length - 6);
            return name;
        }

        /// <summary>
        ///     This method scans the entity writing a new node to the XML
        ///     document for each property not marked "NotSerializable"
        /// </summary>
        /// <param name="sourceObject">
        ///     The Business Entity to be scanned
        /// </param>
        /// <param name="xmlTextWriter ">
        ///     A reference to the XmlTextWriter being used to record the XML
        private void ScanObject(object sourceObject, XmlTextWriter xmlTextWriter)
        {
            var sourceType = sourceObject.GetType();
            Type propertyType;

            // get a property info array for each property in the entity
            var properties = sourceType.GetProperties();

            // this array list is used to store references to any property that
            // is either a Business Entity or a Collection
            var subItemCollections = new List<PropertyInfo>();

            // make sure we got some properties
            if (properties.GetLength(0) >= 0)
            {
                foreach (var property in properties)
                    // we only want to process properties that are readable
                    if (property.CanRead)
                    {
                        // get the Type of the property
                        propertyType = property.PropertyType;

                        // First, we check whether we have an implementation of a significant interface
                        var interfaceFound = false;
                        foreach (var interfaceType in propertyType.GetInterfaces())
                            if (interfaceType.FullName == subItemCollectionType.FullName ||
                                interfaceType.FullName == crossLinkType.FullName)
                            {
                                interfaceFound = true;
                                break;
                            }

                        // if the property is a subclass of the Collection or Cross Link types
                        // add them to the array list.  Otherwise, process the property
                        if (interfaceFound || propertyType.IsSubclassOf(subItemCollectionType) || propertyType.IsSubclassOf(crossLinkType))
                            // This is a sub item collection, store it for later processing
                            subItemCollections.Add(property);
                        else
                        {
                            // make sure the property is not marked with the NotSerializable attribute
                            if (IsSerializable(property))
                            {
                                // if the property is a subclass of the Business Entity, add it to
                                // the array list, otherwise process it.
                                var isEntity = false;
                                foreach (var interfaceType in propertyType.GetInterfaces())
                                    if (interfaceType.FullName == businessEntityType.FullName)
                                    {
                                        isEntity = true;
                                        break;
                                    }

                                if (isEntity || propertyType.IsSubclassOf(businessEntityType))
                                    subItemCollections.Add(property);
                                else
                                {
                                    // get the type and value
                                    var typeString = property.PropertyType.ToString();
                                    string valueString;

                                    try
                                    {
                                        switch (typeString)
                                        {
                                            case "System.DateTime":
                                                var datTime = (DateTime) property.GetValue(sourceObject, null);
                                                var month = datTime.Month.ToString(CultureInfo.InvariantCulture).Trim();
                                                if (month.Length < 2) month = "0" + month;
                                                var day = datTime.Day.ToString(CultureInfo.InvariantCulture).Trim();
                                                if (day.Length < 2) day = "0" + day;
                                                valueString = $"{datTime.Year.ToString(CultureInfo.InvariantCulture)}-{month}-{day}T00:00:00.0000000-05:00";
                                                break;
                                            default:
                                                valueString = property.GetValue(sourceObject, null).ToString();
                                                break;
                                        }
                                    }
                                    catch
                                    {
                                        valueString = string.Empty;
                                    }

                                    // if the property is a boolean, we need to convert it to 1 or 0
                                    // for writing to the XML document
                                    if (typeString == "System.Boolean") valueString = valueString == "False" ? "0" : "1";

                                    // add this property to the XML
                                    xmlTextWriter.WriteElementString(property.Name, valueString);
                                }
                            }
                        }
                    }

                // now we need to process the items that were stored in the Array List, if any.
                if (subItemCollections.Count > 0)
                    // We have sub item Collections, process them
                    foreach (var objProp in subItemCollections)
                    {
                        // get the Type of the property
                        propertyType = objProp.PropertyType;

                        var isBusinessEntity = false;
                        var isSubItemCollection = false;

                        foreach (var currentType in propertyType.GetInterfaces())
                        {
                            if (currentType.FullName == businessEntityType.FullName) isBusinessEntity = true;
                            if (currentType.FullName == subItemCollectionType.FullName) isSubItemCollection = true;
                        }

                        if (isBusinessEntity || propertyType.IsSubclassOf(businessEntityType))
                        {
                            // we are dealing with a Business Entity
                            // get a reference to the property
                            object objBE = objProp.GetValue(sourceObject, null) as IBusinessEntity;

                            // write a new element node
                            xmlTextWriter.WriteStartElement(GetEntityName(objBE));

                            // using recursion to scan this Business Entity
                            ScanObject(objBE, xmlTextWriter);

                            // close the element node
                            xmlTextWriter.WriteEndElement();
                        }
                        else if (isSubItemCollection || propertyType.IsSubclassOf(subItemCollectionType))
                        {
                            // we are dealing with a SubItemCollection or CrossLinkCollection
                            // we need to scan each item in the collection and process it.
                            var objSubItems = objProp.GetValue(sourceObject, null) as IEntitySubItemCollection;

                            if (objSubItems != null)
                                foreach (var objBE in objSubItems)
                                {
                                    xmlTextWriter.WriteStartElement(GetEntityName(objBE));
                                    ScanObject(objBE, xmlTextWriter);
                                    xmlTextWriter.WriteEndElement();
                                }
                        }
                    }
            }
        }

        /// <summary>
        ///     This writes the schema for the properties of the entity
        /// </summary>
        /// <param name="entityObject">
        ///     The Business Entity to be scanned
        /// </param>
        /// <param name="xmlTextWriter">
        ///     A reference to the XmlText writer being used to record the XML
        /// </param>
        /// <param name="entityName">
        ///     The name of the entity being processed
        /// </param>
        private void WriteSchema(object entityObject, XmlTextWriter xmlTextWriter, string entityName)
        {
            // write all the elements and attributes necessary to define the schema
            xmlTextWriter.WriteStartElement("xs:schema");
            xmlTextWriter.WriteAttributeString("id", "NewDataSet");
            xmlTextWriter.WriteAttributeString("xmlns", "");
            xmlTextWriter.WriteAttributeString("xmlns:xs", "http://www.w3.org/2001/XMLSchema");
            xmlTextWriter.WriteAttributeString("xmlns:msdata", "urn:schemas-microsoft-com:xml-msdata");

            xmlTextWriter.WriteStartElement("xs:element");
            xmlTextWriter.WriteAttributeString("name", entityName);

            xmlTextWriter.WriteStartElement("xs:complexType");
            xmlTextWriter.WriteStartElement("xs:sequence");

            // now we process the entity
            ScanObjectSchema(entityObject.GetType(), xmlTextWriter);

            // close the elements
            xmlTextWriter.WriteEndElement(); //"xs:sequence"
            xmlTextWriter.WriteEndElement(); //"xs:ComplexType"
            xmlTextWriter.WriteEndElement(); //"xs:element"
            xmlTextWriter.WriteEndElement(); //"xs:schema"
        }

        /// <summary>
        ///     This method scans the entity writing information to the schema
        ///     for each property.
        /// </summary>
        /// <param name="typeObject">
        ///     The Type of the object being processed
        /// </param>
        /// <param name="xmlTextWriter">
        ///     A reference to the XmlTextWriter being used to record the XML
        /// </param>
        private void ScanObjectSchema(Type typeObject, XmlTextWriter xmlTextWriter)
        {
            Type propertyType;

            // get a list of properties for the object
            var properties = typeObject.GetProperties();

            // this array list is used to store references to any property that
            // is either a Business Entity or a Collection
            var subItemCollections = new List<PropertyInfo>();

            // if we have properties, process them
            if (properties.GetLength(0) >= 0)
            {
                foreach (var property in properties)
                    // we only process properties that are not ReadOnly
                    if (property.CanRead)
                    {
                        // get the property type for the property
                        propertyType = property.PropertyType;

                        // First, we check whether we have an implementation of a significant interface
                        var interfaceFound = false;
                        foreach (var interfaceType in propertyType.GetInterfaces())
                            if (interfaceType.FullName == subItemCollectionType.FullName ||
                                interfaceType.FullName == crossLinkType.FullName)
                            {
                                interfaceFound = true;
                                break;
                            }

                        // if it is a SubItemCollection or CrossLink type, add it to the array
                        // list for later processing.  Otherwise we process it now.
                        if (interfaceFound || propertyType.IsSubclassOf(subItemCollectionType) || propertyType.IsSubclassOf(crossLinkType))
                            // This is a sub item collection, store it for later processing
                            subItemCollections.Add(property);
                        else
                        {
                            // make sure the property is not marked NotSerializable
                            if (IsSerializable(property))
                            {
                                var isEntity = false;
                                foreach (var interfaceType in propertyType.GetInterfaces())
                                    if (interfaceType.FullName == businessEntityType.FullName)
                                    {
                                        isEntity = true;
                                        break;
                                    }

                                if (isEntity || propertyType.IsSubclassOf(businessEntityType))
                                {
                                    // we are dealing with an instance of a Business Entity
                                    // write the schema for the entity
                                    WriteComplexSchemaElement(propertyType, xmlTextWriter);
                                }
                                else
                                {
                                    // we are dealing with something other than a Business Entity
                                    // we need to determine what it is and handle it accordingly
                                    if (propertyType.IsEnum)
                                        // we are dealing with an ENUM
                                        WriteEnumSchemaElement(property, propertyType, xmlTextWriter);
                                    else
                                        // we are dealing with a simple (int, string, bool, etc) property
                                        WriteSchemaElement(property, xmlTextWriter);
                                }
                            }
                        }
                    }

                // now we process the array list if there are any items in it
                if (subItemCollections.Count > 0)
                    // We have sub item Collections, process them
                    foreach (var objProp in subItemCollections)
                    {
                        var subItemProperties = objProp.PropertyType.GetProperties();
                        var businessEntityCollectionType = typeof(IEntitySubItemCollectionItem);

                        foreach (var objSubItemProp in subItemProperties)
                        {
                            var subItemType = objSubItemProp.PropertyType;

                            var simpleCollection = false;
                            foreach (var interfaceType in subItemType.GetInterfaces())
                                if (interfaceType.FullName == businessEntityCollectionType.FullName)
                                {
                                    simpleCollection = true;
                                    break;
                                }

                            if (simpleCollection || subItemType.IsSubclassOf(businessEntityCollectionType))
                            {
                                WriteComplexSchemaElement(subItemType, xmlTextWriter);
                                break;
                            }
                        }
                    }
            }
        }

        /// <summary>
        ///     This method is used to write an complex schema element to the xml document.
        ///     It is used to process properties that are Business Entities
        /// </summary>
        /// <param name="propertyTypeObject">
        ///     The Type object of the property
        /// </param>
        /// <param name="xmlTextWriter">
        ///     A reference to the XmlTextWriter object being used to record the XML
        /// </param>
        private void WriteComplexSchemaElement(Type propertyTypeObject, XmlTextWriter xmlTextWriter)
        {
            // write all the element and attribute strings necessary to define a
            // complex schema element
            xmlTextWriter.WriteStartElement("xs:element");
            var propertyTypeName = propertyTypeObject.Name;
            if (propertyTypeName.EndsWith("Entity")) propertyTypeName = propertyTypeName.Substring(0, propertyTypeName.Length - 6);
            xmlTextWriter.WriteAttributeString("name", propertyTypeName);
            xmlTextWriter.WriteAttributeString("minOccurs", "0");
            xmlTextWriter.WriteAttributeString("maxOccurs", "unbounded");
            xmlTextWriter.WriteStartElement("xs:complexType");
            xmlTextWriter.WriteStartElement("xs:sequence");

            // scan this property type
            ScanObjectSchema(propertyTypeObject, xmlTextWriter);

            // close all the elements
            xmlTextWriter.WriteEndElement(); //"xs:sequence"
            xmlTextWriter.WriteEndElement(); //"xs:complexType"
            xmlTextWriter.WriteEndElement(); //"xs:element"
        }

        /// <summary>
        ///     This method is used to write the schema for a property that is an ENUM
        /// </summary>
        /// <param name="propertyInfoObject">
        ///     A reference to the PropertyInfo object for the property
        /// </param>
        /// <param name="propertyTypeObject">
        ///     The Type object of the property
        /// </param>
        /// <param name="xmlTextWriter">
        ///     A reference to the XmlTextWriter object being used to record the XML
        /// </param>
        private void WriteEnumSchemaElement(PropertyInfo propertyInfoObject, Type propertyTypeObject, XmlTextWriter xmlTextWriter)
        {
            // write all the element and attribute strings necessary to define an ENUM element
            xmlTextWriter.WriteStartElement("xs:element");
            xmlTextWriter.WriteAttributeString("name", propertyInfoObject.Name);
            xmlTextWriter.WriteAttributeString("minOccurs", "0");
            xmlTextWriter.WriteStartElement("xs:simpleType");
            xmlTextWriter.WriteStartElement("xs:restriction");
            xmlTextWriter.WriteAttributeString("base", "xs:string");

            // use reflection to get a list of the values defined in the ENUM
            // then add an element with an attribute for each value
            var stringValues = Enum.GetNames(propertyTypeObject);

            foreach (var stringValue in stringValues)
            {
                xmlTextWriter.WriteStartElement("xs:enumeration");
                xmlTextWriter.WriteAttributeString("value", stringValue);
                xmlTextWriter.WriteEndElement();
            }

            // close the elements
            xmlTextWriter.WriteEndElement(); //"xs:restriction"
            xmlTextWriter.WriteEndElement(); //"xs:simpleType"
            xmlTextWriter.WriteEndElement(); //"xs:element"
        }

        /// <summary>This method is used to write the schema for a simple property</summary>
        /// <param name="propertyInfo ">The PropertyInfo object for the property being processed</param>
        /// <param name="xmlTextWriter"></param>
        private void WriteSchemaElement(PropertyInfo propertyInfo, XmlTextWriter xmlTextWriter)
        {
            var typeName = propertyInfo.PropertyType.ToString().Replace("System.", string.Empty).ToLower(CultureInfo.InvariantCulture);

            // convert type to W3C standard types
            switch (typeName)
            {
                case "int32":
                    typeName = "integer";
                    break;
                case "guid":
                    typeName = "string";
                    break;
                case "datetime":
                    typeName = "dateTime";
                    break;
            }

            // add this property to the schema
            xmlTextWriter.WriteStartElement("xs:element");
            var propertyName = propertyInfo.Name;
            if (propertyName.EndsWith("Entity")) propertyName = propertyName.Substring(0, propertyName.Length - 6);
            xmlTextWriter.WriteAttributeString("name", propertyName);
            xmlTextWriter.WriteAttributeString("type", "xs:" + typeName);
            xmlTextWriter.WriteAttributeString("minOccurs", "0");
            xmlTextWriter.WriteEndElement();
        }
    }

    /// <summary>
    ///     This defines a custom attribute to be used on Busines Entity
    ///     properties that should not be serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotReportSerializableAttribute : Attribute
    {
    }
}