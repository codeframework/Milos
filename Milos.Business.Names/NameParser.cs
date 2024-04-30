using System.Collections;
using System.Linq;
using System.Runtime.Serialization;

namespace Milos.Business.Names;

/// <summary>
/// Summary description for NameParser.
/// </summary>
// TODO: Add INameParser interface
public class NameParser
{
    /// <summary>
    /// Exposes list of CorpIDs (read-only). 
    /// </summary>
    public ArrayList CorpIDs { get; } = [];

    /// <summary>
    /// Exposes list of Suffixes (read-only).
    /// </summary>
    public ArrayList Suffixes { get; } = [];

    /// <summary>
    /// Exposes list of Titles (read-only).
    /// </summary>
    public ArrayList Titles { get; } = [];

    /// <summary>
    /// Default constructor.
    /// </summary>
    public NameParser()
    {
        // Populate CorpIDs list.
        CorpIDs.Add("&");
        CorpIDs.Add(" Corp.");
        CorpIDs.Add(" Corporation");
        CorpIDs.Add(" Llc.");
        CorpIDs.Add(" Llp.");
        CorpIDs.Add(" Limited");
        CorpIDs.Add(" Co.");
        CorpIDs.Add(" Company");
        CorpIDs.Add("Firm");
        CorpIDs.Add("Associates");

        // Populate Suffixes list.
        Suffixes.Add("I");
        Suffixes.Add("II");
        Suffixes.Add("III");
        Suffixes.Add("Jr.");
        Suffixes.Add("Sr.");

        // Populate Titles list.
        Titles.Add("Dr.");
        Titles.Add("Miss");
        Titles.Add("Mrs.");
        Titles.Add("Mr.");
        Titles.Add("Ms.");
        Titles.Add("Prof.");
    }

    /// <summary>
    /// Takes a string that contains a name and try guessing whether that's a Company Name.
    /// </summary>
    /// <param name="name">String containing a name.</param>
    /// <returns>Boolean indicating whether the name is a Company name or not.</returns>
    public bool IsCompanyName(string name)
    {
        name = name.ToLower(CultureInfo.InvariantCulture);
        return CorpIDs.Cast<object>().Any(t => name.IndexOf(t.ToString().ToLower(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0);
    }

    /// <summary>
    /// Takes a string and find out whether that's a suffix.
    /// </summary>
    /// <param name="suffix">String to analyze.</param>
    /// <returns>Boolean indicating whether it's a suffix or not.</returns>
    protected bool IsSuffix(string suffix)
    {
        suffix = suffix.ToLower(CultureInfo.InvariantCulture);
        return Suffixes.Cast<object>().Any(t => t.ToString().ToLower(CultureInfo.InvariantCulture) == suffix);
    }

    /// <summary>
    /// This method parses a given name.
    /// </summary>
    /// <example>
    /// string myName = "Dr. John Paul Jones III";
    /// EPS.Business.Names.Name name = new Name();
    /// np.ParseName(myName, name);
    /// Console.WriteLine("title: " + name.Title);
    /// Console.WriteLine("first: " + name.FirstName);
    /// Console.WriteLine("middle: " + name.MiddleName);
    /// Console.WriteLine("last: " + name.LastName);
    /// Console.WriteLine("suffix: " + name.Suffix);
    /// </example>
    /// <param name="name">The name that must be parsed.</param>
    /// <param name="data">The Name object that will hold the parsed name.</param>
    public void ParseName(string name, Name data)
    {
        // Keep a copy of the name that's been passed.
        //string originalValue = name;

        if (string.IsNullOrEmpty(name.Trim()))
        {
            data.FirstName = string.Empty;
            data.MiddleName = string.Empty;
            data.LastName = string.Empty;
            data.Title = string.Empty;
            data.Suffix = string.Empty;
            return;
        }

        if (!IsCompanyName(name))
        {
            // Names can be in two patterns (last/first/middle or first/middle/last) if the former, get the last name first
            if (name.IndexOf(",", StringComparison.Ordinal) >= 0)
            {
                data.LastName = name.Substring(0, name.IndexOf(",", StringComparison.Ordinal));

                // We might have a suffix along with the last name
                if (data.LastName.IndexOf(" ", StringComparison.Ordinal) >= 0)
                {
                    data.Suffix = data.LastName.Substring(data.LastName.IndexOf(" ", StringComparison.Ordinal));

                    if (!string.IsNullOrEmpty(data.Suffix) && GetSuffixesList().ToLower(CultureInfo.InvariantCulture).IndexOf(data.Suffix.ToLower(CultureInfo.InvariantCulture), StringComparison.Ordinal) > 0)
                        data.LastName = data.LastName.Substring(0, data.LastName.IndexOf(" ", StringComparison.Ordinal));
                    else
                        data.Suffix = string.Empty;
                }

                // Next chunk will be the first name.
                name = name.Substring(name.IndexOf(",", StringComparison.Ordinal) + 1).Trim();

                // We check for a suffix in this part as well.
                // We look for the middle name prior to look for the suffix.
                if (name.IndexOf(" ", StringComparison.Ordinal) >= 0)
                {
                    data.FirstName = name.Substring(0, name.IndexOf(" ", StringComparison.Ordinal));
                    data.MiddleName = name.Substring(name.IndexOf(" ", StringComparison.Ordinal));
                }
                else
                    data.FirstName = name;

                if (data.MiddleName.IndexOf(" ", StringComparison.Ordinal) >= 0 || GetSuffixesList().ToLower(CultureInfo.InvariantCulture).IndexOf(data.MiddleName.ToLower(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                {
                    var sfx = name.Substring(StringHelper.At(" ", name, StringHelper.Occurs(" ", name))).Trim();
                    var oldSfx = sfx;

                    if (IsSuffix(sfx))
                    {
                        data.Suffix = sfx.Trim();
                        data.MiddleName = data.MiddleName.Replace(oldSfx, "").Trim();
                        name = name.Substring(0, StringHelper.At(" ", name, StringHelper.Occurs(" ", name)));
                    }
                    else
                        data.MiddleName = data.FirstName != sfx.Trim() ? sfx.Trim() : string.Empty; // This is not a suffix; it is probably a middle name.
                }
            }
            else
            {
                // We check whether there is a title in the name
                data.Title = name.IndexOf(" ", StringComparison.Ordinal) >= 0 ? name.Substring(0, StringHelper.At(" ", name)).Trim() : name.Trim();

                if (!string.IsNullOrEmpty(data.Title) && GetTitlesList().ToLower(CultureInfo.InvariantCulture).IndexOf(data.Title.ToLower(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                    // We found a title...
                    name = name.IndexOf(" ", StringComparison.Ordinal) >= 0 ? name.Substring(name.IndexOf(" ", StringComparison.Ordinal)).Trim() : "";
                else
                    // No title was specified...
                    data.Title = "";

                // Now we check for a suffix
                data.Suffix = name.IndexOf(" ", StringComparison.Ordinal) >= 0 ? name.Substring(StringHelper.At(" ", name, StringHelper.Occurs(" ", name))).Trim() : name.Trim();

                if (!string.IsNullOrEmpty(data.Suffix) && IsSuffix(data.Suffix))
                    name = name.IndexOf(" ", StringComparison.Ordinal) >= 0 ? name.Substring(0, StringHelper.At(" ", name, StringHelper.Occurs(" ", name))).Trim() : string.Empty;
                else
                    data.Suffix = string.Empty; // No suffix was specified

                // Now, we pull out the first name
                if (name.IndexOf(" ", StringComparison.Ordinal) >= 0)
                {
                    data.FirstName = name.Substring(0, name.IndexOf(" ", StringComparison.Ordinal)).Trim();
                    name = name.Substring(name.IndexOf(" ", StringComparison.Ordinal)).Trim();
                }
                else
                    // No space was found. We assume that this is the last name only.
                    data.FirstName = string.Empty;

                // We now check for a middle name
                if (name.IndexOf(" ", StringComparison.Ordinal) >= 0)
                {
                    data.MiddleName = name.Substring(0, StringHelper.At(" ", name, StringHelper.Occurs(" ", name))).Trim();
                    name = name.Substring(StringHelper.At(" ", name, StringHelper.Occurs(" ", name))).Trim();
                }
                else
                    data.MiddleName = string.Empty; // No middle name was found

                if (string.IsNullOrEmpty(data.LastName))
                    // Whatever is left is the last name
                    data.LastName = name.Trim();
            }
        }
        else
        {
            // Special parsing for company names...
            data.FirstName = string.Empty;
            data.MiddleName = string.Empty;
            data.Title = string.Empty;
            data.Suffix = string.Empty;
            data.LastName = name;
        }
    }

    /// <summary>
    /// GetSuffixesList returns a comma-delimited list of suffixes.
    /// </summary>
    /// <returns>String containing comma-delimited list of suffixes.</returns>
    protected string GetSuffixesList()
    {
        var suffixes = string.Empty;
        foreach (var suffix in Suffixes)
            suffixes += suffix + ",";
        return suffixes;
    }

    /// <summary>
    /// GetTitlesList returns a comma-delimited list of titles.
    /// </summary>
    /// <returns>String containing comma-delimited list of titles.</returns>
    protected string GetTitlesList()
    {
        var titles = "";
        foreach (var title in Titles)
            titles += title + ",";
        return titles;
    }

    /// <summary>
    /// Delegate for the InvalidName event.
    /// </summary>
    public delegate void InvalidNameEventHandler(object sender, InvalidNameEventArgs e);

    /// <summary>
    /// Event that's gonna be fired whenever the Name dialog parser is required.
    /// </summary>
    public event InvalidNameEventHandler InvalidName;

    /// <summary>
    /// Notifies subscribers of InvalidName event.
    /// </summary>
    /// <param name="e">Args containing info about the source (name) and the Name object (parser's best guess for the parsing).</param>
    protected virtual void OnInvalidName(InvalidNameEventArgs e) => InvalidName?.Invoke(this, e);

    /// <summary>
    /// EventArgs class for the ShowNameWindow event.
    /// </summary>
    public class InvalidNameEventArgs : EventArgs
    {
        /// <summary>
        /// This is a string containing the full name sent to the parser.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// This is the Name object that holds the parsed name (or whatever the parser's best guess was).
        /// </summary>
        public Name Name { get; set; }
    }
}

/// <summary>
/// This class is meant to help passing a "name" back and forth. Mainly used by the Name Parser.
/// </summary>
public class Name
{
    /// <summary>
    /// Canceled
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// Updated
    /// </summary>
    public bool Updated { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Middle name
    /// </summary>
    public string MiddleName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Suffix
    /// </summary>
    public string Suffix { get; set; } = string.Empty;

    /// <summary>
    /// Check whether all fields are empty.
    /// </summary>
    /// <returns></returns>
    public bool IsClean() => string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(MiddleName) && string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(Suffix);

    /// <summary>
    /// Fills a given Name entity with the content of this dummy object.
    /// </summary>
    /// <param name="entity">Entity to be filled.</param>
    public void FillNameEntity(NameBusinessEntity entity)
    {
        if (entity == null) return;
        entity.Title = Title;
        entity.FirstName = FirstName;
        entity.MiddleName = MiddleName;
        entity.LastName = LastName;
        entity.Suffix = Suffix;
    }

    /// <summary>
    /// Fills this dummy object with data from a Name entity.
    /// </summary>
    /// <param name="entity">Name entity from where to getting the data from.</param>
    public void FillFromNameEntity(NameBusinessEntity entity)
    {
        if (entity == null) return;
        Title = entity.Title;
        FirstName = entity.FirstName;
        MiddleName = entity.MiddleName;
        LastName = entity.LastName;
        Suffix = entity.Suffix;
    }
}

/// <summary>
/// NameParserException class. It's meant to be thrown anytime an error
/// has occurred when parsing a name.
/// </summary>
[Serializable]
public class NameParserException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    public NameParserException() { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message">Message</param>
    public NameParserException(string message) : base(message) { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="message">Message</param>
    /// <param name="inner">Inner exception</param>
    public NameParserException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="info">Serialization Info</param>
    /// <param name="context">Streaming Context</param>
    protected NameParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}